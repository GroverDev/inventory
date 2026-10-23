import { AfterViewInit, Component, OnDestroy, OnInit, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { TotpService } from '../../core/services/totp.service';
import { OtpInputComponent } from '../otp-input/otp-input.component';

@Component({
  selector: 'app-totp',
  imports: [FormsModule, RouterLink, OtpInputComponent],
  templateUrl: './totp.component.html',
})
export class TotpComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly totp = inject(TotpService);
  protected readonly auth = inject(AuthService);

  private readonly otp = viewChild(OtpInputComponent);

  readonly loading = signal(false);
  readonly hasError = signal(false);
  readonly code = signal('');
  readonly timeLeft = signal(30);

  readonly showRecovery = signal(false);
  recoveryCode = '';
  readonly recoveryError = signal(false);

  private timerHandle?: ReturnType<typeof setInterval>;

  ngOnInit() {
    // Sin un login pendiente no hay nada que verificar.
    if (!this.auth.pendingUser()) {
      this.router.navigate(['/auth/login'], { replaceUrl: true });
      return;
    }
    // Los códigos TOTP rotan cada 30 s en punto del reloj.
    const sync = () => this.timeLeft.set(30 - (Math.floor(Date.now() / 1000) % 30));
    sync();
    this.timerHandle = setInterval(sync, 500);
  }

  ngAfterViewInit() {
    this.otp()?.focus(0);
  }

  ngOnDestroy() {
    clearInterval(this.timerHandle);
  }

  async submit() {
    if (this.code().length < 6 || this.loading()) return;
    this.loading.set(true);
    this.hasError.set(false);

    const ok = await this.totp.verifyAndComplete(this.code());
    this.loading.set(false);

    if (ok) {
      await this.router.navigateByUrl('/');
    } else {
      this.hasError.set(true);
      this.otp()?.clear();
    }
  }

  async submitRecovery() {
    if (!this.recoveryCode.trim() || this.loading()) return;
    this.loading.set(true);
    this.recoveryError.set(false);

    const ok = await this.totp.verifyWithRecovery(this.recoveryCode.trim());
    this.loading.set(false);

    if (ok) {
      await this.router.navigateByUrl('/');
    } else {
      this.recoveryError.set(true);
      this.recoveryCode = '';
    }
  }
}
