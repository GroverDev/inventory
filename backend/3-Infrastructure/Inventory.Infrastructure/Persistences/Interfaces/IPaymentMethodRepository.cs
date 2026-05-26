namespace Inventory.Infrastructure;

public interface IPaymentMethodRepository
{
    Task<List<Inventory.Domain.PaymentMethod>> GetPaymentMethods();
}
