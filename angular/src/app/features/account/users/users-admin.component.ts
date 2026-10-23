import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MsgService } from '../../../core/services/msg.service';
import { SharedModule } from '../../../shared/shared.module';
import { AppUser } from './user.model';
import { UserService } from './user.service';

/** Usuarios de la empresa: búsqueda, alta, edición, clave y TOTP. */
@Component({
  selector: 'app-users-admin',
  imports: [FormsModule, SharedModule],
  templateUrl: './users-admin.component.html',
})
export class UsersAdminComponent {
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly service = inject(UserService);

  protected readonly users = signal<AppUser[]>([]);
  protected filter = { FullName: '', Email: '', IncludeInactive: false };

  protected passwordModal = { show: false, uuid: '', userName: '', newPassword: '', confirm: '', showPwd: false };

  protected formatDate(date?: string): string {
    if (!date) return '—';
    return new Date(date).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  protected async search() {
    const { ok, Data } = await this.service.getByName(this.filter.FullName.trim(), this.filter.Email, this.filter.IncludeInactive);
    if (ok) this.users.set(Data);
  }

  protected create() {
    this.router.navigate(['user-edit', '0']);
  }

  protected edit(user: AppUser) {
    this.router.navigate(['user-edit', user.Uuid]);
  }

  protected async remove(uuid: string) {
    if (!(await this.msg.showMessageQuestion('¿Desea eliminar el usuario?'))) return;
    const { ok } = await this.service.remove(uuid);
    if (ok) {
      await this.msg.showMessageModal({ Description: 'El usuario se eliminó correctamente.', MessageType: 'success' });
      await this.search();
    }
  }

  protected openPasswordModal(user: AppUser) {
    this.passwordModal = { show: true, uuid: user.Uuid, userName: user.FullName, newPassword: '', confirm: '', showPwd: false };
  }

  protected closePasswordModal() {
    this.passwordModal.show = false;
  }

  protected async savePassword() {
    const m = this.passwordModal;
    m.newPassword = m.newPassword.trim();
    m.confirm = m.confirm.trim();
    if (!m.newPassword || m.newPassword.length < 6) {
      await this.msg.showMessageModal({ Description: 'La contraseña debe tener al menos 6 caracteres.', MessageType: 'warning' });
      return;
    }
    if (m.newPassword !== m.confirm) {
      await this.msg.showMessageModal({ Description: 'Las contraseñas no coinciden.', MessageType: 'warning' });
      return;
    }
    if (!(await this.msg.showMessageQuestion(`¿Cambiar la contraseña de ${m.userName}?`))) return;

    const { ok } = await this.service.changePassword(m.uuid, m.newPassword);
    if (ok) {
      this.closePasswordModal();
      await this.msg.showMessageModal({ Description: 'Contraseña actualizada correctamente.', MessageType: 'success' });
    }
  }

  protected async requireMfa(user: AppUser) {
    if (!(await this.msg.showMessageQuestion(`¿Requerir TOTP a ${user.FullName}? El usuario deberá configurarlo en su próximo inicio de sesión.`))) return;
    const { ok } = await this.service.requireMfa(user.Uuid);
    if (ok) {
      this.patch(user, { MfaRequired: true });
      await this.msg.showMessageModal({ Description: 'Se ha marcado el TOTP como requerido para el usuario.', MessageType: 'success' });
    }
  }

  protected async unrequireMfa(user: AppUser) {
    if (!(await this.msg.showMessageQuestion(`¿Quitar la obligatoriedad de TOTP para ${user.FullName}?`))) return;
    const { ok } = await this.service.unrequireMfa(user.Uuid);
    if (ok) {
      this.patch(user, { MfaRequired: false });
      await this.msg.showMessageModal({ Description: 'Se quitó la obligatoriedad de TOTP.', MessageType: 'success' });
    }
  }

  protected async resetMfa(user: AppUser) {
    if (!(await this.msg.showMessageQuestion(`¿Resetear el TOTP de ${user.FullName}? Esto deshabilita y elimina su configuración actual.`))) return;
    const { ok } = await this.service.resetMfa(user.Uuid);
    if (ok) {
      this.patch(user, { MfaEnabled: false, MfaRequired: false });
      await this.msg.showMessageModal({ Description: 'El TOTP del usuario fue reseteado correctamente.', MessageType: 'success' });
    }
  }

  /** Refleja el cambio en la fila sin volver a consultar la lista. */
  private patch(user: AppUser, changes: Partial<AppUser>) {
    this.users.update(list => list.map(u => (u.Uuid === user.Uuid ? { ...u, ...changes } : u)));
  }
}
