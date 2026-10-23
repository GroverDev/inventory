import { useAuthStore } from '@/stores/auth'

export const useAuth = () => {
  const authStore = useAuthStore()

  /** Inicia sesión y, si quedó autenticado, carga el menú de accesos del usuario. */
  const loginApp = async (email: string, password: string, turnstileToken = '', rememberMe = false) => {
    const ok = await authStore.login(email, password, turnstileToken, rememberMe)
    if (ok.success) {
      await authStore.getAccessMenuApi()
    }
    return ok
  }
  return { loginApp }
}
