namespace Inventory.Domain;

public class PurchaseProductDetailResponse
{
    public Guid Id { get; set; }
    public Guid PurchaseId { get; set; }
    public Guid ProductId { get; set; }
    public decimal OrderUnitPrice { get; set; }
    public int OrderedQuantity { get; set; }
    public decimal OrderFinalPrice { get; set; }
    public decimal DeliveryUnitPrice { get; set; }
    public int DeliveredQuantity { get; set; }
    public decimal DeliveryFinalPrice { get; set; }
    public int PurchaseStatusId { get; set; }
    public string ProductName { get; set; } ="";

    /// <summary>Acumulado recibido, sumado desde el log de recepciones.</summary>
    public int ReceivedQuantity { get; set; }

    /// <summary>Saldo por recibir. Es el tope de la próxima recepción.</summary>
    public int PendingQuantity { get; set; }

    /// <summary>
    /// Seguimiento del producto: 'none', 'lot' o 'serial'. Viaja con el detalle
    /// porque la recepción exige el lote cuando vale 'lot', y sin esto la UI no
    /// tendría cómo saber qué líneas pedirlo antes de enviar.
    /// </summary>
    public string TrackingMode { get; set; } = "none";
}
