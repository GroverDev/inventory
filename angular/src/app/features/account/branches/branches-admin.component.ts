import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { MsgService } from '../../../core/services/msg.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { SharedModule } from '../../../shared/shared.module';
import { Branch } from './branch.model';
import { BranchService } from './branch.service';

const FORM = 'branches-admin';

/** Sucursales de la empresa: lista con filtro y acceso a su edición. */
@Component({
  selector: 'app-branches-admin',
  imports: [FormsModule, SharedModule],
  templateUrl: './branches-admin.component.html',
})
export class BranchesAdminComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly permissions = inject(PermissionsService);
  private readonly auth = inject(AuthService);
  private readonly branchService = inject(BranchService);

  protected readonly branches = signal<Branch[]>([]);
  protected filter = '';
  protected includeInactive = false;

  protected readonly canCreate = computed(() => this.permissions.can(FORM, 'create'));
  protected readonly canUpdate = computed(() => this.permissions.can(FORM, 'update'));
  protected readonly canDelete = computed(() => this.permissions.can(FORM, 'delete'));
  protected readonly currentBranchId = computed(() => this.auth.user()?.BranchId);

  // Son pocas: se listan al entrar, sin esperar una búsqueda.
  ngOnInit() {
    return this.search();
  }

  protected async search() {
    const { ok, Data } = await this.branchService.getAll(this.filter.trim(), this.includeInactive);
    if (ok) this.branches.set(Data);
  }

  protected create() {
    this.router.navigate(['branch-edit', '0']);
  }

  protected edit(branch: Branch) {
    this.router.navigate(['branch-edit', branch.Id]);
  }

  protected async remove(branch: Branch) {
    const confirmed = await this.msg.showMessageQuestion(
      `¿Desea eliminar la sucursal «${branch.Name}»? Sus ventas y movimientos se conservan para consulta.`);
    if (!confirmed) return;

    const { ok } = await this.branchService.remove(branch.Id);
    if (ok) {
      await this.msg.showMessageModal({ Description: 'La sucursal se eliminó correctamente.', MessageType: 'success' });
      await this.search();
    }
  }
}
