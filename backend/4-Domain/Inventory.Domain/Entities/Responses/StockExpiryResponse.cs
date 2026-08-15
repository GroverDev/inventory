namespace Inventory.Domain;

/// <summary>Una existencia con vencimiento, clasificada por urgencia.</summary>
public class StockExpiryResponse
{
    public Guid StockItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string LotCode { get; set; } = "";
    public DateTime ExpiryDate { get; set; }
    public decimal Quantity { get; set; }

    /// <summary>Negativo si ya venció.</summary>
    public int DiasRestantes { get; set; }

    /// <summary>VENCIDO | CRITICO (30 días) | PROXIMO (90 días) | VIGENTE.</summary>
    public string Estado { get; set; } = "";

    /// <summary>A precio de venta: lo que se pierde si no se rota a tiempo.</summary>
    public decimal ValorEnRiesgo { get; set; }
}
