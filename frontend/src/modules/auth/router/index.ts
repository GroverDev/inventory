
import { isNotAuthenticatedGuard } from '@/guards/authGuard';

export default {
  name: 'auth',
  component: () => import(/* webpackChunkName: "auth" */ '@/modules/auth/layout/AuthLayout.vue'),
  children: [
    {
      path: '',
      name: 'login',
      // Solo en login, no en toda /auth: totp/totp-setup se visitan con un
      // token real ya emitido (2FA obligatorio pendiente de configurar), y
      // si el guard también aplicara ahí, empujaría de vuelta a la app a
      // alguien a mitad de esa configuración obligatoria.
      beforeEnter: isNotAuthenticatedGuard,
      component: () => import(/* webpackChunkName: "login" */ '@/modules/auth/views/LoginView.vue'),
      meta: { title: 'Punto de Venta - Inicio de sesión' },
    },
    {
      path: 'totp',
      name: 'totp',
      component: () => import('@/modules/auth/views/TotpView.vue'),
      meta: { title: 'Punto de Venta - Verificación TOTP' },
    },
    {
      path: 'totp-setup',
      name: 'totp-setup',
      component: () => import('@/modules/auth/views/TotpSetup.vue'),
      meta: { title: 'Punto de Venta - Configurar TOTP' },
    },
  ],
};
