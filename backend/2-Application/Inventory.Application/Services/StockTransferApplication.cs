using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class StockTransferApplication(IStockTransferRepository _repository) : IStockTransferApplication
{
    private const string ErrorGeneral = "Ocurrio un error, por favor comuniquese con Sistemas.";

    public async Task<Response<string>> CreateDraft(StockTransferRequest request, int userId)
    {
        Response<string> resp = new();
        try
        {
            resp.Data = (await _repository.CreateDraft(request, userId)).ToString();
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, ErrorGeneral, ex); }
        return resp;
    }

    public Task<Response<bool>> UpdateDraft(Guid id, StockTransferRequest request, int userId) =>
        Ejecutar(() => _repository.UpdateDraft(id, request, userId));

    public Task<Response<bool>> Cancel(Guid id, int userId) =>
        Ejecutar(() => _repository.Cancel(id, userId));

    public Task<Response<bool>> Send(Guid id, int userId) =>
        Ejecutar(() => _repository.Send(id, userId));

    public Task<Response<bool>> Receive(Guid id, ReceiveStockTransferRequest request, int userId) =>
        Ejecutar(() => _repository.Receive(id, request.Lots, userId));

    public async Task<Response<List<StockTransferResponse>>> GetTransfers(DateTime from, DateTime to, string status)
    {
        Response<List<StockTransferResponse>> resp = new() { Data = [] };
        try
        {
            resp.Data = await _repository.GetTransfers(from, to, status);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, ErrorGeneral, ex); }
        return resp;
    }

    public async Task<Response<StockTransferResponse>> GetTransfer(Guid id)
    {
        Response<StockTransferResponse> resp = new() { Data = new() };
        try
        {
            resp.Data = await _repository.GetTransfer(id)
                        ?? throw new CustomException("No existe el traspaso, o no involucra a esta sucursal.", MessageTypes.Info);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(ex.messageType, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, ErrorGeneral, ex); }
        return resp;
    }

    private static async Task<Response<bool>> Ejecutar(Func<Task> accion)
    {
        Response<bool> resp = new();
        try
        {
            await accion();
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, ErrorGeneral, ex); }
        return resp;
    }
}
