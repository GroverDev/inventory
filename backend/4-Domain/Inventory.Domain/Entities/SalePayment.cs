namespace Inventory.Domain;

public class SalePayment
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public decimal AmountGiven { get; set; }
    public decimal AmountReturned { get; set; }
}
