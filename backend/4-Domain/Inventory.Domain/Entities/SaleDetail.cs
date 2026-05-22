using Common.Utilities;

namespace Inventory.Domain;


public class SaleDetail : Audit
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal LineTotalDiscounts { get; set; }
    public decimal LineTotal { get; set; }
   
}

