import { useAuthStore } from "@/modules/auth/stores/auth.store";

//import type { User } from '@/modules/auth/models/user.interface';

export const useAuth = () => {

  const authStore = useAuthStore();

  const loginApp = async (email: string, password: string, turnstileToken = '') => {
    const ok = await authStore.login(email, password, turnstileToken)
    if (ok.success) {
      await authStore.getAccessMenuApi();
    }
    return ok;
  }
  return { loginApp }
}




