using Common.Utilities;

namespace Inventory.Domain;

public class PurchaseDeliveryDetail:Audit
{
    public Guid Id { get; set; }

    public Guid PurchaseDeliveryId { get; set; }
    public Guid ProductId { get; set; }

    public DateTime DeliveryDate { get; set; }
    public int DeliveryQuantity { get; set; }
	public int OrderedQuantity { get; set; }

    /// <summary>Precio unitario efectivamente facturado en esta recepción.</summary>
    public decimal UnitPrice { get; set; }

    public decimal FinalPrice => DeliveryQuantity * UnitPrice;
}
