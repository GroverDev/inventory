import { Component, OnInit, computed, inject, signal } from '@angular/core';

import { MsgService } from '../../../core/services/msg.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { SharedModule } from '../../../shared/shared.module';
import { ConnectedUser, isLongSession } from './session.model';
import { SessionService } from './session.service';

/** Usuarios conectados ahora, con la opción de cerrarles la sesión. */
@Component({
  selector: 'app-active-sessions',
  imports: [SharedModule],
  templateUrl: './active-sessions.component.html',
})
export class ActiveSessionsComponent implements OnInit {
  private readonly msg = inject(MsgService);
  private readonly permissions = inject(PermissionsService);
  private readonly service = inject(SessionService);

  protected readonly sessions = signal<ConnectedUser[]>([]);
  protected readonly canDelete = computed(() => this.permissions.can('active-sessions', 'delete'));
  protected readonly isLongSession = isLongSession;

  ngOnInit() {
    return this.load();
  }

  protected formatDate(date: string): string {
    if (!date) return '—';
    return new Date(date).toLocaleString('es-BO', {
      day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false,
    });
  }

  protected async load() {
    const { ok, Data } = await this.service.getConnectedUsers();
    if (ok) this.sessions.set(Data);
  }

  protected async close(session: ConnectedUser) {
    const confirmed = await this.msg.showMessageQuestion(
      `¿Cerrar la sesión de ${session.FullName} en "${session.Device || 'este dispositivo'}"? Se le pedirá iniciar sesión de nuevo.`);
    if (!confirmed) return;

    const { ok } = await this.service.close(session.Id);
    if (ok) {
      await this.msg.showMessageModal({ Description: 'La sesión se cerró correctamente.', MessageType: 'success' });
      await this.load();
    }
  }
}
