import { useApi } from '@/api/useApi'
import { useAuthStore } from '@/stores/auth'
import type { ResponseObject } from '@/models/response'
import type { User } from '@/models/user'

export interface TotpSetupData {
  QrCodeBase64: string
  SecretKey: string
}

export interface MfaEnableData {
  RecoveryCodes: string[]
}

export const useTotp = () => {
  const { post, get } = useApi()
  const authStore = useAuthStore()

  // La casilla "mantener sesión y recordar este equipo" se marcó en el login y
  // vale para las dos cosas: sesión de 30 días y dispositivo de confianza.
  const verify = async (endpoint: string, body: Record<string, unknown>) => {
    const remember = authStore.getPendingRemember
    const response = await post<ResponseObject<User>>(endpoint, {
      TotpSessionToken: authStore.getPendingUser?.TotpSessionToken ?? '',
      ...body,
      RememberMe: remember,
      RememberDevice: remember,
    })
    if (response.ok && response.Data?.Token) {
      authStore.completarTotp(response.Data)
      await authStore.getAccessMenuApi()
      return { success: true }
    }
    return { success: false }
  }

  /** Verifica el código TOTP durante el login (el usuario ya lo tiene configurado). */
  const verifyAndComplete = (code: string) => verify('Mfa/verify', { TotpCode: code })

  /** Verifica con un código de recuperación durante el login. */
  const verifyWithRecovery = (recoveryCode: string) => verify('Mfa/verify-recovery', { RecoveryCode: recoveryCode })

  /** Paso 1 de la configuración: QR y clave. */
  const setupTotp = () => get<ResponseObject<TotpSetupData>>('Mfa/setup')

  /** Paso 2: confirma el código para activar el TOTP y devuelve los códigos de recuperación. */
  const enableTotp = async (code: string) => {
    const response = await post<ResponseObject<MfaEnableData>>('Mfa/enable', { Code: code })
    return { ok: response.ok, recoveryCodes: response.Data?.RecoveryCodes ?? [] }
  }

  return { verifyAndComplete, verifyWithRecovery, setupTotp, enableTotp }
}
