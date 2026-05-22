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
}
