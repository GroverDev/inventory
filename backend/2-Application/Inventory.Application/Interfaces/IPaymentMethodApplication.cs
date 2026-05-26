using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application.Interfaces;

public interface IPaymentMethodApplication
{
    Task<Response<List<PaymentMethod>>> GetPaymentMethods();
}
