using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface ICashMovementApplication
{
    Task<Response<string>> CreateMovement(CashMovementRequest request, int userId);
}
