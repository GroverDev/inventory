namespace Inventory.Domain;

public class SaleProductResponse
{
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public DateTime SaleDate { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal Total { get; set; }
        public bool IsActive { get; set; }
        public List<SaleProductDetailResponse> Detail { get; set; } = [];
        public List<SalePaymentResponse> Payments { get; set; } = [];
        public List<SaleReturnResponse> Returns { get; set; } = [];
}


