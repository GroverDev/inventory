namespace Inventory.Domain;

public class SaleDetailRequest
{
    public string Id { get; set; } = "";
    public string SaleId { get; set; } = "";
    public string ProductId { get; set; } ="";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal LineTotalDiscounts { get; set; }
    public decimal LineTotal { get; set; }
    public string DiscountId   { get; set; } = "";
    public string DiscountType { get; set; } = "";   // "Percentage" | "FixedAmount" — solo descuentos manuales
    public decimal DiscountValue { get; set; }
}


   