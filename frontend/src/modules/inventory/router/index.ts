import { isAuthenticatedGuard } from '@/guards/authGuard';

export default {
  name: 'inventory',
  component: () => import(/* webpackChunkName: "system" */ '@/modules/inventory/layout/InventoryLayout.vue'),
  children: [
    {
      path: '',
      name: 'inventory-dashboard',
      component: () => import(/* webpackChunkName: "main-inventory" */ '@/modules/inventory/views/DashboardView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        title: 'Gestión de Inventario',
        titleForm: 'Gestión de Inventario',
      },
    },
    {
      path: 'products-admin',
      name: 'products-admin',
      component: () => import(/* webpackChunkName: "products-admin" */ '@/modules/inventory/views/products/ProductsAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        //roles: ['admin', 'super-admin']
        title: 'PV - Registro de Productos',
        titleForm: 'Registro de Productos'
      },
    },
    {
      path: 'product-edit/:id',
      name: 'product-edit',
      component: () => import(/* webpackChunkName: "products-edit" */ '@/modules/inventory/views/products/ProductEditView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Registro de Productos',
        titleForm: 'Registro de Productos'
      },
    },
    {
      path: 'purchases-admin',
      name: 'purchases-admin',
      component: () => import(/* webpackChunkName: "purchases-admin" */ '@/modules/inventory/views/purchases/PurchasesAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: { requiresAuth: true, title: 'PV - Registro de Compras', titleForm: 'Registro de Compras' },
    },
    {
      path: 'purchase-edit/:id',
      name: 'purchase-edit',
      component: () => import(/* webpackChunkName: "purchase-edit" */ '@/modules/inventory/views/purchases/PurchaseEditView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: { requiresAuth: true, title: 'PV - Registro de Compras', titleForm: 'Registro de Compras' },
    },
    {
      path: 'purchase-receive/:id',
      name: 'purchase-receive',
      component: () => import(/* webpackChunkName: "purchase-receive" */ '@/modules/inventory/views/purchases/PurchaseReceiveView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: { requiresAuth: true, title: 'PV - Recepcionar Orden', titleForm: 'Recepcionar Orden' },
    },
    {
      path: 'providers-admin',
      name: 'providers-admin',
      component: () => import(/* webpackChunkName: "providers-admin" */ '@/modules/inventory/views/providers/ProvidersAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Registro de Proveedores',
        titleForm: 'Registro de Proveedores',
      },
    },
    {
      path: 'provider-edit/:id',
      name: 'provider-edit',
      component: () => import(/* webpackChunkName: "provider-edit" */ '@/modules/inventory/views/providers/ProviderEditView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Registro de Proveedores',
        titleForm: 'Registro de Proveedores',
      },
    },
    {
      path: 'customers-admin',
      name: 'customers-admin',
      component: () => import(/* webpackChunkName: "customers-admin" */ '@/modules/inventory/views/customers/CustomersAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Registro de Clientes',
        titleForm: 'Registro de Clientes',
      },
    },
    {
      path: 'customer-edit/:id',
      name: 'customer-edit',
      component: () => import(/* webpackChunkName: "customer-edit" */ '@/modules/inventory/views/customers/CustomerEditView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Registro de Clientes',
        titleForm: 'Registro de Clientes',
      },
    },
    {
      path: 'categories-admin',
      name: 'categories-admin',
      component: () => import(/* webpackChunkName: "categories-admin" */ '@/modules/inventory/views/categories/CategoriesAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Registro de Categorías',
        titleForm: 'Registro de Categorías',
      },
    },
    {
      path: 'category-edit/:id',
      name: 'category-edit',
      component: () => import(/* webpackChunkName: "category-edit" */ '@/modules/inventory/views/categories/CategoryEditView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Registro de Categorías',
        titleForm: 'Registro de Categorías',
      },
    },
    {
      path: 'laboratories-admin',
      name: 'laboratories-admin',
      component: () => import(/* webpackChunkName: "laboratories-admin" */ '@/modules/inventory/views/laboratories/LaboratoriesAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Registro de Laboratorios',
        titleForm: 'Registro de Laboratorios',
      },
    },
    {
      path: 'laboratory-edit/:id',
      name: 'laboratory-edit',
      component: () => import(/* webpackChunkName: "laboratory-edit" */ '@/modules/inventory/views/laboratories/LaboratoryEditView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Registro de Laboratorios',
        titleForm: 'Registro de Laboratorios',
      },
    },
    {
      path: 'uom-admin',
      name: 'uom-admin',
      component: () => import(/* webpackChunkName: "uom-admin" */ '@/modules/inventory/views/uom/UnitOfMeasurementAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Unidades de Medida',
        titleForm: 'Unidades de Medida',
      },
    },
    {
      path: 'uom-edit/:id',
      name: 'uom-edit',
      component: () => import(/* webpackChunkName: "uom-edit" */ '@/modules/inventory/views/uom/UnitOfMeasurementEditView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Unidades de Medida',
        titleForm: 'Unidades de Medida',
      },
    },
    {
      path: 'sales-admin',
      name: 'sales-admin',
      component: () => import(/* webpackChunkName: "sales-admin" */ '@/modules/inventory/views/sales/SalesAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: { requiresAuth: true, title: 'PV - Registro de Ventas', titleForm: 'Registro de Ventas' },
    },
    {
      path: 'sale-detail/:id',
      name: 'sale-detail',
      component: () => import(/* webpackChunkName: "sale-detail" */ '@/modules/inventory/views/sales/SaleDetailView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: { requiresAuth: true, title: 'PV - Detalle de Venta', titleForm: 'Detalle de Venta' },
    },
    {
      path: 'inventory-stock',
      name: 'inventory-stock',
      component: () => import(/* webpackChunkName: "inventory-stock" */ '@/modules/inventory/views/stock-inventory/InventoryStockView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: { requiresAuth: true, title: 'PV - Control de Stock', titleForm: 'Control de Stock' },
    },
    {
      path: 'stock-history/:id',
      name: 'stock-history',
      component: () => import(/* webpackChunkName: "stock-history" */ '@/modules/inventory/views/stock-inventory/StockHistoryView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: { requiresAuth: true, title: 'PV - Historial de Movimientos', titleForm: 'Historial de Movimientos' },
    },
    {
      path: 'stock-adjustment/:id',
      name: 'stock-adjustment',
      component: () => import(/* webpackChunkName: "stock-adjustment" */ '@/modules/inventory/views/stock-inventory/StockAdjustmentView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: { requiresAuth: true, title: 'PV - Ajuste de Stock', titleForm: 'Ajuste de Stock' },
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
