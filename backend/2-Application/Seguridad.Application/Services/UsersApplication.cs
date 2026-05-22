using Mapster;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Seguridad.Domain;
using Seguridad.Domain.Entities.requests;
using Seguridad.Domain.Requests;
using Seguridad.Infrastructure;

namespace Seguridad.Application;

public class UsersApplication(IUsersRepository _usersRepository, IMfaRepository _mfaRepository) : IUsersApplication
{
    public async Task<Response<bool>> CreateUser(UserRequest userRequest, int UserId)
{
    var resp = new Response<bool>();
    try
    {
        var user = userRequest.Adapt<Users>();

        user.Uuid = Guid.NewGuid();
        user.IsActive = true;
        user.CreatedBy = UserId;
        user.UserName = user.Email;
        user.LastAccess = DateTime.Now;
        user.ChangePassword = true;
        user.IsActive = true;

        resp.Data = await _usersRepository.CreateUser(user, UserId);
        resp.ok = true;
    }
    catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
    return resp;
}

public async Task<Response<List<UsersResponse>>> GetUsers(UserSearchRequest user)
{
    var resp = new Response<List<UsersResponse>>() { Data = [] };
    try
    {
        resp.Data = await _usersRepository.GetUsers(user);
        resp.ok = true;
    }
    catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
    return resp;
}


public async Task<Response<UsersResponse>> GetUser(Guid uuid)
{
    var resp = new Response<UsersResponse>();
    try
    {
        resp.Data = await _usersRepository.GetUser(uuid);
        if (resp.Data != null) resp.ok = true;
        else resp.SetMessage(MessageTypes.Warning, "Usuario no encontrado");
    }
    catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
    return resp;
}

public async Task<Response<bool>> UpdateUser(Guid uuid, UserUpdateRequest request, int modifiedBy)
{
    var resp = new Response<bool>();
    try
    {
        // Map request to domain entity
        // Since we are updating specific fields (email, name), we can map manually or use mapper if configured.
        // But UserRequest is incomplete (no password), so mapping to Users might be tricky if we don't set other required fields.
        // Ideally we just pass the fields or a partial entity. The repository handles it by only updating the fields we care about.
        //var user = _mapper.Map<Users>(request);
        var user = new Users();
        user.Uuid = uuid; // Important
        user.FullName = request.FullName;
        user.Email = request.Email;
        
        resp.Data = await _usersRepository.UpdateUser(user, modifiedBy);
        resp.ok = true;
    }
    catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
    return resp;
}

public async Task<Response<bool>> DeleteUser(Guid uuid, int modifiedBy)
{
    var resp = new Response<bool>();
    try
    {
        resp.Data = await _usersRepository.DeleteUser(uuid, modifiedBy);
        resp.ok = true;
    }
    catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
    return resp;
}

public async Task<Response<bool>> AdminResetMfa(Guid userUuid)
{
    var resp = new Response<bool>();
    try
    {
        var mfa = await _mfaRepository.GetTotpMfaByUuid(userUuid);
        if (mfa == null)
            throw new CustomException("El usuario no tiene TOTP configurado.");

        await _mfaRepository.AdminResetMfa(mfa.UserId);
        resp.Data = true;
        resp.ok = true;
    }
    catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
    return resp;
}

public async Task<Response<bool>> AdminSetMfaRequired(Guid userUuid, bool required)
{
    var resp = new Response<bool>();
    try
    {
        var mfa = await _mfaRepository.GetTotpMfaByUuid(userUuid);
        int? userId = mfa?.UserId ?? await _mfaRepository.GetUserIdByUuid(userUuid);

        if (userId == null)
            throw new CustomException("Usuario no encontrado o inactivo.");

        await _mfaRepository.AdminSetRequired(userId.Value, required);
        resp.Data = true;
        resp.ok = true;
    }
    catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
    catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrio un error, por favor comuniquese con Soporte Tecnico.", ex); }
    return resp;
}
}
