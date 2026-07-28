using System;

namespace Inventory.Domain.Entities.Requests;

public class PurchaseDeliveryRequest
{
    public string Id { get; set; } = Guid.Empty.ToString();
    public string PurchaseId { get; set; } = "";
    public bool IsActive { get; set; }
    public string DeliveryDate { get; set; } = "01/01/1900";

    /// <summary>
    /// Uid de la operación, generado por el cliente al abrir la pantalla.
    /// Hace idempotente el reintento de una recepción.
    /// </summary>
    public string OperationUid { get; set; } = "";

    public List<PurchaseDeliveryDetailRequest> Detail { get; set; } = [];

    /// <summary>
    /// Solo informativo: el estado resultante lo deriva el servidor a partir de
    /// los saldos. Lo que llegue en este campo se ignora.
    /// </summary>
    public int PurchaseStatusId { get; set; }

    public string ProviderName { get; set; } = "";
    public string PurchaseStatusName { get; set; } = "";
}
