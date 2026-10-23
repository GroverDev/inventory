import type { RouteRecordRaw } from 'vue-router'

/**
 * Rutas del módulo reports. El `name` de cada ruta coincide con el `url` del formulario en el
 * menú de accesos, que también es la clave de sus permisos.
 * Generado desde frontend/ por la migración: agrega aquí las rutas nuevas a mano.
 */
const routes: RouteRecordRaw[] = [
  {
    path: '/reports/sales',
    name: 'report-sales',
    component: () => import('@/modules/reports/views/SalesReportView.vue'),
    meta: { authRequired: true, title: 'Reporte de Ventas' },
  },
  {
    path: '/reports/stock',
    name: 'report-stock',
    component: () => import('@/modules/reports/views/StockReportView.vue'),
    meta: { authRequired: true, title: 'Reporte de Stock' },
  },
  {
    path: '/reports/purchases',
    name: 'report-purchases',
    component: () => import('@/modules/reports/views/PurchasesReportView.vue'),
    meta: { authRequired: true, title: 'Reporte de Compras' },
  },
  {
    path: '/reports/write-offs',
    name: 'report-write-offs',
    component: () => import('@/modules/reports/views/WriteOffReportView.vue'),
    meta: { authRequired: true, title: 'Reporte de Mermas' },
  },
]

export default routes
