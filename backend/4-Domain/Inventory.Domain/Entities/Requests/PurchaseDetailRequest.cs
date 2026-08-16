using System;

namespace Inventory.Domain.Entities.Requests;

public class PurchaseDetailRequest
{
    public string Id { get; set; } = "";
    public string PurchaseId { get; set; } = "";
    public string ProductId { get; set; } ="";
    public decimal OrderUnitPrice { get; set; }
    public int OrderedQuantity { get; set; }
    public decimal OrderFinalPrice { get; set; }
    public decimal DeliveryUnitPrice { get; set; }
    public int DeliveredQuantity { get; set; }
    public decimal DeliveryFinalPrice { get; set; }
    public int PurchaseStatusId { get; set; }

    public string ProductName { get; set; } = "";

    /// <summary>Acumulado recibido, sumado desde el log de recepciones.</summary>
    public int ReceivedQuantity { get; set; }

    /// <summary>Saldo por recibir. Es el tope de la próxima recepción.</summary>
    public int PendingQuantity { get; set; }

    /// <summary>
    /// Seguimiento del producto: 'none', 'lot' o 'serial'. Tiene que estar acá y
    /// no solo en <c>PurchaseProductDetailResponse</c> porque la consulta de la
    /// orden se sirve mapeada con <c>Adapt&lt;PurchaseRequest&gt;()</c>: lo que
    /// no exista en este DTO no llega al cliente, y sin él la recepción no sabe
    /// a qué línea pedirle el lote.
    /// </summary>
    public string TrackingMode { get; set; } = "none";
}
