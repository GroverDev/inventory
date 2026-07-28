using System;

namespace Inventory.Domain.Entities.Requests;

public class PurchaseDeliveryDetailRequest
{
    public string Id { get; set; } = Guid.Empty.ToString();
    public string PurchaseDeliveryId { get; set; } = Guid.Empty.ToString();
    public string ProductId { get; set; } = "";

    public int DeliveryQuantity { get; set; }
    public int OrderedQuantity { get; set; }

    /// <summary>Precio unitario facturado por el proveedor en esta entrega.</summary>
    public decimal UnitPrice { get; set; }
}
