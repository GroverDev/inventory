namespace Inventory.Infrastructure;

public interface IPaymentMethodRepository
{
    Task<List<Inventory.Domain.PaymentMethod>> GetPaymentMethods();

    /// <summary>Qué medios se arquean al cierre. El efectivo queda siempre marcado.</summary>
    Task SetRequiresCount(List<(Guid Id, bool RequiresCount)> methods, int userId);
}
