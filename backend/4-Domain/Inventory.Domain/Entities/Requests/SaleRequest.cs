namespace Inventory.Domain;

public class SaleRequest
{
        public string Id { get; set; } = "";
        public string CustomerId { get; set; } = Guid.Empty.ToString();
        public string SaleDate { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        public decimal Subtotal { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal Total { get; set; }
        public bool IsActive { get; set; }
        // public decimal TotalPaidCustomer { get; set; }
        // public decimal TotalReturnedCustomer { get; set; }

        public List<SaleDetailRequest> Detail { get; set; } = [];
}
