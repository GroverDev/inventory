using Common.Utilities;

namespace Inventory.Domain;

public class PurchaseDelivery: Audit
{
    public Guid Id { get; set; }
    public Guid PurchaseId { get; set; }
    public DateTime DeliveryDate { get; set; }

    public bool IsActive { get; set; }
    public int PurchaseStatusId { get; set; }
    public List<PurchaseDeliveryDetail> Detail { get; set; } = [];
}
