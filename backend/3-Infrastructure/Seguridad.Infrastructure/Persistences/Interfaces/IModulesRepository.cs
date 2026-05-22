using Seguridad.Domain.Entities;

namespace Seguridad.Infrastructure;

public interface IModulesRepository
{
    public Task<int> CreateModule(Modules module);
    public Task<int> UpdateModule(Modules module);
    public Task<int> DeleteModule(int id, int idUserModified);
    public Task<List<Modules>> GetModules(string nameModule);
    public Task<Modules> GetModule(int id);
}
