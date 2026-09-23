using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class HeldSaleApplication(IHeldSaleRepository _repository) : IHeldSaleApplication
{
    private const string ErrorGeneral = "Ocurrio un error, por favor comuniquese con Sistemas.";
    private const string YaNoEsta = "Esa venta ya no está en espera: otra caja la retomó o la descartó.";

    public async Task<Response<string>> Hold(HeldSaleRequest request, int userId)
    {
        Response<string> resp = new();
        try
        {
            resp.Data = (await _repository.Hold(request, userId)).ToString();
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, ErrorGeneral, ex); }
        return resp;
    }

    public async Task<Response<List<HeldSaleResponse>>> GetHeld()
    {
        Response<List<HeldSaleResponse>> resp = new() { Data = [] };
        try
        {
            resp.Data = await _repository.GetHeld();
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, ErrorGeneral, ex); }
        return resp;
    }

    public async Task<Response<HeldSaleResponse>> Take(Guid id)
    {
        Response<HeldSaleResponse> resp = new() { Data = new() };
        try
        {
            resp.Data = await _repository.Take(id) ?? throw new CustomException(YaNoEsta);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, ErrorGeneral, ex); }
        return resp;
    }

    public async Task<Response<bool>> Discard(Guid id)
    {
        Response<bool> resp = new();
        try
        {
            if (!await _repository.Discard(id)) throw new CustomException(YaNoEsta);
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, ErrorGeneral, ex); }
        return resp;
    }
}
