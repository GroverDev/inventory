import type { NavigationGuard } from 'vue-router';
import { useAuthStore } from '@/modules/auth/stores/auth.store';

const isAuthenticatedGuard = async (to: any, _: any, next: any) => {

  const authStore = useAuthStore();

  document.title = to.meta.title;
  if (authStore.getToken !== '' && authStore.getToken != null) {
    next();
  } else {
    next({ name: 'login' });
  }
};

/**
 * Si ya hay sesión, no tiene sentido mostrar el login: manda directo a la
 * app. Sin esto, entrar por la raíz (que redirige a /auth) fuerza el
 * formulario aunque el token siga siendo válido.
 */
const isNotAuthenticatedGuard: NavigationGuard = async (_to, _from, next) => {
  const authStore = useAuthStore();
  if (authStore.getToken !== '' && authStore.getToken != null) {
    next({ name: 'inventory-dashboard' });
  } else {
    next();
  }
};

export {
  isAuthenticatedGuard,
  isNotAuthenticatedGuard
}
