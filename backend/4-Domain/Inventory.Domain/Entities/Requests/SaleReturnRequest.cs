namespace Inventory.Domain;

public class SaleReturnRequest
{
    public string SaleId { get; set; } = "";
    public string? Reason { get; set; }
    public List<SaleReturnDetailRequest> Detail { get; set; } = [];
}

public class SaleReturnDetailRequest
{
    public string SaleDetailId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public int QuantityReturned { get; set; }
    public decimal UnitPrice { get; set; }
}
