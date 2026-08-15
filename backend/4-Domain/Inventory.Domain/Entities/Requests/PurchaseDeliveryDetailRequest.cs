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

    /// <summary>Lote recibido. Obligatorio si el producto usa lotes.</summary>
    public string? LotCode { get; set; }

    /// <summary>Vencimiento del lote, en formato dd/MM/yyyy. Opcional.</summary>
    public string? ExpiryDate { get; set; }
}
