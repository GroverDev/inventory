namespace Inventory.Domain;

public class SaleProductDetailResponse
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal LineTotalDiscounts { get; set; }
    public decimal LineTotal { get; set; }
    public string ProductName { get; set; } = "";

    /// <summary>Lote del que salió la línea. Vacío si el producto no usa lotes.</summary>
    public string? LotCode { get; set; }

    /// <summary>Vencimiento del lote vendido.</summary>
    public DateTime? ExpiryDate { get; set; }
}
