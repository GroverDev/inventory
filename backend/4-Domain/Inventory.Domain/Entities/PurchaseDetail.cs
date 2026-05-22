using Common.Utilities;

namespace Inventory.Domain;

public  class PurchaseDetail: Audit
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
    }
