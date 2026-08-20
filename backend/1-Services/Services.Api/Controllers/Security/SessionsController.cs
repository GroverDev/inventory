using Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seguridad.Application;
using Seguridad.Domain;
using Services.Api.Utils;

namespace Services.Api.Controllers.Security;

/// <summary>
/// Panel de administración de sesiones: quién está conectado ahora mismo y
/// cierre remoto de una sesión ajena. Ver <see cref="Common.Utilities.Security.SessionRevocationRegistry"/>
/// para cómo se logra que el cierre sea inmediato y no solo "no se renueva".
/// </summary>
[ApiExplorerSettings(GroupName = "SECURITY")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class SessionsController(
    IAuthenticationApplication _authenticationApplication,
    IUsersApplication _usersApplication,
    IRolesApplication _rolesApplication) : ControllerBase
{
    // Formulario (route) al que pertenece este controlador, usado para verificar permisos por acción.
    private const string FormRoute = "active-sessions";

    /// <summary>Todas las sesiones activas del tenant, con datos del usuario: el panel de "usuarios conectados".</summary>
    [HttpGet("connected")]
    public async Task<ActionResult<Response<List<ConnectedUserResponse>>>> GetConnected()
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "read"))
            return Ok(new Response<List<ConnectedUserResponse>>()
            { ok = false, Message = new Msg { MessageType = "warning", Description = "No tiene permiso para ver las sesiones activas." } });

        return Ok(await _authenticationApplication.GetConnectedUsers(datos.TenantId));
    }

    /// <summary>Sesiones activas de un usuario puntual, para su ficha de administración.</summary>
    [HttpGet("user/{uuid}")]
    public async Task<ActionResult<Response<List<SessionResponse>>>> GetUserSessions(Guid uuid)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "read"))
            return Ok(new Response<List<SessionResponse>>()
            { ok = false, Message = new Msg { MessageType = "warning", Description = "No tiene permiso para ver las sesiones activas." } });

        var targetUserId = await _usersApplication.GetUserIdByUuid(uuid);
        if (targetUserId == null) return Ok(new Response<List<SessionResponse>>() { Data = [], ok = true });

        return Ok(await _authenticationApplication.GetActiveSessions(targetUserId.Value, datos.TenantId));
    }

    /// <summary>Cierra una sesión puntual (un dispositivo/navegador).</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> CloseSession(long id)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "delete"))
            return Ok(new Response<bool>()
            { ok = false, Message = new Msg { MessageType = "warning", Description = "No tiene permiso para cerrar sesiones." } });

        return Ok(await _authenticationApplication.CloseSession(id, datos.TenantId));
    }

    /// <summary>Cierra todas las sesiones activas de un usuario (todos sus dispositivos).</summary>
    [HttpDelete("user/{uuid}")]
    public async Task<ActionResult<Response<bool>>> CloseAllUserSessions(Guid uuid)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "delete"))
            return Ok(new Response<bool>()
            { ok = false, Message = new Msg { MessageType = "warning", Description = "No tiene permiso para cerrar sesiones." } });

        var targetUserId = await _usersApplication.GetUserIdByUuid(uuid);
        if (targetUserId == null) return Ok(new Response<bool>() { Data = true, ok = true });

        return Ok(await _authenticationApplication.CloseAllSessions(targetUserId.Value, datos.TenantId));
    }
}
