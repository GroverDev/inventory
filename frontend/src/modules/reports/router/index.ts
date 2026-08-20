import { isAuthenticatedGuard } from '@/guards/authGuard';

export default {
  name: 'reports',
  component: () => import('@/modules/inventory/layout/InventoryLayout.vue'),
  children: [
    {
      path: 'sales',
      name: 'report-sales',
      component: () => import('@/modules/reports/views/SalesReportView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: { requiresAuth: true, title: 'PV - Reporte de Ventas', titleForm: 'Reporte de Ventas' },
    },
    {
      path: 'stock',
      name: 'report-stock',
      component: () => import('@/modules/reports/views/StockReportView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: { requiresAuth: true, title: 'PV - Reporte de Stock', titleForm: 'Reporte de Stock' },
    },
    {
      path: 'purchases',
      name: 'report-purchases',
      component: () => import('@/modules/reports/views/PurchasesReportView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: { requiresAuth: true, title: 'PV - Reporte de Compras', titleForm: 'Reporte de Compras' },
    },
    {
      path: 'write-offs',
      name: 'report-write-offs',
      component: () => import('@/modules/reports/views/WriteOffReportView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: { requiresAuth: true, title: 'PV - Reporte de Mermas', titleForm: 'Reporte de Mermas' },
    },
  ],
};
