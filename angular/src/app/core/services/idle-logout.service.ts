import { Injectable, effect, inject } from '@angular/core';
import { Router } from '@angular/router';

import { AuthService } from './auth.service';
import { MsgService } from './msg.service';

/** Minutos sin actividad tras los cuales se cierra una sesión NO recordada. */
const IDLE_MINUTES = 30;

const ACTIVITY_EVENTS = ['mousedown', 'keydown', 'touchstart', 'scroll', 'wheel'] as const;

/**
 * Cierra la sesión tras un rato sin actividad, para que una pestaña olvidada
 * en un equipo compartido no quede abierta. Solo rige para sesiones que el
 * usuario no marcó como "mantener iniciada". Se inicia una vez, desde AppComponent.
 */
@Injectable({ providedIn: 'root' })
export class IdleLogoutService {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);

  private timer: ReturnType<typeof setTimeout> | null = null;
  private lastReset = 0;

  start(): void {
    ACTIVITY_EVENTS.forEach(e => window.addEventListener(e, this.onActivity, { passive: true }));

    // Arranca al iniciar sesión y se detiene al cerrarla.
    effect(() => {
      this.auth.isAuthenticated();
      this.auth.remember();
      this.arm();
    });
  }

  private stop(): void {
    if (this.timer) clearTimeout(this.timer);
    this.timer = null;
  }

  private arm(): void {
    this.stop();
    if (this.auth.isAuthenticated() && !this.auth.remember()) {
      this.timer = setTimeout(() => this.expire(), IDLE_MINUTES * 60 * 1000);
    }
  }

  private async expire(): Promise<void> {
    this.stop();
    if (!this.auth.isAuthenticated()) return;
    // El cierre por inactividad no lo decide el usuario: el equipo conserva su
    // confianza (a diferencia del botón "Cerrar sesión").
    await this.auth.logout(true);
    await this.router.navigate(['/auth/login']);
    this.msg.showMessageModal({
      Description: 'Tu sesión se cerró por inactividad. Inicia sesión nuevamente.',
      MessageType: 'info',
    });
  }

  // Con throttle: mousedown/scroll pueden dispararse muchas veces por segundo.
  private readonly onActivity = (): void => {
    const now = Date.now();
    if (now - this.lastReset < 5000) return;
    this.lastReset = now;
    if (this.timer) this.arm();
  };
}
