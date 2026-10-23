import { Route } from '@angular/router';

/** Rutas planas, con el mismo nombre que las del frontend Vue (coinciden con `url` del menú). */
export const INVENTORY_ROUTES: Route[] = [
  {
    path: 'products-admin',
    title: 'PV - Registro de Productos',
    loadComponent: () => import('./products/products-admin.component').then(m => m.ProductsAdminComponent),
  },
  {
    path: 'product-edit/:id',
    title: 'PV - Registro de Productos',
    loadComponent: () => import('./products/product-edit.component').then(m => m.ProductEditComponent),
  },
  {
    path: 'purchases-admin',
    title: 'PV - Registro de Compras',
    loadComponent: () => import('./purchases/purchases-admin.component').then(m => m.PurchasesAdminComponent),
  },
  {
    path: 'purchase-edit/:id',
    title: 'PV - Registro de Compras',
    loadComponent: () => import('./purchases/purchase-edit.component').then(m => m.PurchaseEditComponent),
  },
  {
    path: 'purchase-receive/:id',
    title: 'PV - Recepción de Compras',
    loadComponent: () => import('./purchases/purchase-receive.component').then(m => m.PurchaseReceiveComponent),
  },
  {
    path: 'inventory-stock',
    title: 'PV - Control de Stock',
    loadComponent: () => import('./stock/inventory-stock.component').then(m => m.InventoryStockComponent),
  },
  {
    path: 'stock-history/:id',
    title: 'PV - Historial de Movimientos',
    loadComponent: () => import('./stock/stock-history.component').then(m => m.StockHistoryComponent),
  },
  {
    path: 'stock-adjustment/:id',
    title: 'PV - Ajuste de Stock',
    loadComponent: () => import('./stock/stock-adjustment.component').then(m => m.StockAdjustmentComponent),
  },
  {
    path: 'stock-expiry',
    title: 'PV - Vencimientos',
    loadComponent: () => import('./stock/stock-expiry.component').then(m => m.StockExpiryComponent),
  },
  {
    path: 'stock-traceability',
    title: 'PV - Trazabilidad de Lote',
    loadComponent: () => import('./stock/stock-traceability.component').then(m => m.StockTraceabilityComponent),
  },
  {
    path: 'stock-write-off/:id',
    title: 'PV - Baja de Stock',
    loadComponent: () => import('./stock/stock-write-off.component').then(m => m.StockWriteOffComponent),
  },
  {
    path: 'stock-transfers',
    title: 'PV - Traspasos entre Sucursales',
    loadComponent: () => import('./transfers/stock-transfers.component').then(m => m.StockTransfersComponent),
  },
  {
    path: 'stock-transfer/:id',
    title: 'PV - Traspaso',
    loadComponent: () => import('./transfers/stock-transfer.component').then(m => m.StockTransferComponent),
  },
  {
    path: 'sales-admin',
    title: 'PV - Registro de Ventas',
    loadComponent: () => import('./sales/sales-admin.component').then(m => m.SalesAdminComponent),
  },
  {
    path: 'sale-detail/:id',
    title: 'PV - Detalle de Venta',
    loadComponent: () => import('./sales/sale-detail.component').then(m => m.SaleDetailComponent),
  },
  {
    path: 'discounts-admin',
    title: 'PV - Gestión de Descuentos',
    loadComponent: () => import('./discounts/discounts-admin.component').then(m => m.DiscountsAdminComponent),
  },
  {
    path: 'discount-limits',
    title: 'PV - Límites de Descuento',
    loadComponent: () => import('./discounts/discount-limits.component').then(m => m.DiscountLimitsComponent),
  },
  {
    path: 'cash-sessions',
    title: 'PV - Turnos de Caja',
    loadComponent: () => import('./cash/cash-sessions-admin.component').then(m => m.CashSessionsAdminComponent),
  },
  {
    path: 'cash-close-settings',
    title: 'PV - Configuración de Cierre de Caja',
    loadComponent: () => import('./cash/cash-close-settings.component').then(m => m.CashCloseSettingsComponent),
  },
  {
    path: 'report-sales',
    title: 'PV - Reporte de Ventas',
    loadComponent: () => import('../reports/sales-report.component').then(m => m.SalesReportComponent),
  },
  {
    path: 'report-stock',
    title: 'PV - Reporte de Stock',
    loadComponent: () => import('../reports/stock-report.component').then(m => m.StockReportComponent),
  },
  {
    path: 'report-purchases',
    title: 'PV - Reporte de Compras',
    loadComponent: () => import('../reports/purchases-report.component').then(m => m.PurchasesReportComponent),
  },
  {
    path: 'report-write-offs',
    title: 'PV - Reporte de Mermas',
    loadComponent: () => import('../reports/write-off-report.component').then(m => m.WriteOffReportComponent),
  },
];
