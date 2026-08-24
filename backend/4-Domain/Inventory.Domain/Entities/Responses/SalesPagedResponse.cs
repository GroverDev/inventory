namespace Inventory.Domain;

public class SalesPagedResponse
{
    public List<SaleProductResponse> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public decimal PeriodSubtotal { get; set; }
    public decimal PeriodDiscounts { get; set; }
    public decimal PeriodTotal { get; set; }

    /// <summary>Lo devuelto en el período (sale_returns), que PeriodTotal no descuenta.</summary>
    public decimal PeriodReturned { get; set; }

    /// <summary>PeriodTotal menos PeriodReturned: lo que quedó efectivamente cobrado.</summary>
    public decimal PeriodNet { get; set; }
}
