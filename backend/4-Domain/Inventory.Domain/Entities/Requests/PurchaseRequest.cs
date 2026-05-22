using System;

namespace Inventory.Domain.Entities.Requests;

public class PurchaseRequest
{
    public string Id { get; set; } = "";
    public string PurchaseDate { get; set; } = "";
    public decimal Total { get; set; }
    public bool IsActive { get; set; }
    public string ProviderId { get; set; } = "";
    public string EstimatedDeliveryDate { get; set; } = "01/01/1900";

    public int PurchaseStatusId { get; set; }
    public List<PurchaseDetailRequest> Detail { get; set; } =[];

    public string ProviderName { get; set; } = "";
    public string PurchaseStatusName { get; set; } = "";
}
