using Common.Utilities;

namespace Inventory.Domain;

public class PurchaseDeliveryDetail:Audit
{
    public Guid Id { get; set; }

    public Guid PurchaseDeliveryId { get; set; }
    public Guid ProductId { get; set; }

    public DateOnly DeliveryDate { get; set; }
    public int DeliveryQuantity { get; set; }
	public int OrderedQuantity { get; set; }

    /// <summary>Precio unitario efectivamente facturado en esta recepción.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Lote que trae la mercadería. Obligatorio si el producto usa seguimiento por
    /// lotes: la recepción es el momento en que la caja física entra a la farmacia
    /// con su etiqueta, y no hay otro momento para capturarlo.
    /// </summary>
    public string? LotCode { get; set; }

    /// <summary>Vencimiento del lote o de la unidad. Opcional.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Números de serie recibidos, uno por unidad. Obligatorio —y de la misma
    /// cantidad que <see cref="DeliveryQuantity"/>— si el producto usa
    /// seguimiento por series. Vacío en cualquier otro modo.
    /// </summary>
    public List<string> SerialNumbers { get; set; } = [];

    public decimal FinalPrice => DeliveryQuantity * UnitPrice;
}
