using System.Data;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;

namespace Inventory.Infrastructure;

public interface IStockMovementRepository
{
    /// <summary>Historial de movimientos del producto. Con <paramref name="stockItemId"/> se acota a un lote/existencia puntual.</summary>
    Task<List<StockMovementResponse>> GetMovementsByProduct(Guid productId, Guid? stockItemId);

    /// <summary>
    /// Existencias con vencimiento, de la más urgente a la menos. Solo devuelve las
    /// que tienen fecha: un producto sin seguimiento por lotes no aparece.
    /// </summary>
    /// <param name="dias">Ventana en días. 0 o menos devuelve todas.</param>
    Task<List<StockExpiryResponse>> GetExpiring(int dias);
    /// <summary>
    /// A quién se le vendió un lote. Vacío si no se vendió nada de él todavía.
    /// </summary>
    Task<List<LotTraceabilityResponse>> GetTraceability(string lotCode);

    /// <summary>Unidades serializadas disponibles de un producto, para elegir en el mostrador.</summary>
    Task<List<StockSerialResponse>> GetAvailableSerials(Guid productId);

    Task CreateAdjustment(StockMovement movement, int userId);

    /// <summary>
    /// Da de baja una existencia puntual (lote vencido/dañado/retirado). A
    /// diferencia de <see cref="CreateAdjustment"/>, exige un lote explícito.
    /// </summary>
    Task CreateWriteOff(StockMovement movement, Guid stockItemId, int userId);

    /// <summary>Mermas por vencimiento en un rango de fechas, agregadas y en detalle.</summary>
    Task<WriteOffReportResponse> GetWriteOffs(DateTime desde, DateTime hasta, Guid? productId);

    Task InsertMovement(StockMovement movement, IDbConnection db, IDbTransaction transaction);
}
