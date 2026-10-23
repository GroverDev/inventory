import { Component, EventEmitter, Output, TemplateRef, ViewChild, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { NgbModal, NgbModalRef } from '@ng-bootstrap/ng-bootstrap';

import { ResponseArray, ResponseObject } from '../../core/models/api.models';
import { BranchOption } from '../../core/models/user.model';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { EventService } from '../../core/services/event.service';
import { MsgService } from '../../core/services/msg.service';
import { RootReducerState } from '../../store';
import { changeMode } from '../../store/layouts/layout-action';

interface TrustedDevice {
  Id: number;
  DeviceLabel: string;
  CreatedAt: string;
  ExpiresAt: string;
}

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss'],
  standalone: false
})
export class TopbarComponent {
  readonly auth = inject(AuthService);
  private readonly api = inject(ApiService);
  private readonly msg = inject(MsgService);
  private readonly router = inject(Router);
  private readonly modalService = inject(NgbModal);
  private readonly eventService = inject(EventService);
  private readonly store = inject(Store<RootReducerState>);

  @Output() mobileMenuButtonClicked = new EventEmitter();
  @ViewChild('changePassword') changePasswordTpl!: TemplateRef<unknown>;
  @ViewChild('trustedDevices') trustedDevicesTpl!: TemplateRef<unknown>;

  private modalRef?: NgbModalRef;
  private readonly element = document.documentElement;

  pwd = { current: '', next: '', confirm: '', showCurrent: false, showNext: false };

  devices: TrustedDevice[] = [];
  devicesLoading = false;

  get branches(): BranchOption[] {
    return this.auth.user()?.Branches ?? [];
  }

  /** Alterna el menú lateral en pantallas pequeñas. */
  toggleMobileMenu(event: Event) {
    document.querySelector('.hamburger-icon')?.classList.toggle('open');
    event.preventDefault();
    this.mobileMenuButtonClicked.emit();
  }

  fullscreen() {
    document.body.classList.toggle('fullscreen-enable');
    if (!document.fullscreenElement) {
      this.element.requestFullscreen?.();
    } else {
      document.exitFullscreen?.();
    }
  }

  printPage() {
    window.print();
  }

  /** Modo claro/oscuro del topbar. */
  changeMode(mode: string) {
    this.eventService.broadcast('changeMode', mode);
    // Se guarda en el estado del layout: es lo que recuerdan el POS, la recarga y el resto de las pantallas.
    this.store.dispatch(changeMode({ mode: mode === 'dark' ? 'dark' : 'light' }));
  }

  windowScroll() {
    const backToTop = document.getElementById('back-to-top');
    if (document.body.scrollTop > 80 || document.documentElement.scrollTop > 80) {
      if (backToTop) backToTop.style.display = 'block';
      document.getElementById('page-topbar')?.classList.add('topbar-shadow');
    } else {
      if (backToTop) backToTop.style.display = 'none';
      document.getElementById('page-topbar')?.classList.remove('topbar-shadow');
    }
  }

  async changeBranch(branch: BranchOption) {
    if (branch.BranchId === this.auth.user()?.BranchId) return;

    const confirmed = await this.msg.showMessageQuestion(
      `¿Pasar a la sucursal «${branch.Name}»? Las ventas, cajas y movimientos que registre desde ahora quedarán en ella.`);
    if (!confirmed) return;

    if (await this.auth.switchBranch(branch.BranchId)) {
      // Recarga completa: lo que está en pantalla (stock, caja abierta) es de
      // la sucursal anterior, y cada vista lo volvería a pedir por su cuenta.
      window.location.reload();
    }
  }

  async logout() {
    // Primero se cierra la sesión: con el token puesto, el guard del login
    // devolvería al usuario a la app.
    await this.auth.logout();
    await this.router.navigate(['/auth/login']);
  }

  // ── Cambiar contraseña ──────────────────────────────────
  openChangePassword() {
    this.pwd = { current: '', next: '', confirm: '', showCurrent: false, showNext: false };
    this.modalRef = this.modalService.open(this.changePasswordTpl, { centered: true });
  }

  async submitChangePassword() {
    const warn = (Description: string) => this.msg.showMessageModal({ Description, MessageType: 'warning' });

    if (!this.pwd.current) return warn('Ingresa tu contraseña actual.');
    if (this.pwd.next.length < 6) return warn('La nueva contraseña debe tener al menos 6 caracteres.');
    if (this.pwd.next !== this.pwd.confirm) return warn('Las contraseñas no coinciden.');

    const resp = await this.api.put<ResponseObject<boolean>>('Users/me/password', {
      currentPassword: this.pwd.current,
      newPassword: this.pwd.next,
    });
    if (resp.ok) {
      this.modalRef?.close();
      await this.msg.showMessageModal({ Description: 'Contraseña actualizada correctamente.', MessageType: 'success' });
    }
  }

  // ── Dispositivos de confianza ───────────────────────────
  async openDevices() {
    this.modalRef = this.modalService.open(this.trustedDevicesTpl, { centered: true, size: 'lg' });
    await this.loadDevices();
  }

  private async loadDevices() {
    this.devicesLoading = true;
    const resp = await this.api.get<ResponseArray<TrustedDevice>>('Mfa/devices');
    if (resp.ok) this.devices = resp.Data;
    this.devicesLoading = false;
  }

  async forgetDevice(id: number) {
    const confirmed = await this.msg.showMessageQuestion(
      '¿Olvidar este dispositivo? La próxima vez que inicies sesión desde ahí te pedirá el código TOTP de nuevo.');
    if (!confirmed) return;

    const resp = await this.api.del<ResponseObject<boolean>>(`Mfa/devices/${id}`);
    if (resp.ok) await this.loadDevices();
  }

  async forgetAllDevices() {
    const confirmed = await this.msg.showMessageQuestion(
      '¿Olvidar todos tus dispositivos de confianza? Tendrás que ingresar el código TOTP la próxima vez que inicies sesión desde cualquiera de ellos.');
    if (!confirmed) return;

    const resp = await this.api.del<ResponseObject<boolean>>('Mfa/devices');
    if (resp.ok) await this.loadDevices();
  }

  /** Fecha y hora locales: el backend guarda en UTC y el navegador convierte. */
  formatDateTime(date: string): string {
    if (!date) return '—';
    return new Date(date).toLocaleString('es-BO', {
      day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false,
    });
  }
}
