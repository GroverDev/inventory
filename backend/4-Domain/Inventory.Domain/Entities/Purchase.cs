using Common.Utilities;

namespace Inventory.Domain;

public class Purchase: Audit
{
     public Guid Id { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal Total { get; set; }
        public bool IsActive { get; set; }

        public Guid ProviderId { get; set; }
        public DateTime EstimatedDeliveryDate { get; set; }
        public int PurchaseStatusId { get; set; }

        public List<PurchaseDetail> Detail = new List<PurchaseDetail>();
}
