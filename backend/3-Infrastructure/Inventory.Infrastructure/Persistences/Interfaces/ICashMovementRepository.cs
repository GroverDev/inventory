using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface ICashMovementRepository
{
    Task<Guid> CreateMovement(CashMovement movement);
    Task<List<CashMovementResponse>> GetMovementsBySession(Guid sessionId);
}
