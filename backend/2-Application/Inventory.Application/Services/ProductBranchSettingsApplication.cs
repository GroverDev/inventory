using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class ProductBranchSettingsApplication(IProductBranchSettingsRepository _repository) : IProductBranchSettingsApplication
{
    public async Task<Response<List<ProductBranchSettingResponse>>> GetSettings(Guid productId)
    {
        Response<List<ProductBranchSettingResponse>> resp = new() { Data = [] };
        try
        {
            resp.Data = await _repository.GetSettings(productId);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<bool>> SaveSettings(Guid productId, List<ProductBranchSettingRequest> settings, int userId)
    {
        Response<bool> resp = new();
        try
        {
            await _repository.SaveSettings(productId, settings, userId);
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return resp;
    }
}
