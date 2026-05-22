using System;

namespace Inventory.Domain.Entities.Requests;

public class PurchaseDeliveryRequest
{
    public string Id { get; set; } = "";
    public string PurchaseId { get; set; } = "";
    public bool IsActive { get; set; }
    public string DeliveryDate { get; set; } = "01/01/1900";

    public int PurchaseStatusId { get; set; }
    public List<PurchaseDeliveryDetailRequest> Detail { get; set; } = [];

    public string ProviderName { get; set; } = "";
    public string PurchaseStatusName { get; set; } = "";
}
