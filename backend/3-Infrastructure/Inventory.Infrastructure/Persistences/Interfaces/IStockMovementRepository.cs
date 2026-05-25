using System.Data;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;

namespace Inventory.Infrastructure;

public interface IStockMovementRepository
{
    Task<List<StockMovementResponse>> GetMovementsByProduct(Guid productId);
    Task CreateAdjustment(StockMovement movement, int userId);
    Task InsertMovement(StockMovement movement, IDbConnection db, IDbTransaction transaction);
}
