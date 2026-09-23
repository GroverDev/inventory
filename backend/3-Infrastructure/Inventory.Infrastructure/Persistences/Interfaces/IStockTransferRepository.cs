using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface IStockTransferRepository
{
    /// <summary>Crea un borrador con origen en la sucursal activa. Devuelve su id.</summary>
    public Task<Guid> CreateDraft(StockTransferRequest request, int userId);

    /// <summary>Reemplaza destino, notas y productos de un borrador.</summary>
    public Task UpdateDraft(Guid id, StockTransferRequest request, int userId);

    public Task Cancel(Guid id, int userId);
    public Task Send(Guid id, int userId);
    public Task Receive(Guid id, List<ReceivedLotRequest> lots, int userId);

    /// <summary>
    /// Traspasos que salen de la sucursal activa o llegan a ella, en el rango de
    /// fechas. Los pendientes (borrador o en tránsito) se incluyen siempre.
    /// </summary>
    public Task<List<StockTransferResponse>> GetTransfers(DateTime from, DateTime to, string status);

    /// <summary>Traspaso con su detalle y sus lotes, si involucra a la sucursal activa.</summary>
    public Task<StockTransferResponse?> GetTransfer(Guid id);
}
