import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';

import { MsgService } from '../../../core/services/msg.service';
import { FormAction, FormActionsComponent } from '../../../shared/form-actions/form-actions.component';
import { SharedModule } from '../../../shared/shared.module';
import { AppForm } from '../forms/form.model';
import { FormService } from '../forms/form.service';
import { FormTreeNodeComponent, TreeNode } from './form-tree-node.component';
import { Role, newRole } from './role.model';
import { RoleService } from './role.service';

@Component({
  selector: 'app-role-edit',
  imports: [FormsModule, NgbDropdownModule, SharedModule, FormActionsComponent, FormTreeNodeComponent],
  templateUrl: './role-edit.component.html',
})
export class RoleEditComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly roleService = inject(RoleService);
  private readonly formService = inject(FormService);

  protected role: Role = newRole();
  protected readonly isSaved = signal(false);
  protected submitted = false;

  protected readonly allForms = signal<AppForm[]>([]);
  protected readonly selectedFormIds = signal<number[]>([]);

  protected readonly actions: FormAction[] = [
    { label: 'Grabar', icon: 'ri-save-line', btnClass: 'btn-primary', run: () => this.saveRole(), disabled: () => this.isSaved() },
    { label: 'Cancelar', icon: 'ri-forbid-line', btnClass: 'btn-warning', run: () => this.back() },
  ];

  protected readonly formsAsTree = computed((): TreeNode[] => {
    const nodeMap = new Map<number, TreeNode>();
    this.allForms().forEach(form => nodeMap.set(form.Id, { form, children: [] }));

    const roots: TreeNode[] = [];
    this.allForms().forEach(form => {
      const node = nodeMap.get(form.Id)!;
      const parent = nodeMap.get(form.FormId);
      if (parent) parent.children.push(node);
      else roots.push(node);
    });

    const sortNode = (n: TreeNode) => {
      n.children.sort((a, b) => a.form.Orden - b.form.Orden);
      n.children.forEach(sortNode);
    };
    roots.sort((a, b) => a.form.Orden - b.form.Orden);
    roots.forEach(sortNode);
    return roots;
  });

  protected readonly allSelected = computed(
    () => this.selectedFormIds().length === this.allForms().length && this.allForms().length > 0);

  async ngOnInit() {
    const roleId = parseInt(this.route.snapshot.paramMap.get('id') ?? '0', 10);
    await this.loadAllForms();
    if (roleId > 0) {
      await this.loadRole(roleId);
      await this.loadAssignedForms(roleId);
    }
  }

  // El menú de la app se arma por jerarquía padre->hijo: un formulario cuyo padre no está
  // asignado al rol queda huérfano y no se muestra nunca. Esto espeja lo que hace
  // AssignFormsToRole en el backend, para que el árbol refleje lo que se va a guardar: cada
  // pantalla marcada arrastra a sus ancestros, y los contenedores que quedan sin ninguna
  // pantalla debajo se descartan, porque solo darían un grupo vacío en el menú.
  protected onSelection(ids: number[]) {
    this.selectedFormIds.set(this.normalizeSelection(ids));
  }

  private normalizeSelection(ids: number[]): number[] {
    const byId = new Map<number, AppForm>(this.allForms().map(f => [f.Id, f]));
    const result = new Set<number>();

    ids.forEach(id => {
      const form = byId.get(id);
      if (!form) return;
      if (!form.IsFormRegister && form.Route === 'ninguno') return; // contenedor: entra solo como ancestro

      let current: AppForm | undefined = form;
      while (current && !result.has(current.Id)) {
        result.add(current.Id);
        current = byId.get(current.FormId);
      }
    });
    return [...result];
  }

  protected toggleAll() {
    this.selectedFormIds.set(this.allSelected() ? [] : this.allForms().map(f => f.Id));
  }

  private async loadRole(id: number) {
    const { ok, Data } = await this.roleService.getById(id);
    if (ok) this.role = Data;
  }

  private async loadAllForms() {
    const { ok, Data } = await this.formService.getAll('');
    if (ok) this.allForms.set(Data);
  }

  private async loadAssignedForms(rolId: number) {
    const { ok, Data } = await this.roleService.getForms(rolId);
    if (ok) this.selectedFormIds.set(Data.map(f => f.Id));
  }

  protected back() {
    this.router.navigate(['roles-admin']);
  }

  protected async saveRole() {
    this.submitted = true;
    this.role.NameRol = this.role.NameRol.trim();
    if (!this.role.NameRol) return;
    if (!(await this.msg.showMessageQuestion('¿Desea guardar el rol?'))) return;

    if (this.role.Id === 0) {
      const { ok, Data } = await this.roleService.create(this.role);
      if (!ok) return;
      this.role.Id = Data;
      await this.msg.showMessageModal({ Description: 'El rol se creó correctamente. Ahora puede asignar formularios.', MessageType: 'success' });
      await this.loadAllForms();
    } else {
      const { ok } = await this.roleService.update(this.role);
      if (ok) await this.msg.showMessageModal({ Description: 'El rol se actualizó correctamente.', MessageType: 'success' });
    }
  }

  protected async saveForms() {
    if (!(await this.msg.showMessageQuestion('¿Desea guardar la asignación de formularios?'))) return;

    const { ok } = await this.roleService.assignForms(this.role.Id, this.selectedFormIds());
    if (ok) {
      // El backend normaliza la selección (agrega ancestros, descarta contenedores vacíos):
      // se recarga para que el árbol muestre lo que realmente quedó guardado.
      await this.loadAssignedForms(this.role.Id);
      await this.msg.showMessageModal({ Description: 'Los formularios se asignaron correctamente.', MessageType: 'success' });
    }
  }
}
