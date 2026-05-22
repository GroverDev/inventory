

import { createRouter, createWebHistory } from 'vue-router'

import authRouter from '@/modules/auth/router/index';
import commonRouter from '@/modules/common/router/index';
import inventoryRouter from '@/modules/inventory/router/index';
import userAccountRouter from '@/modules/user-account/router/index';
import posRouter from '@/modules/inventory/router/pos_router';


const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/auth',
      ...authRouter,
    },
    {
      path: '/',
      redirect: '/auth', // Redirige a la ruta pública por defecto
    },
    {
      path: '/common',
      ...commonRouter,
    },
    {
      path: '/inventory',
      ...inventoryRouter,
    },
    {
      path: '/pos',
      ...posRouter,
    },
    {
      path: '/account',
      ...userAccountRouter,
    },
    //  {
    //   path: '/:pathMatch(.*)*', // Catch-all route for 404s or unhandled paths
    //   name: 'NotFound',
    //   component: () => import('@/modules/public_access/views/NotFoundView.vue') // Crea un componente NotFound simple
    // }
  ],
})


export default router
