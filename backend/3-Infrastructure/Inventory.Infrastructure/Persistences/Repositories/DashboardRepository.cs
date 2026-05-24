using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class DashboardRepository(InventoryDbContext _DbContext) : IDashboardRepository
{
    private class SalesKpiRow
    {
        public decimal KpiTotal { get; set; }
        public int KpiCount { get; set; }
    }

    public async Task<DashboardResponse> GetDashboard()
    {
        DashboardResponse dashboard = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();

            // Rangos calculados en C# para evitar diferencias de zona horaria con DATE()
            var todayStart = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);
            var todayEnd   = DateTime.SpecifyKind(DateTime.Today.AddDays(1), DateTimeKind.Unspecified);
            var monthStart = DateTime.SpecifyKind(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTimeKind.Unspecified);

            // Ventas del día
            var todayKpi = await db.QueryFirstAsync<SalesKpiRow>(@"
                SELECT COALESCE(SUM(total), 0)          AS KpiTotal,
                       CAST(COUNT(*) AS INTEGER)         AS KpiCount
                  FROM sales
                 WHERE state
                   AND sale_date >= @Start
                   AND sale_date  < @End;",
                new { Start = todayStart, End = todayEnd });

            dashboard.TodaySalesTotal = todayKpi.KpiTotal;
            dashboard.TodaySalesCount = todayKpi.KpiCount;

            // Ventas del mes
            var monthKpi = await db.QueryFirstAsync<SalesKpiRow>(@"
                SELECT COALESCE(SUM(total), 0)          AS KpiTotal,
                       CAST(COUNT(*) AS INTEGER)         AS KpiCount
                  FROM sales
                 WHERE state
                   AND sale_date >= @Start
                   AND sale_date  < @End;",
                new { Start = monthStart, End = todayEnd });

            dashboard.MonthSalesTotal = monthKpi.KpiTotal;
            dashboard.MonthSalesCount = monthKpi.KpiCount;

            // Compras pendientes (REQUESTED = 1)
            var pending = await db.QueryFirstAsync<SalesKpiRow>(@"
                SELECT 0                                AS KpiTotal,
                       CAST(COUNT(*) AS INTEGER)        AS KpiCount
                  FROM purchases
                 WHERE state AND is_active AND purchase_status_id = 1;");
            dashboard.PendingPurchasesCount = pending.KpiCount;

            // Productos con stock bajo mínimo
            var lowStock = await db.QueryFirstAsync<SalesKpiRow>(@"
                SELECT 0                                AS KpiTotal,
                       CAST(COUNT(*) AS INTEGER)        AS KpiCount
                  FROM products
                 WHERE state AND is_active AND current_stock < min_reorder_quantity;");
            dashboard.LowStockCount = lowStock.KpiCount;

            // Últimas 5 ventas del día
            var recentSales = await db.QueryAsync<DashboardRecentSale>(@"
                SELECT s.id, c.full_name AS CustomerName, s.sale_date, s.total
                  FROM sales s
                 INNER JOIN customers c ON c.id = s.customer_id
                 WHERE s.state
                   AND s.sale_date >= @Start
                   AND s.sale_date  < @End
                 ORDER BY s.sale_date DESC
                 LIMIT 5;",
                new { Start = todayStart, End = todayEnd });
            dashboard.RecentSales = recentSales.ToList();

            // Top 5 productos con stock crítico
            var criticalStock = await db.QueryAsync<DashboardLowStockProduct>(@"
                SELECT p.id, p.product_name, p.product_code, p.current_stock, p.min_reorder_quantity
                  FROM products p
                 WHERE p.state AND p.is_active AND p.current_stock < p.min_reorder_quantity
                 ORDER BY p.current_stock ASC
                 LIMIT 5;");
            dashboard.LowStockProducts = criticalStock.ToList();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<DashboardResponse>(ex); }
        finally { db.Close(); }

        return dashboard;
    }
}
