// stores/auth.ts
import { defineStore } from 'pinia'
import { useLocalStorage, useStorage, useSessionStorage } from '@vueuse/core'
import { computed, readonly } from 'vue'
import axios from 'axios';

import { useApi } from '@/modules/common/composables/api/useApi';
import { User } from '@/modules/auth/models/user.model';
import type { ResponseArray, ResponseObject } from '@/modules/common/models';
import { AccessMenu } from '@/modules/auth/models/acccessMenu.interface';


export const useAuthStore = defineStore('auth', () => {
  const { post, get } = useApi();
  // Usando VueUse para localStorage reactivo
  const token = useLocalStorage<string | null>('auth_token', null)
  const user = useStorage<User>('auth_user', new User())
  const pendingUser = useSessionStorage<User | null>('auth_pending_user', null)
  //
  const accessMenuUser = useLocalStorage<AccessMenu[]>('auth_access_menu', [])

  // Computed para autenticación
  const isAuthenticated = computed(() => !!token.value)

  // Getters
  const getToken = computed(() => token.value)
  const getUser = computed(() => user.value)
  const getPendingUser = computed(() => pendingUser.value)
  const getAccessMenu = computed(() => accessMenuUser.value)
  const isLoggedIn = computed(() => isAuthenticated.value)

  // Actions
  const login = async (email: string, password: string) => {
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
          LoginWith: 1
        }
      );

      if (responseLogin.ok) {
        const { Data: newUser } = responseLogin;

        // Caso 1: TOTP ya configurado → redirige a verificar código (tiene TotpSessionToken, no JWT real)
        if (newUser.RequireTotp) {
          pendingUser.value = newUser;
          return { success: false, requireTotp: true, totpSetupRequired: false };
        }

        // Caso 2: TOTP no configurado → el JWT real ya vino, pero debe configurarlo ahora
        setAuth(newUser.Token, newUser);
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

  const setAuth = (newToken: string, newUser: User) => {
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

  const completarTotp = (newUser: User) => {
    setAuth(newUser.Token, newUser);
    pendingUser.value = null;
  }

  /// Limpia la sesión solo en el navegador, sin llamar al servidor. Se usa
  /// cuando la sesión ya está muerta y revocar no aportaría nada.
  const clearSession = () => {
    token.value = null
    user.value = null
    pendingUser.value = null
    accessMenuUser.value = [];

    // Remover header de autorización
    delete axios.defaults.headers.common['Authorization']
  }

  const logout = async () => {
    // Revoca el refresh token en el servidor y borra la cookie: sin esto la
    // sesión seguiría viva del lado del backend aunque el navegador la olvide.
    try {
      await post(`Login/revoke`, { RefreshToken: '' });
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

    // Getters
    getToken,
    getUser,
    getPendingUser,
    getAccessMenu,
    isLoggedIn,

    // Actions
    login,
    getAccessMenuApi,
    setAuth,
    setToken,
    setAccessMenu,
    clearSession,
    completarTotp,
    logout,
    //verifyToken,
    //initializeAuth
  }
})
