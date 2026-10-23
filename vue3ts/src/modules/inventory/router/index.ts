import type { RouteRecordRaw } from 'vue-router'

/**
 * Rutas del módulo inventory. El `name` de cada ruta coincide con el `url` del formulario en el
 * menú de accesos, que también es la clave de sus permisos.
 * Generado desde frontend/ por la migración: agrega aquí las rutas nuevas a mano.
 */
const routes: RouteRecordRaw[] = [
  {
    path: '/inventory',
    name: 'inventory-dashboard',
    component: () => import('@/modules/inventory/views/DashboardView.vue'),
    meta: { authRequired: true, title: 'Gestión de Inventario' },
  },
  {
    path: '/inventory/products-admin',
    name: 'products-admin',
    component: () => import('@/modules/inventory/views/products/ProductsAdminView.vue'),
    meta: { authRequired: true, title: 'Registro de Productos' },
  },
  {
    path: '/inventory/product-edit/:id',
    name: 'product-edit',
    component: () => import('@/modules/inventory/views/products/ProductEditView.vue'),
    meta: { authRequired: true, title: 'Registro de Productos' },
  },
  {
    path: '/inventory/purchases-admin',
    name: 'purchases-admin',
    component: () => import('@/modules/inventory/views/purchases/PurchasesAdminView.vue'),
    meta: { authRequired: true, title: 'Registro de Compras' },
  },
  {
    path: '/inventory/purchase-edit/:id',
    name: 'purchase-edit',
    component: () => import('@/modules/inventory/views/purchases/PurchaseEditView.vue'),
    meta: { authRequired: true, title: 'Registro de Compras' },
  },
  {
    path: '/inventory/purchase-receive/:id',
    name: 'purchase-receive',
    component: () => import('@/modules/inventory/views/purchases/PurchaseReceiveView.vue'),
    meta: { authRequired: true, title: 'Recepcionar Orden' },
  },
  {
    path: '/inventory/providers-admin',
    name: 'providers-admin',
    component: () => import('@/modules/inventory/views/providers/ProvidersAdminView.vue'),
    meta: { authRequired: true, title: 'Registro de Proveedores' },
  },
  {
    path: '/inventory/provider-edit/:id',
    name: 'provider-edit',
    component: () => import('@/modules/inventory/views/providers/ProviderEditView.vue'),
    meta: { authRequired: true, title: 'Registro de Proveedores' },
  },
  {
    path: '/inventory/customers-admin',
    name: 'customers-admin',
    component: () => import('@/modules/inventory/views/customers/CustomersAdminView.vue'),
    meta: { authRequired: true, title: 'Registro de Clientes' },
  },
  {
    path: '/inventory/customer-edit/:id',
    name: 'customer-edit',
    component: () => import('@/modules/inventory/views/customers/CustomerEditView.vue'),
    meta: { authRequired: true, title: 'Registro de Clientes' },
  },
  {
    path: '/inventory/categories-admin',
    name: 'categories-admin',
    component: () => import('@/modules/inventory/views/categories/CategoriesAdminView.vue'),
    meta: { authRequired: true, title: 'Registro de Categorías' },
  },
  {
    path: '/inventory/category-edit/:id',
    name: 'category-edit',
    component: () => import('@/modules/inventory/views/categories/CategoryEditView.vue'),
    meta: { authRequired: true, title: 'Registro de Categorías' },
  },
  {
    path: '/inventory/laboratories-admin',
    name: 'laboratories-admin',
    component: () => import('@/modules/inventory/views/laboratories/LaboratoriesAdminView.vue'),
    meta: { authRequired: true, title: 'Registro de Laboratorios' },
  },
  {
    path: '/inventory/laboratory-edit/:id',
    name: 'laboratory-edit',
    component: () => import('@/modules/inventory/views/laboratories/LaboratoryEditView.vue'),
    meta: { authRequired: true, title: 'Registro de Laboratorios' },
  },
  {
    path: '/inventory/uom-admin',
    name: 'uom-admin',
    component: () => import('@/modules/inventory/views/uom/UnitOfMeasurementAdminView.vue'),
    meta: { authRequired: true, title: 'Unidades de Medida' },
  },
  {
    path: '/inventory/uom-edit/:id',
    name: 'uom-edit',
    component: () => import('@/modules/inventory/views/uom/UnitOfMeasurementEditView.vue'),
    meta: { authRequired: true, title: 'Unidades de Medida' },
  },
  {
    path: '/inventory/sales-admin',
    name: 'sales-admin',
    component: () => import('@/modules/inventory/views/sales/SalesAdminView.vue'),
    meta: { authRequired: true, title: 'Registro de Ventas' },
  },
  {
    path: '/inventory/sale-detail/:id',
    name: 'sale-detail',
    component: () => import('@/modules/inventory/views/sales/SaleDetailView.vue'),
    meta: { authRequired: true, title: 'Detalle de Venta' },
  },
  {
    path: '/inventory/inventory-stock',
    name: 'inventory-stock',
    component: () => import('@/modules/inventory/views/stock-inventory/InventoryStockView.vue'),
    meta: { authRequired: true, title: 'Control de Stock' },
  },
  {
    path: '/inventory/stock-transfers',
    name: 'stock-transfers',
    component: () => import('@/modules/inventory/views/stock-transfers/StockTransfersView.vue'),
    meta: { authRequired: true, title: 'Traspasos entre Sucursales' },
  },
  {
    path: '/inventory/stock-transfer/:id',
    name: 'stock-transfer',
    component: () => import('@/modules/inventory/views/stock-transfers/StockTransferView.vue'),
    meta: { authRequired: true, title: 'Traspasos entre Sucursales' },
  },
  {
    path: '/inventory/stock-expiry',
    name: 'stock-expiry',
    component: () => import('@/modules/inventory/views/stock-inventory/StockExpiryView.vue'),
    meta: { authRequired: true, title: 'Vencimientos' },
  },
  {
    path: '/inventory/stock-traceability',
    name: 'stock-traceability',
    component: () => import('@/modules/inventory/views/stock-inventory/StockTraceabilityView.vue'),
    meta: { authRequired: true, title: 'Trazabilidad de Lote' },
  },
  {
    path: '/inventory/stock-history/:id',
    name: 'stock-history',
    component: () => import('@/modules/inventory/views/stock-inventory/StockHistoryView.vue'),
    meta: { authRequired: true, title: 'Historial de Movimientos' },
  },
  {
    path: '/inventory/stock-adjustment/:id',
    name: 'stock-adjustment',
    component: () => import('@/modules/inventory/views/stock-inventory/StockAdjustmentView.vue'),
    meta: { authRequired: true, title: 'Ajuste de Stock' },
  },
  {
    path: '/inventory/stock-write-off/:id',
    name: 'stock-write-off',
    component: () => import('@/modules/inventory/views/stock-inventory/StockWriteOffView.vue'),
    meta: { authRequired: true, title: 'Dar de Baja por Vencimiento' },
  },
  {
    path: '/inventory/point-of-sale',
    name: 'point-sale-admin',
    component: () => import('@/modules/inventory/views/point-of-sale/PointOfSaleView.vue'),
    meta: { authRequired: true, title: 'Punto de venta' },
  },
  {
    path: '/inventory/discount-limits',
    name: 'discount-limits',
    component: () => import('@/modules/inventory/views/discounts/DiscountLimitsView.vue'),
    meta: { authRequired: true, title: 'Límites de Descuento' },
  },
  {
    path: '/inventory/discounts-admin',
    name: 'discounts-admin',
    component: () => import('@/modules/inventory/views/discounts/DiscountsAdminView.vue'),
    meta: { authRequired: true, title: 'Gestión de Descuentos' },
  },
  {
    path: '/inventory/cash-close-settings',
    name: 'cash-close-settings',
    component: () => import('@/modules/inventory/views/cash-sessions/CashCloseSettingsView.vue'),
    meta: { authRequired: true, title: 'Configuración de Cierre' },
  },
  {
    path: '/inventory/cash-sessions',
    name: 'cash-sessions',
    component: () => import('@/modules/inventory/views/cash-sessions/CashSessionsAdminView.vue'),
    meta: { authRequired: true, title: 'Turnos de Caja' },
  },
  {
    path: '/pos',
    name: 'pos-sale',
    component: () => import('@/modules/inventory/views/point-of-sale/PointOfSaleView.vue'),
    meta: { authRequired: true, title: 'Gestión de Inventario' },
  },
  {
    path: '/pos/point-of-sale',
    name: 'point-sale-admin',
    component: () => import('@/modules/inventory/views/point-of-sale/PointOfSaleView.vue'),
    meta: { authRequired: true, title: 'Punto de venta' },
  },
]

export default routes
