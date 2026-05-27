using Common.Utilities;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seguridad.Application;
using Seguridad.Domain;
using Seguridad.Domain.Entities.requests;
using Seguridad.Domain.Requests;
using Services.Api.Utils;

namespace Services.Api.Controllers.Security;

[Authorize]
[ApiExplorerSettings(GroupName = "SECURITY")]
[Route("api/[controller]")]
[ApiController]
public class UsersController(IUsersApplication _usersApplication): ControllerBase
{
    [HttpPost("GetUsers")]
public async Task<ActionResult<Response<List<UsersResponse>>>> Get([FromBody] UserSearchRequest userSearchRequest)
{
    ValidationResult result = new UserSearchRequestValidator().Validate(userSearchRequest);
    if (!result.IsValid) return ErrorsValidation<List<UsersResponse>>.GetResponse(result.Errors);

    var resp = await _usersApplication.GetUsers(userSearchRequest);
    return Ok(resp);
}

[HttpPost]
public async Task<ActionResult<Response<bool>>> CreateUser([FromBody] UserRequest user) // Return Response<bool> based on IUsersApplication
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var datos = TokenData.GetData(HttpContext);

    // ValidationResult result = new UserSearchRequestValidator().Validate(userSearchRequest);
    // if (!result.IsValid) return ErrorsValidation<List<UsersResponse>>.getRespuesta(result.Errors);

    var resp = await _usersApplication.CreateUser(user, datos.UserId);
    return Ok(resp);
}

[HttpGet("{uuid}")]
public async Task<ActionResult<Response<UsersResponse>>> GetUser(Guid uuid)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var resp = await _usersApplication.GetUser(uuid);
    return Ok(resp);
}

[HttpPut("{uuid}")]
public async Task<ActionResult<Response<bool>>> UpdateUser(Guid uuid, [FromBody] UserUpdateRequest user)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var datos = TokenData.GetData(HttpContext);

    ValidationResult result = new UserUpdateRequestValidator().Validate(user);
    if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

    var resp = await _usersApplication.UpdateUser(uuid, user, datos.UserId);
    return Ok(resp);
}

[HttpDelete("{uuid}")]
public async Task<ActionResult<Response<bool>>> DeleteUser(Guid uuid)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var datos = TokenData.GetData(HttpContext);

    var resp = await _usersApplication.DeleteUser(uuid, datos.UserId);
    return Ok(resp);
}

[HttpGet("{uuid}/roles")]
public async Task<ActionResult<Response<List<Roles>>>> GetRolesByUser(Guid uuid)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var resp = await _usersApplication.GetRolesByUser(uuid);
    return Ok(resp);
}

[HttpPut("{uuid}/roles")]
public async Task<ActionResult<Response<bool>>> AssignRolesToUser(Guid uuid, [FromBody] UserRolesRequest request)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var datos = TokenData.GetData(HttpContext);
    var resp = await _usersApplication.AssignRolesToUser(uuid, request.RoleIds, datos.UserId);
    return Ok(resp);
}

[HttpPut("me/password")]
public async Task<ActionResult<Response<bool>>> ChangeOwnPassword([FromBody] ChangeOwnPasswordRequest request)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var datos = TokenData.GetData(HttpContext);

    ValidationResult result = new ChangeOwnPasswordRequestValidator().Validate(request);
    if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

    var resp = await _usersApplication.ChangeOwnPassword(datos.UserId, request.CurrentPassword, request.NewPassword);
    return Ok(resp);
}

[HttpPut("{uuid}/password")]
public async Task<ActionResult<Response<bool>>> ChangePassword(Guid uuid, [FromBody] ChangePasswordRequest request)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var datos = TokenData.GetData(HttpContext);

    ValidationResult result = new ChangePasswordRequestValidator().Validate(request);
    if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

    var resp = await _usersApplication.ChangeUserPassword(uuid, request.NewPassword, datos.UserId);
    return Ok(resp);
}

[HttpPost("{uuid}/mfa/reset")]
public async Task<ActionResult<Response<bool>>> AdminResetMfa(Guid uuid)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var resp = await _usersApplication.AdminResetMfa(uuid);
    return Ok(resp);
}

[HttpPut("{uuid}/mfa/required")]
public async Task<ActionResult<Response<bool>>> AdminRequireMfa(Guid uuid)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var resp = await _usersApplication.AdminSetMfaRequired(uuid, true);
    return Ok(resp);
}

[HttpDelete("{uuid}/mfa/required")]
public async Task<ActionResult<Response<bool>>> AdminUnrequireMfa(Guid uuid)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var resp = await _usersApplication.AdminSetMfaRequired(uuid, false);
    return Ok(resp);
}
}
