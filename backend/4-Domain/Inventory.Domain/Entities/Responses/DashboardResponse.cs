namespace Inventory.Domain;

public class DashboardResponse
{
    public decimal TodaySalesTotal { get; set; }
    public int TodaySalesCount { get; set; }
    public decimal MonthSalesTotal { get; set; }
    public int MonthSalesCount { get; set; }
    public int PendingPurchasesCount { get; set; }
    public int LowStockCount { get; set; }
    public List<DashboardRecentSale> RecentSales { get; set; } = [];
    public List<DashboardLowStockProduct> LowStockProducts { get; set; } = [];
}

public class DashboardRecentSale
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = "";
    public DateTime SaleDate { get; set; }
    public decimal Total { get; set; }
}

public class DashboardLowStockProduct
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public int CurrentStock { get; set; }
    public int MinReorderQuantity { get; set; }
}
