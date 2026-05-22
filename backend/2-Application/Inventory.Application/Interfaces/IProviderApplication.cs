using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface IProviderApplication
{
    public Task<Response<bool>> CreateProvider(ProviderRequest providerRequest, int createdBy);
    public Task<Response<bool>> UpdateProvider(ProviderRequest providerRequest, int modifiedBy);
    public Task<Response<bool>> DeleteProvider(string id, int modifiedBy);
    public Task<Response<List<ProviderRequest>>> GetProviders(string providerName);
    public Task<Response<ProviderRequest>> GetProvider(string id);
}
