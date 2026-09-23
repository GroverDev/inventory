using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface IStockTransferApplication
{
    public Task<Response<string>> CreateDraft(StockTransferRequest request, int userId);
    public Task<Response<bool>> UpdateDraft(Guid id, StockTransferRequest request, int userId);
    public Task<Response<bool>> Cancel(Guid id, int userId);
    public Task<Response<bool>> Send(Guid id, int userId);
    public Task<Response<bool>> Receive(Guid id, ReceiveStockTransferRequest request, int userId);
    public Task<Response<List<StockTransferResponse>>> GetTransfers(DateTime from, DateTime to, string status);
    public Task<Response<StockTransferResponse>> GetTransfer(Guid id);
}
