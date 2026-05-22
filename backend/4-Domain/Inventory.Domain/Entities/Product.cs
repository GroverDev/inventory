using Common.Utilities;

namespace Inventory.Domain;

public class Product : Audit
{
        public Guid Id { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";

        public string Description { get; set; } = "";
        public decimal SalePrice { get; set; }
        public Guid UomId { get; set; }
        public int CurrentStock { get; set; }
        public bool IsActive { get; set; }
        public int MinReorderQuantity { get; set; }
        public bool AvailableInPos { get; set; }
        public Guid LaboratoryId { get; set; }
        public string BarCode { get; set; } = "";
      
}
