import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';

import { MsgService } from '../../../core/services/msg.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { FormAction, FormActionsComponent } from '../../../shared/form-actions/form-actions.component';
import { SharedModule } from '../../../shared/shared.module';
import { UserBranch } from '../branches/branch.model';
import { Role } from '../roles/role.model';
import { RoleService } from '../roles/role.service';
import { Session, isLongSession } from '../sessions/session.model';
import { SessionService } from '../sessions/session.service';
import { AppUser, newUser } from './user.model';
import { UserService } from './user.service';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

@Component({
  selector: 'app-user-edit',
  imports: [FormsModule, NgbDropdownModule, SharedModule, FormActionsComponent],
  templateUrl: './user-edit.component.html',
})
export class UserEditComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly permissions = inject(PermissionsService);
  private readonly userService = inject(UserService);
  private readonly roleService = inject(RoleService);
  private readonly sessionService = inject(SessionService);

  protected user: AppUser = newUser();
  protected readonly isSaved = signal(false);
  protected submitted = false;
  protected showPassword = false;

  protected readonly allRoles = signal<Role[]>([]);
  protected readonly selectedRoleIds = signal<number[]>([]);
  protected readonly sessions = signal<Session[]>([]);
  protected readonly userBranches = signal<UserBranch[]>([]);
  protected readonly enabledBranchIds = signal<string[]>([]);
  protected readonly defaultBranchId = signal('');

  protected readonly canReadSessions = computed(() => this.permissions.can('active-sessions', 'read'));
  protected readonly canDeleteSessions = computed(() => this.permissions.can('active-sessions', 'delete'));
  protected readonly canUpdateUsers = computed(() => this.permissions.can('users-admin', 'update'));
  protected readonly isLongSession = isLongSession;

  protected readonly allRolesSelected = computed(
    () => this.selectedRoleIds().length === this.allRoles().length && this.allRoles().length > 0);

  protected readonly actions: FormAction[] = [
    { label: 'Grabar', icon: 'ri-save-line', btnClass: 'btn-primary', run: () => this.saveUser(), disabled: () => this.isSaved() },
    { label: 'Cancelar', icon: 'ri-forbid-line', btnClass: 'btn-warning', run: () => this.back() },
  ];

  protected get fullNameInvalid() { return !this.user.FullName.trim(); }
  protected get emailRequired() { return !this.user.Email.trim(); }
  protected get emailFormat() { return !EMAIL_PATTERN.test(this.user.Email.trim()); }
  protected get passwordRequired() { return !this.user.Uuid && !(this.user.Password ?? '').trim(); }

  async ngOnInit() {
    const userId = this.route.snapshot.paramMap.get('id');
    await this.loadAllRoles();
    if (userId && userId !== '0') {
      await this.loadUser(userId);
      await this.loadUserRoles(userId);
      await this.loadUserBranches(userId);
      if (this.canReadSessions()) await this.loadSessions(userId);
    }
  }

  private async loadUser(uuid: string) {
    const { ok, Data } = await this.userService.getById(uuid);
    if (ok) this.user = { ...Data, Password: '' };
  }

  private async loadAllRoles() {
    const { ok, Data } = await this.roleService.getAll('', '');
    if (ok) this.allRoles.set(Data);
  }

  private async loadUserRoles(uuid: string) {
    const { ok, Data } = await this.userService.getRoles(uuid);
    if (ok) this.selectedRoleIds.set(Data.map(r => r.Id));
  }

  protected isRoleSelected(id: number) {
    return this.selectedRoleIds().includes(id);
  }

  protected toggleRole(id: number, checked: boolean) {
    this.selectedRoleIds.update(ids => (checked ? [...ids, id] : ids.filter(x => x !== id)));
  }

  protected toggleAllRoles() {
    this.selectedRoleIds.set(this.allRolesSelected() ? [] : this.allRoles().map(r => r.Id));
  }

  protected async saveRoles() {
    if (!(await this.msg.showMessageQuestion('¿Desea guardar la asignación de roles?'))) return;
    const { ok } = await this.userService.assignRoles(this.user.Uuid, this.selectedRoleIds());
    if (ok) await this.msg.showMessageModal({ Description: 'Los roles se asignaron correctamente.', MessageType: 'success' });
  }

  private async loadUserBranches(uuid: string) {
    const { ok, Data } = await this.userService.getBranches(uuid);
    if (!ok) return;
    this.userBranches.set(Data);
    this.enabledBranchIds.set(Data.filter(b => b.Enabled).map(b => b.BranchId));
    this.defaultBranchId.set(Data.find(b => b.IsDefault)?.BranchId ?? '');
  }

  protected isBranchEnabled(id: string) {
    return this.enabledBranchIds().includes(id);
  }

  protected toggleBranch(id: string, checked: boolean) {
    this.enabledBranchIds.update(ids => (checked ? [...ids, id] : ids.filter(x => x !== id)));
    this.keepDefaultConsistent();
  }

  /** Una sucursal deshabilitada no puede ser la predeterminada, y con una sola habilitada no hay nada que elegir. */
  private keepDefaultConsistent() {
    if (!this.enabledBranchIds().includes(this.defaultBranchId())) this.defaultBranchId.set('');
    if (!this.defaultBranchId() && this.enabledBranchIds().length === 1) this.defaultBranchId.set(this.enabledBranchIds()[0]);
  }

  protected async saveBranches() {
    if (this.enabledBranchIds().length === 0) {
      await this.msg.showMessageModal({ Description: 'Habilite al menos una sucursal: sin ninguna, el usuario no podrá iniciar sesión.', MessageType: 'warning' });
      return;
    }
    if (!this.defaultBranchId()) {
      await this.msg.showMessageModal({ Description: 'Elija la sucursal predeterminada.', MessageType: 'warning' });
      return;
    }
    if (!(await this.msg.showMessageQuestion('¿Desea guardar las sucursales del usuario?'))) return;

    const { ok } = await this.userService.assignBranches(this.user.Uuid, this.enabledBranchIds(), this.defaultBranchId());
    if (ok) {
      await this.msg.showMessageModal({ Description: 'Las sucursales se guardaron. El cambio se aplica al usuario en su próxima renovación de sesión.', MessageType: 'success' });
      await this.loadUserBranches(this.user.Uuid);
    }
  }

  protected back() {
    this.router.navigate(['users-admin']);
  }

  protected formatSessionDate(date: string): string {
    if (!date) return '—';
    return new Date(date).toLocaleString('es-BO', {
      day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false,
    });
  }

  private async loadSessions(uuid: string) {
    const { ok, Data } = await this.sessionService.getUserSessions(uuid);
    if (ok) this.sessions.set(Data);
  }

  protected async closeOneSession(id: number) {
    if (!(await this.msg.showMessageQuestion('¿Cerrar esta sesión? El usuario deberá iniciar sesión de nuevo en ese dispositivo.'))) return;
    const { ok } = await this.sessionService.close(id);
    if (ok) {
      await this.msg.showMessageModal({ Description: 'La sesión se cerró correctamente.', MessageType: 'success' });
      await this.loadSessions(this.user.Uuid);
    }
  }

  protected async closeAllSessions() {
    if (!(await this.msg.showMessageQuestion(
      `¿Cerrar todas las sesiones de ${this.user.FullName}? Deberá iniciar sesión de nuevo en todos sus dispositivos.`))) return;
    const { ok } = await this.sessionService.closeAllUserSessions(this.user.Uuid);
    if (ok) {
      await this.msg.showMessageModal({ Description: 'Se cerraron todas las sesiones del usuario.', MessageType: 'success' });
      await this.loadSessions(this.user.Uuid);
    }
  }

  protected async saveUser() {
    this.submitted = true;
    this.user.FullName = this.user.FullName.trim();
    this.user.UserName = this.user.UserName.trim();
    this.user.Email = this.user.Email.trim();
    this.user.Password = (this.user.Password ?? '').trim();
    if (this.fullNameInvalid || this.emailRequired || this.emailFormat || this.passwordRequired) return;

    if (!(await this.msg.showMessageQuestion('¿Desea guardar el usuario?'))) return;

    if (!this.user.Uuid || this.user.Uuid === '0') {
      const { ok, Data } = await this.userService.create(this.user);
      if (ok && Data) {
        this.isSaved.set(true);
        this.user.Uuid = Data;
        await this.loadUserRoles(Data);
        await this.loadUserBranches(Data);
        await this.msg.showMessageModal({ Description: 'El usuario se creó correctamente. Ahora asigne sus roles y presione «Guardar Roles».', MessageType: 'success' });
      }
    } else {
      const { ok, Data } = await this.userService.update(this.user);
      if (ok && Data) {
        await this.msg.showMessageModal({ Description: 'El usuario se actualizó correctamente.', MessageType: 'success' });
        this.back();
      }
    }
  }
}
