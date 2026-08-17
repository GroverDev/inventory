using Common.Utilities;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;

namespace Inventory.Application;

public interface IStockMovementApplication
{
    Task<Response<List<StockMovementResponse>>> GetMovementsByProduct(string productId);

    Task<Response<List<StockExpiryResponse>>> GetExpiring(int dias);
    Task<Response<List<LotTraceabilityResponse>>> GetTraceability(string lotCode);
    Task<Response<List<StockSerialResponse>>> GetAvailableSerials(string productId);
    Task<Response<bool>> CreateAdjustment(StockAdjustmentRequest request, int userId);
}
