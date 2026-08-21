using Common.Utilities;

namespace Inventory.Domain;

public class Purchase: Audit
{
     public Guid Id { get; set; }
        public DateOnly PurchaseDate { get; set; }
        public decimal Total { get; set; }
        public bool IsActive { get; set; }

        public Guid ProviderId { get; set; }
        public DateOnly EstimatedDeliveryDate { get; set; }
        public int PurchaseStatusId { get; set; }

        public List<PurchaseDetail> Detail = new List<PurchaseDetail>();
}
