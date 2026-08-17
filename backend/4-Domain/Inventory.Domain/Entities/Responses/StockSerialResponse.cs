namespace Inventory.Domain;

/// <summary>
/// Una unidad serializada disponible para vender. El mostrador elige de acá
/// cuál entrega, en vez de dejar que FEFO decida por él.
/// </summary>
public class StockSerialResponse
{
    public Guid StockItemId { get; set; }
    public string SerialNumber { get; set; } = "";

    /// <summary>Puede no tener: no todo lo serializado vence.</summary>
    public DateTime? ExpiryDate { get; set; }
}
