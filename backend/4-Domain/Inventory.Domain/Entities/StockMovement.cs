using Common.Utilities;

namespace Inventory.Domain;

public class StockMovement : Audit
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    /// <summary>
    /// Existencia concreta que se movió. Con tracking_mode = 'none' es la única
    /// que tiene el producto; con lotes o series identifica cuál.
    /// </summary>
    public Guid StockItemId { get; set; }

    public string MovementType { get; set; } = "";   // VENTA | COMPRA | AJUSTE
    public int Quantity { get; set; }                // positivo=entrada, negativo=salida
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    public string? Reason { get; set; }
    public string? Observation { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }       // SALE | PURCHASE | null
}
