namespace Inventory.Domain;

public class SalesPagedResponse
{
    public List<SaleProductResponse> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public decimal PeriodSubtotal { get; set; }
    public decimal PeriodDiscounts { get; set; }
    public decimal PeriodTotal { get; set; }
}
