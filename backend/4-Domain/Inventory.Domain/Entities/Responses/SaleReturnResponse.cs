namespace Inventory.Domain;

public class SaleReturnResponse
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public DateTime ReturnDate { get; set; }
    public string? Reason { get; set; }
    public decimal TotalReturned { get; set; }
    public bool IsFullReturn { get; set; }
    public string PaymentMethodName { get; set; } = "";
    public List<SaleReturnDetailResponse> Detail { get; set; } = [];
}

public class SaleReturnDetailResponse
{
    public Guid Id { get; set; }
    public Guid SaleDetailId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int QuantityReturned { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountShare { get; set; }
    public decimal LineTotal { get; set; }
}
