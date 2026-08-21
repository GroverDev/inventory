namespace Inventory.Domain;

public class SaleRequest
{
        public string Id { get; set; } = "";
        public string CustomerId { get; set; } = Guid.Empty.ToString();
        public string SaleDate { get; set; } = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss");
        public decimal Subtotal { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal Total { get; set; }
        public bool IsActive { get; set; }
        public string CashSessionId { get; set; } = "";
        public string HeaderDiscountId     { get; set; } = "";
        public decimal HeaderDiscountAmount { get; set; }
        public string HeaderDiscountType   { get; set; } = "";
        public decimal HeaderDiscountValue { get; set; }
        public string SupervisorAuthToken  { get; set; } = "";

        public List<SaleDetailRequest> Detail { get; set; } = [];
        public List<SalePaymentRequest> Payments { get; set; } = [];
}
