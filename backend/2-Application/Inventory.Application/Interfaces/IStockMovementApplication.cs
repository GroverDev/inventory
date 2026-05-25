using Common.Utilities;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;

namespace Inventory.Application;

public interface IStockMovementApplication
{
    Task<Response<List<StockMovementResponse>>> GetMovementsByProduct(string productId);
    Task<Response<bool>> CreateAdjustment(StockAdjustmentRequest request, int userId);
}
