using Common.Utilities;

namespace Inventory.Domain;

public class StockMovement : Audit
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string MovementType { get; set; } = "";   // VENTA | COMPRA | AJUSTE
    public int Quantity { get; set; }                // positivo=entrada, negativo=salida
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    public string? Reason { get; set; }
    public string? Observation { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }       // SALE | PURCHASE | null
}
