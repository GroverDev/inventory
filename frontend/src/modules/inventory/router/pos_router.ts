import { isAuthenticatedGuard } from '@/guards/authGuard';

export default {
  name: 'pos',
  component: () => import(/* webpackChunkName: "system" */ '@/modules/inventory/layout/PointOfSaleLayout.vue'),
  children: [
    {
      path: '',
      name: 'pos-sale',
      component: () => import(/* webpackChunkName: "pos-sale" */ '@/modules/inventory/views/point-of-sale/PointOfSaleView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        title: 'Gestión de Inventario',
        titleForm: 'Gestión de Inventario',
      },
    },
    {
      path: 'point-of-sale',
      name: 'point-sale-admin',
      component: () => import(/* webpackChunkName: "point-of-sale" */ '@/modules/inventory/views/point-of-sale/PointOfSaleView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Punto de venta',
        titleForm: 'Punto de venta'
      },
    },

  ],
};
