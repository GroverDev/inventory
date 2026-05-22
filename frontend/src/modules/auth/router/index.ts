
//import { isNotAuthenticatedGuard } from '@/shared/guards/authGuard';

export default {
  name: 'auth',
  component: () => import(/* webpackChunkName: "auth" */ '@/modules/auth/layout/AuthLayout.vue'),
  children: [
    {
      path: '',
      name: 'login',
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
