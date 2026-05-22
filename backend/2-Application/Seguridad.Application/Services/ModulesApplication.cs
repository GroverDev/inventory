using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Seguridad.Domain;
using Seguridad.Domain.Entities;
using Seguridad.Infrastructure;

namespace Seguridad.Application;

public class ModulesApplication(IModulesRepository _modulesRepository) : IModulesApplication
{
    public async Task<Response<int>> CreateModule(ModulesRequest moduleRequest, int createdBy)
{
    Response<int> respuesta = new() { Data = 0 };
    try
    {
        var module = moduleRequest.Adapt<Modules>();
        module.State = true;
        AuditHelper.SetCreated(module, createdBy);

        respuesta.Data = await _modulesRepository.CreateModule(module);
        respuesta.ok = true;
    }
    catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return respuesta;
}

public async Task<Response<bool>> UpdateModule(ModulesRequest moduleRequest, int modifiedBy)
{
    Response<bool> respuesta = new();
    try
    {
        var module = moduleRequest.Adapt<Modules>();
        AuditHelper.SetModified(module, modifiedBy);

        var rowsAffected = await _modulesRepository.UpdateModule(module);
        if (rowsAffected <= 0)
            throw new CustomException("No se pudo modificar el módulo");
        respuesta.Data = respuesta.ok = true;
    }
    catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return respuesta;
}

public async Task<Response<bool>> DeleteModule(int id, int modifiedBy)
{
    Response<bool> respuesta = new();
    try
    {
        var rowsAffected = await _modulesRepository.DeleteModule(id, modifiedBy);
        if (rowsAffected <= 0)
            throw new CustomException("No se pudo eliminar el módulo");
        respuesta.Data = respuesta.ok = true;
    }
    catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return respuesta;
}

public async Task<Response<List<ModulesResponse>>> GetModules(string nameModule)
{
    Response<List<ModulesResponse>> modules = new() { Data = new() };
    try
    {
        var result = await _modulesRepository.GetModules(nameModule);
        modules.Data = result.Adapt<List<ModulesResponse>>();
        modules.ok = true;
    }
    catch (CustomException ex) { modules.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { modules.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return modules;
}

public async Task<Response<ModulesResponse>> GetModule(int id)
{
    Response<ModulesResponse> module = new() { Data = new() };
    try
    {
        var result = await _modulesRepository.GetModule(id);
        module.Data = result.Adapt<ModulesResponse>();
        module.ok = true;
    }
    catch (CustomException ex) { module.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { module.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
    return module;
}
}
