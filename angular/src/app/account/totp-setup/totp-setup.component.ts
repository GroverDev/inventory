import { Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { TotpService, TotpSetupData } from '../../core/services/totp.service';
import { OtpInputComponent } from '../otp-input/otp-input.component';

@Component({
  selector: 'app-totp-setup',
  imports: [RouterLink, OtpInputComponent],
  templateUrl: './totp-setup.component.html',
})
export class TotpSetupComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly totp = inject(TotpService);

  private readonly otp = viewChild(OtpInputComponent);

  /** 1 = escanear QR, 2 = confirmar código, 3 = códigos de recuperación. */
  readonly step = signal(1);
  readonly loading = signal(false);
  readonly setup = signal<TotpSetupData | null>(null);
  readonly copied = signal(false);

  readonly code = signal('');
  readonly hasError = signal(false);
  readonly confirming = signal(false);

  readonly recoveryCodes = signal<string[]>([]);
  readonly codesCopied = signal(false);

  async ngOnInit() {
    // El JWT real ya se emitió al iniciar sesión; sin él no hay nada que configurar.
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/auth/login'], { replaceUrl: true });
      return;
    }
    this.loading.set(true);
    const res = await this.totp.setupTotp();
    this.loading.set(false);
    if (res.ok) this.setup.set(res.Data ?? null);
  }

  async copySecret() {
    const secret = this.setup()?.SecretKey;
    if (!secret) return;
    await navigator.clipboard.writeText(secret);
    this.flag(this.copied);
  }

  async confirm() {
    if (this.code().length < 6 || this.confirming()) return;
    this.confirming.set(true);
    this.hasError.set(false);

    const res = await this.totp.enableTotp(this.code());
    this.confirming.set(false);

    if (res.ok) {
      this.recoveryCodes.set(res.recoveryCodes);
      this.step.set(3);
    } else {
      this.hasError.set(true);
      this.otp()?.clear();
    }
  }

  async copyCodes() {
    await navigator.clipboard.writeText(this.recoveryCodes().join('\n'));
    this.flag(this.codesCopied);
  }

  downloadCodes() {
    const blob = new Blob([this.recoveryCodes().join('\n')], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'recovery-codes.txt';
    a.click();
    URL.revokeObjectURL(url);
  }

  async finish() {
    // El menú aún no se había pedido: el login quedó a mitad del TOTP.
    await this.auth.getAccessMenuApi();
    await this.router.navigateByUrl('/', { replaceUrl: true });
  }

  private flag(target: { set(v: boolean): void }) {
    target.set(true);
    setTimeout(() => target.set(false), 2000);
  }
}
