import { useApi } from '@/modules/common/composables/api/useApi';
import { useAuthStore } from '@/modules/auth/stores/auth.store';
import type { ResponseObject } from '@/modules/common/models';
import type { User } from '@/modules/auth/models/user.model';

export interface TotpSetupData {
  QrCodeBase64: string;
  SecretKey: string;
}

export interface MfaEnableData {
  RecoveryCodes: string[];
}

export const useTotp = () => {
  const { post, get } = useApi();
  const authStore = useAuthStore();

  // Verify TOTP code during login (user has TOTP configured)
  const verifyAndComplete = async (code: string, rememberDevice = false) => {
    const sessionToken = authStore.getPendingUser?.TotpSessionToken ?? '';
    const response = await post<ResponseObject<User>>('Mfa/verify', {
      TotpSessionToken: sessionToken,
      TotpCode: code,
      RememberDevice: rememberDevice,
    });
    if (response.ok && response.Data?.Token) {
      authStore.completarTotp(response.Data);
      await authStore.getAccessMenuApi();
      return { success: true };
    }
    return { success: false };
  };

  // Verify with recovery code during login
  const verifyWithRecovery = async (recoveryCode: string, rememberDevice = false) => {
    const sessionToken = authStore.getPendingUser?.TotpSessionToken ?? '';
    const response = await post<ResponseObject<User>>('Mfa/verify-recovery', {
      TotpSessionToken: sessionToken,
      RecoveryCode: recoveryCode,
      RememberDevice: rememberDevice,
    });
    if (response.ok && response.Data?.Token) {
      authStore.completarTotp(response.Data);
      await authStore.getAccessMenuApi();
      return { success: true };
    }
    return { success: false };
  };

  // Step 1: get QR code and secret for setup
  const setupTotp = async () => {
    return await get<ResponseObject<TotpSetupData>>('Mfa/setup');
  };

  // Step 2: confirm TOTP code to activate, returns recovery codes
  const enableTotp = async (code: string) => {
    const response = await post<ResponseObject<MfaEnableData>>('Mfa/enable', { Code: code });
    return { ok: response.ok, recoveryCodes: response.Data?.RecoveryCodes ?? [] };
  };

  return { verifyAndComplete, verifyWithRecovery, setupTotp, enableTotp };
};
