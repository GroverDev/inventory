using System.Data;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;

namespace Inventory.Infrastructure;

public interface IStockMovementRepository
{
    Task<List<StockMovementResponse>> GetMovementsByProduct(Guid productId);

    /// <summary>
    /// Existencias con vencimiento, de la más urgente a la menos. Solo devuelve las
    /// que tienen fecha: un producto sin seguimiento por lotes no aparece.
    /// </summary>
    /// <param name="dias">Ventana en días. 0 o menos devuelve todas.</param>
    Task<List<StockExpiryResponse>> GetExpiring(int dias);
    Task CreateAdjustment(StockMovement movement, int userId);
    Task InsertMovement(StockMovement movement, IDbConnection db, IDbTransaction transaction);
}
