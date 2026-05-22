using Common.Utilities;
using Seguridad.Domain;
using Seguridad.Domain.Entities;

namespace Seguridad.Application;

public interface IModulesApplication
{
    public Task<Response<int>> CreateModule(ModulesRequest moduleRequest, int createdBy);
    public Task<Response<bool>> UpdateModule(ModulesRequest moduleRequest, int modifiedBy);
    public Task<Response<bool>> DeleteModule(int id, int idUserModified);
    public Task<Response<List<ModulesResponse>>> GetModules(string nameModule);
    public Task<Response<ModulesResponse>> GetModule(int id);
}
