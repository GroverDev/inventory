import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { MsgService } from '../../../core/services/msg.service';
import { FormAction, FormActionsComponent } from '../../../shared/form-actions/form-actions.component';
import { SharedModule } from '../../../shared/shared.module';
import { Branch, newBranch } from './branch.model';
import { BranchService } from './branch.service';

@Component({
  selector: 'app-branch-edit',
  imports: [FormsModule, SharedModule, FormActionsComponent],
  templateUrl: './branch-edit.component.html',
})
export class BranchEditComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly auth = inject(AuthService);
  private readonly branchService = inject(BranchService);

  protected branch: Branch = newBranch();
  protected readonly isSaved = signal(false);
  protected submitted = false;

  protected readonly actions: FormAction[] = [
    { label: 'Grabar', icon: 'ri-save-line', btnClass: 'btn-primary', run: () => this.save(), disabled: () => this.isSaved() },
    { label: 'Cancelar', icon: 'ri-forbid-line', btnClass: 'btn-warning', run: () => this.back() },
  ];

  protected get nameInvalid(): boolean {
    return (this.branch.Name?.trim().length ?? 0) < 2;
  }

  async ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== '0') {
      const { ok, Data } = await this.branchService.getById(id);
      if (ok) this.branch = Data;
    }
  }

  protected back() {
    this.router.navigate(['branches-admin']);
  }

  protected async save() {
    this.submitted = true;
    if (this.nameInvalid) return;
    if (!(await this.msg.showMessageQuestion('¿Desea guardar la sucursal?'))) return;

    const creating = !this.branch.Id;
    const { ok } = creating ? await this.branchService.create(this.branch) : await this.branchService.update(this.branch);
    if (!ok) return;

    if (creating) this.isSaved.set(true);
    // El creador quedó habilitado en la sucursal nueva: se renueva la sesión para
    // que aparezca en el selector de sucursal sin volver a entrar.
    await this.auth.refreshBranches();
    await this.msg.showMessageModal({
      Description: creating ? 'La sucursal se creó correctamente.' : 'La sucursal se actualizó correctamente.',
      MessageType: 'success',
    });
    this.back();
  }
}
