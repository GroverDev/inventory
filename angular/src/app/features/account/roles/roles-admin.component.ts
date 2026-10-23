import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MsgService } from '../../../core/services/msg.service';
import { SharedModule } from '../../../shared/shared.module';
import { Role } from './role.model';
import { RoleService } from './role.service';

/** Roles de seguridad: búsqueda, alta, edición (con asignación de formularios) y baja. */
@Component({
  selector: 'app-roles-admin',
  imports: [FormsModule, SharedModule],
  templateUrl: './roles-admin.component.html',
})
export class RolesAdminComponent {
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly service = inject(RoleService);

  protected readonly roles = signal<Role[]>([]);
  protected filter = '';

  protected async search() {
    const { ok, Data } = await this.service.getAll(this.filter.trim());
    if (ok) this.roles.set(Data);
  }

  protected create() {
    this.router.navigate(['role-edit', '0']);
  }

  protected edit(role: Role) {
    this.router.navigate(['role-edit', role.Id]);
  }

  protected async remove(id: number) {
    if (!(await this.msg.showMessageQuestion('¿Desea eliminar el rol?'))) return;
    const { ok } = await this.service.remove(id);
    if (ok) {
      await this.msg.showMessageModal({ Description: 'El rol se eliminó correctamente.', MessageType: 'success' });
      await this.search();
    }
  }
}
