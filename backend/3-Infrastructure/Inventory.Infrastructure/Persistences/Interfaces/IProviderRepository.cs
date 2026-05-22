using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface IProviderRepository
{
 public  Task<bool> CreateProvider(Provider provider);
 public  Task<int> UpdateProvider(Provider provider);
 public Task<int> DeleteProvider(Guid id, int idUserModified);
 public  Task<Provider> GetProvider(Guid Id);
  public Task<List<Provider>> GetProviders(string providerName);
}
