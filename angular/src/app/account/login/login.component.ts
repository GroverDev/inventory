import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { TurnstileComponent, TurnstileStatus } from '../turnstile/turnstile.component';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, TurnstileComponent],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  private readonly turnstile = viewChild(TurnstileComponent);

  // Sin site key configurada el widget no se muestra y el login funciona como
  // siempre: quien exige el token es el backend, según la cabecera Origin.
  readonly turnstileSiteKey = environment.turnstileSiteKey;
  readonly turnstileToken = signal('');
  readonly turnstileStatus = signal<TurnstileStatus>('pending');

  /**
   * Solo se espera al captcha mientras esté resolviéndose. Si Cloudflare no
   * carga, el formulario se habilita igual: el backend exige el token
   * únicamente ante señal de abuso, así que el login limpio no depende del widget.
   */
  readonly canSubmit = computed(() =>
    !this.turnstileSiteKey || this.turnstileToken() !== '' || this.turnstileStatus() === 'unavailable');

  readonly year = new Date().getFullYear();
  readonly showPassword = signal(false);
  readonly submitted = signal(false);
  readonly loading = signal(false);

  readonly form = this.fb.nonNullable.group({
    usuario: ['', [Validators.required, Validators.email]],
    contrasenia: ['', Validators.required],
    // Desmarcado por defecto: sin él la sesión muere al cerrar el navegador.
    rememberMe: [false],
  });

  get f() {
    return this.form.controls;
  }

  async onSubmit() {
    this.submitted.set(true);
    if (this.form.invalid || this.loading() || !this.canSubmit()) return;

    this.loading.set(true);
    const { usuario, contrasenia, rememberMe } = this.form.getRawValue();
    try {
      const result = await this.auth.login(usuario.trim(), contrasenia.trim(), this.turnstileToken(), rememberMe);

      if (result.success) {
        await this.auth.getAccessMenuApi();
        await this.router.navigateByUrl('/');
        return;
      }
      if (result.requireTotp) {
        await this.router.navigate(['/auth', result.totpSetupRequired ? 'totp-setup' : 'totp']);
        return;
      }
      // El token de Turnstile es de un solo uso: si el login no prosperó hay
      // que pedir uno nuevo o el siguiente intento fallaría por token duplicado.
      this.turnstile()?.reset();
    } finally {
      this.loading.set(false);
    }
  }
}
