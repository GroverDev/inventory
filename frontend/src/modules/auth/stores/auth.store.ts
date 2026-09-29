// stores/auth.ts
import { defineStore } from 'pinia'
import { useLocalStorage, useSessionStorage } from '@vueuse/core'
import { computed, readonly, type WritableComputedRef } from 'vue'
import axios from 'axios';

import { useApi } from '@/modules/common/composables/api/useApi';
import { User } from '@/modules/auth/models/user.model';
import type { ResponseArray, ResponseObject } from '@/modules/common/models';
import { AccessMenu } from '@/modules/auth/models/acccessMenu.interface';


export const useAuthStore = defineStore('auth', () => {
  const { post, get } = useApi();
  // "Mantener sesión iniciada": única marca que sobrevive al cierre del
  // navegador. Sin ella nada de la sesión queda en disco, para que en una PC
  // compartida o prestada el siguiente usuario encuentre el login.
  const remember = useLocalStorage<boolean>('auth_remember', false)

  // El JWT nunca va a localStorage: vive en sessionStorage (se borra al cerrar
  // la pestaña). Si la sesión es "recordada", al abrir la web se renueva sola
  // con la cookie HttpOnly de refresh — ver restoreSession().
  const token = useSessionStorage<string | null>('auth_token', null)

  // Usuario y menú no son secretos, pero tampoco deben quedar en un equipo
  // ajeno: van a localStorage solo si la sesión es recordada, para poder
  // rearmar la pantalla tras renovar el token sin volver a pedirlos.
  const sessionOrLocal = <T>(key: string, initial: T): WritableComputedRef<T> => {
    const inSession = useSessionStorage<T>(key, initial)
    const inLocal = useLocalStorage<T>(key, initial)
    return computed({
      get: () => (remember.value ? inLocal.value : inSession.value) as T,
      set: (value: T) => {
        if (remember.value) inLocal.value = value
        else inSession.value = value
      },
    })
  }
  const user = sessionOrLocal<User | null>('auth_user', new User())
  const accessMenuUser = sessionOrLocal<AccessMenu[]>('auth_access_menu', [])

  const pendingUser = useSessionStorage<User | null>('auth_pending_user', null)
  // Elección del login, que debe llegar hasta la verificación del TOTP.
  const pendingRemember = useSessionStorage<boolean>('auth_pending_remember', false)

  // Versiones anteriores guardaban el token en localStorage. Se borra siempre,
  // y lo demás si no hay sesión recordada, para no dejar datos ni tokens
  // olvidados en equipos donde alguien ya inició sesión.
  try {
    localStorage.removeItem('auth_token')
    if (!remember.value) {
      localStorage.removeItem('auth_user')
      localStorage.removeItem('auth_access_menu')
    }
  } catch { /* almacenamiento bloqueado */ }

  // Computed para autenticación
  const isAuthenticated = computed(() => !!token.value)

  // Getters
  const getToken = computed(() => token.value)
  const getUser = computed(() => user.value)
  const getPendingUser = computed(() => pendingUser.value)
  const getPendingRemember = computed(() => pendingRemember.value)
  const getAccessMenu = computed(() => accessMenuUser.value)
  const isLoggedIn = computed(() => isAuthenticated.value)

  // Actions
  const login = async (email: string, password: string, turnstileToken = '', rememberMe = false) => {
    try {

      const responseLogin = await post<ResponseObject<User>>(`Login`,
        {
          UserName: '',
          Email: email,
          Password: password,
          Device: '',
          WithEmail: true,
          // InicioSesionDesde.Web — antes iba 5 (Postman), lo que falseaba la
          // auditoría de accesos en sec.users_login.
          LoginFrom: 1,
          LoginWith: 1,
          // Captcha de Cloudflare. El backend lo exige según la cabecera
          // Origin, no según este cuerpo; si no está configurado viaja vacío.
          TurnstileToken: turnstileToken,
          // Solo la web lo usa (vida de la cookie de refresh); el móvil no lo manda.
          RememberMe: rememberMe
        }
      );

      if (responseLogin.ok) {
        const { Data: newUser } = responseLogin;

        // Caso 1: TOTP ya configurado → redirige a verificar código (tiene TotpSessionToken, no JWT real)
        if (newUser.RequireTotp) {
          pendingUser.value = newUser;
          pendingRemember.value = rememberMe;
          return { success: false, requireTotp: true, totpSetupRequired: false };
        }

        // Caso 2: TOTP no configurado → el JWT real ya vino, pero debe configurarlo ahora
        setAuth(newUser.Token, newUser, rememberMe);
        if (newUser.TotpSetupRequired) {
          return { success: false, requireTotp: true, totpSetupRequired: true };
        }

        return { success: true, user: newUser }
      }
      return { success: false, user: undefined };
    } catch (error) {
      console.error('Error en login:', error)
      throw error
    }
  }

  const getAccessMenuApi = async () => {
    try {
      const respAccesos = await get<ResponseArray<AccessMenu>>(
        `AccessMenu`,
      );
      if (respAccesos.ok) {
        const { Data: newMenu } = respAccesos;
        setAccessMenu(newMenu);

        return { success: true }
      }

    } catch (error) {
      console.error('Error en set accessMenu:', error)
      throw error
    }
  }
  const setAccessMenu = (newAccessMenu: AccessMenu[]) => {
    accessMenuUser.value = newAccessMenu
  }

  const setAuth = (newToken: string, newUser: User, rememberMe = false) => {
    // Primero la marca: decide en qué storage se escriben el usuario y el menú.
    remember.value = rememberMe
    token.value = newToken
    user.value = newUser

    // Configurar el token en axios para futuras peticiones
    axios.defaults.headers.common['Authorization'] = `Bearer ${newToken}`
  }

  /// Renueva solo el access token tras un refresh, conservando el usuario.
  const setToken = (newToken: string) => {
    token.value = newToken
    axios.defaults.headers.common['Authorization'] = `Bearer ${newToken}`
  }

  /// Actualiza la sucursal activa y la lista de habilitadas, conservando el
  /// resto del usuario. Lo usan el cambio de sucursal y el refresh, que puede
  /// devolver otra sucursal si al usuario le quitaron la que tenía.
  const setBranchInfo = (info: Pick<User, 'BranchId' | 'BranchName' | 'Branches'>) => {
    if (!user.value || !info?.BranchId) return
    user.value = {
      ...user.value,
      BranchId: info.BranchId,
      BranchName: info.BranchName,
      Branches: info.Branches ?? [],
    }
  }

  /// Pasa la sesión a otra sucursal. El backend valida que el usuario esté
  /// habilitado y devuelve un token nuevo con esa sucursal.
  const switchBranch = async (branchId: string): Promise<boolean> => {
    const resp = await post<ResponseObject<User>>('Login/switch-branch', { BranchId: branchId });
    if (!resp.ok) return false

    setToken(resp.Data.Token)
    setBranchInfo(resp.Data)
    return true
  }

  /// Vuelve a pedir las sucursales habilitadas (por ejemplo tras crear una).
  /// Renovar la sesión las trae sin necesitar otro endpoint.
  const refreshBranches = async (): Promise<boolean> => {
    const { tryRefreshSession } = await import('@/modules/common/composables/api/refreshSession')
    return tryRefreshSession()
  }

  const completarTotp = (newUser: User) => {
    setAuth(newUser.Token, newUser, pendingRemember.value);
    pendingUser.value = null;
    pendingRemember.value = false;
  }

  /// Al abrir la web sin token en esta pestaña: si la sesión fue recordada,
  /// intenta renovarla en silencio con la cookie de refresh. Devuelve si
  /// quedó autenticado. Si falla (cookie vencida o revocada) limpia todo.
  const restoreSession = async (): Promise<boolean> => {
    if (token.value) return true
    if (!remember.value || !user.value?.Email) return false

    const { tryRefreshSession } = await import('@/modules/common/composables/api/refreshSession')
    if (await tryRefreshSession()) return true

    clearSession()
    return false
  }

  /// Limpia la sesión solo en el navegador, sin llamar al servidor. Se usa
  /// cuando la sesión ya está muerta y revocar no aportaría nada.
  const clearSession = () => {
    token.value = null
    user.value = null
    accessMenuUser.value = [];
    pendingUser.value = null
    pendingRemember.value = false
    // Después de vaciar user/menú: mientras la marca siga puesta, esos
    // valores se escriben en localStorage, que es donde hay que borrarlos.
    remember.value = false
    for (const key of ['auth_user', 'auth_access_menu']) {
      try { localStorage.removeItem(key); sessionStorage.removeItem(key) } catch { /* bloqueado */ }
    }

    // Remover header de autorización
    delete axios.defaults.headers.common['Authorization']
  }

  /// `keepDevice` es para el cierre automático (inactividad): revoca solo la
  /// sesión y deja el equipo como de confianza. El botón "Cerrar sesión" no lo
  /// usa, y ahí el servidor también olvida el equipo.
  const logout = async (keepDevice = false) => {
    // Revoca el refresh token en el servidor y borra la cookie: sin esto la
    // sesión seguiría viva del lado del backend aunque el navegador la olvide.
    try {
      await post(`Login/revoke`, { RefreshToken: '', KeepDevice: keepDevice });
    } catch {
      // Sin red o sesión ya vencida: igual se limpia el navegador.
    }

    clearSession();
  }

  // Verificar si el token sigue siendo válido
  // const verifyToken = async () => {
  //   if (!token.value) return false

  //   try {
  //     const response = await axios.get('/api/auth/verify')
  //     return response.status === 200
  //   } catch (error) {
  //     logout()
  //     return false
  //   }
  // }

  // Inicializar el store cuando la app se carga
  // const initializeAuth = () => {
  //   if (token.value) {
  //     axios.defaults.headers.common['Authorization'] = `Bearer ${token.value}`
  //   }
  // }

  return {
    // State
    token: readonly(token),
    user: readonly(user),
    accessMenuUser: readonly(accessMenuUser),
    isAuthenticated,
    remember: readonly(remember),

    // Getters
    getToken,
    getUser,
    getPendingUser,
    getPendingRemember,
    getAccessMenu,
    isLoggedIn,

    // Actions
    login,
    getAccessMenuApi,
    setAuth,
    setToken,
    setAccessMenu,
    setBranchInfo,
    switchBranch,
    refreshBranches,
    clearSession,
    completarTotp,
    restoreSession,
    logout,
    //verifyToken,
    //initializeAuth
  }
})
