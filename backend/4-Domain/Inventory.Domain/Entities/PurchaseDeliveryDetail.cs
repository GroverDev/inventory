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

}
