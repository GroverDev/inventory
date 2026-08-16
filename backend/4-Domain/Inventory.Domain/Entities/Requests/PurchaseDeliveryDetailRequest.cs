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

    /// <summary>
    /// Vencimiento del lote. Opcional. Se parsea con la cultura del servidor, así
    /// que el web manda ISO (yyyy-MM-dd), que toda cultura interpreta igual.
    /// </summary>
    public string? ExpiryDate { get; set; }

    /// <summary>
    /// Números de serie recibidos, uno por unidad. El servidor exige que sean
    /// tantos como unidades cuando el producto se identifica por serie.
    /// </summary>
    public List<string>? SerialNumbers { get; set; }
}
