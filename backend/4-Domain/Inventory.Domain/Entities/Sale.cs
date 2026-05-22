using Common.Utilities;

namespace Inventory.Domain;

public class Sale: Audit
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal Total { get; set; }
        public bool IsActive { get; set; }
        public List<SaleDetail> Detail { get; set; } = [];
    }