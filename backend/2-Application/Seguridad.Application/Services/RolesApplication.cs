using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Seguridad.Domain;
using Seguridad.Infrastructure;

namespace Seguridad.Application;

public class RolesApplication(IRolesRepository _rolesRepository) : IRolesApplication
{
    public async Task<Response<List<Roles>>> GetRolesXUserId(int userId)
    {
        var resp = new Response<List<Roles>>() { Data = [] };
        try
        {
            resp.Data = await _rolesRepository.GetRolesXUserId(userId);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
        return resp;
    }

    public async Task<Response<List<Roles>>> GetRoles(RolSearchRequest rolSearchRequest)
    {
        var resp = new Response<List<Roles>> { Data = [] };
        try
        {
            resp.Data = await _rolesRepository.GetRoles(rolSearchRequest);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
        return resp;
    }

    public async Task<Response<Roles>> GetRoleById(int id)
    {
        var resp = new Response<Roles> { Data = new() };
        try
        {
            resp.Data = await _rolesRepository.GetRoleById(id);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
        return resp;
    }

    public async Task<Response<int>> CreateRole(RolesRequest request, int createdBy)
    {
        var resp = new Response<int> { Data = 0 };
        try
        {
            var role = request.Adapt<Roles>();
            role.State = true;
            AuditHelper.SetCreated(role, createdBy);

            resp.Data = await _rolesRepository.CreateRole(role);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<bool>> UpdateRole(RolesRequest request, int modifiedBy)
    {
        var resp = new Response<bool>();
        try
        {
            var role = request.Adapt<Roles>();
            AuditHelper.SetModified(role, modifiedBy);

            var rows = await _rolesRepository.UpdateRole(role);
            if (rows <= 0) throw new CustomException("No se pudo modificar el rol");
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<bool>> DeleteRole(int id, int modifiedBy)
    {
        var resp = new Response<bool>();
        try
        {
            var rows = await _rolesRepository.DeleteRole(id, modifiedBy);
            if (rows <= 0) throw new CustomException("No se pudo eliminar el rol");
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<bool>> AssignFormsToRole(RolesFormsRequest request, int userId)
    {
        var resp = new Response<bool>();
        try
        {
            await _rolesRepository.AssignFormsToRole(request.RolId, request.FormIds, userId);
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<bool> HasFormPermission(int userId, string formRoute, string action)
    {
        try
        {
            return await _rolesRepository.HasFormPermission(userId, formRoute, action);
        }
        catch
        {
            // Ante cualquier error de verificación, denegar por seguridad.
            return false;
        }
    }
}
