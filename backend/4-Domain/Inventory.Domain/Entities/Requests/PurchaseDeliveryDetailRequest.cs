using System;

namespace Inventory.Domain.Entities.Requests;

public class PurchaseDeliveryDetailRequest
{
 public string Id { get; set; } = "";
    public string PurchaseDeliveryId { get; set; } = "";
    public string ProductId { get; set; } ="";

    public int DeliveryQuantity { get; set; }
	public int OrderedQuantity { get; set; }
}
