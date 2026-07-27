import { getApi } from '@/modules/common/composables/api/getApi';
import { useAuthStore } from '@/modules/auth/stores/auth.store';

/// InicioSesionDesde.ReconexionWeb en el backend.
const LOGIN_FROM_RECONEXION_WEB = 3;

/**
 * Refresco en curso. Vive a nivel de módulo (no dentro de `useApi`) porque
 * cada componente crea su propia instancia del composable: si varias llamadas
 * caducan a la vez deben compartir un único refresco. Rotar el token en
 * paralelo dispararía la detección de reuso del backend y cerraría la sesión.
 */
let refreshing: Promise<boolean> | null = null;

export const tryRefreshSession = (): Promise<boolean> => {
  if (!refreshing) {
    refreshing = doRefresh().finally(() => {
      refreshing = null;
    });
  }
  return refreshing;
};

/**
 * Sesión irrecuperable: limpia el estado local y lleva al login. Sin esto el
 * usuario quedaría en la pantalla con un token muerto y cada petición
 * fallando, sin entender por qué.
 *
 * No se llama a `logout()` porque ese revoca contra el servidor, y aquí el
 * refresh ya falló: el backend borró la cookie por su cuenta.
 */
export const handleSessionExpired = async (): Promise<void> => {
  useAuthStore().clearSession();

  // Import dinámico: el router arrastra las vistas, que a su vez usan este
  // módulo. Cargarlo en el nivel superior crearía un ciclo de imports.
  const { default: router } = await import('@/router');
  if (router.currentRoute.value.name !== 'login') {
    await router.push({ name: 'login' });
  }
};

const doRefresh = async (): Promise<boolean> => {
  try {
    const api = getApi();
    // El cuerpo va sin token a propósito: el backend lo toma de la cookie.
    const { data } = await api.post('Login/refresh', {
      RefreshToken: '',
      Device: '',
      LoginFrom: LOGIN_FROM_RECONEXION_WEB,
    });

    const newToken = data?.Data?.Token;
    if (data?.ok && newToken) {
      useAuthStore().setToken(newToken);
      return true;
    }
  } catch {
    // Sin red o refresh inválido: se trata como sesión no recuperable.
  }
  return false;
};
