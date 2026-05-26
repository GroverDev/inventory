namespace Inventory.Domain;

public class SalePaymentResponse
{
    public Guid Id { get; set; }
    public Guid PaymentMethodId { get; set; }
    public string PaymentMethodName { get; set; } = "";
    public string IconCss { get; set; } = "";
    public decimal AmountGiven { get; set; }
    public decimal AmountReturned { get; set; }
}
