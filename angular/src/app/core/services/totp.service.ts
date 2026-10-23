import { Injectable, inject } from '@angular/core';

import { ResponseObject } from '../models/api.models';
import { User } from '../models/user.model';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';

export interface TotpSetupData {
  QrCodeBase64: string;
  SecretKey: string;
}

export interface MfaEnableData {
  RecoveryCodes: string[];
}

@Injectable({ providedIn: 'root' })
export class TotpService {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  /**
   * Verifica el código TOTP del login. La casilla "mantener sesión y recordar
   * este equipo" se marcó en el login y vale para sesión de 30 días y para
   * dispositivo de confianza.
   */
  verifyAndComplete(code: string): Promise<boolean> {
    return this.verify('Mfa/verify', { TotpCode: code });
  }

  verifyWithRecovery(recoveryCode: string): Promise<boolean> {
    return this.verify('Mfa/verify-recovery', { RecoveryCode: recoveryCode });
  }

  /** Paso 1: QR y clave para configurar el TOTP. */
  setupTotp() {
    return this.api.get<ResponseObject<TotpSetupData>>('Mfa/setup');
  }

  /** Paso 2: confirma el código para activarlo y devuelve los códigos de recuperación. */
  async enableTotp(code: string): Promise<{ ok: boolean; recoveryCodes: string[] }> {
    const resp = await this.api.post<ResponseObject<MfaEnableData>>('Mfa/enable', { Code: code });
    return { ok: resp.ok, recoveryCodes: resp.Data?.RecoveryCodes ?? [] };
  }

  private async verify(endpoint: string, credential: Record<string, string>): Promise<boolean> {
    const remember = this.auth.pendingRemember();
    const resp = await this.api.post<ResponseObject<User>>(endpoint, {
      TotpSessionToken: this.auth.pendingUser()?.TotpSessionToken ?? '',
      ...credential,
      RememberMe: remember,
      RememberDevice: remember,
    });

    if (!resp.ok || !resp.Data?.Token) return false;
    this.auth.completarTotp(resp.Data);
    await this.auth.getAccessMenuApi();
    return true;
  }
}
