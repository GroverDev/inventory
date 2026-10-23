export class DashboardRecentSale {
  Id: string = '';
  CustomerName: string = '';
  SaleDate: string = '';
  Total: number = 0;
}

export class DashboardLowStockProduct {
  Id: string = '';
  ProductName: string = '';
  ProductCode: string = '';
  CurrentStock: number = 0;
  MinReorderQuantity: number = 0;
}

export class DashboardKpi {
  TodaySalesTotal: number = 0;
  TodaySalesCount: number = 0;
  MonthSalesTotal: number = 0;
  MonthSalesCount: number = 0;
  PendingPurchasesCount: number = 0;
  LowStockCount: number = 0;
  RecentSales: DashboardRecentSale[] = [];
  LowStockProducts: DashboardLowStockProduct[] = [];
}
