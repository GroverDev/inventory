using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class ProviderApplication(IProviderRepository _providerRepository): IProviderApplication
{
    public async Task<Response<bool>> CreateProvider(ProviderRequest providerRequest, int createdBy)
    {
        Response<bool> respuesta = new();
        try
        {
            providerRequest.Id = Guid.Empty.ToString();

            var provider = providerRequest.Adapt<Provider>();
            provider.CreatedBy = provider.ModifiedBy = createdBy;
            provider.Created = provider.Modified = DateTime.Now;
            provider.State = true;
            provider.IsActive = true;

            respuesta.Data = await _providerRepository.CreateProvider(provider);
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> UpdateProvider(ProviderRequest providerRequest, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            var provider = providerRequest.Adapt<Provider>();
            provider.ModifiedBy = modifiedBy;
            provider.Modified = DateTime.Now;

            var rowsAffected = await _providerRepository.UpdateProvider(provider);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo modificar el proveedor");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }

    public async Task<Response<bool>> DeleteProvider(string id, int modifiedBy)
    {
        Response<bool> respuesta = new();
        try
        {
            Guid providerId = Guid.Parse(id);

            var rowsAffected = await _providerRepository.DeleteProvider(providerId, modifiedBy);
            if (rowsAffected <= 0)
                throw new CustomException("No se pudo eliminar al proveedor");
            respuesta.Data = respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respuesta;
    }
    public async Task<Response<List<ProviderRequest>>> GetProviders(string providerName)
    {
        Response<List<ProviderRequest>> providers = new() { Data = [] };
        try
        {
            var resp = await _providerRepository.GetProviders(providerName);
            foreach (var providerItem in resp)
            {
                var providerNew = providerItem.Adapt<ProviderRequest>();
                providers.Data.Add(providerNew);
            }

            providers.ok = true;
        }
        catch (CustomException ex) { providers.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { providers.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return providers;
    }

    public async Task<Response<ProviderRequest>> GetProvider(string id)
    {
        Response<ProviderRequest> respProviders = new() { Data = new() };
        try
        {
            Guid fabricatorId = Guid.Parse(id);
            var fabricator = await _providerRepository.GetProvider(fabricatorId);

            var clientNew = fabricator.Adapt<ProviderRequest>();
            respProviders.Data = clientNew;
            respProviders.ok = true;
        }
        catch (CustomException ex) { respProviders.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respProviders.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return respProviders;
    }
}
