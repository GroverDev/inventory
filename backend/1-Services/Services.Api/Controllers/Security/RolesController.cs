using Common.Utilities;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seguridad.Application;
using Seguridad.Domain;
using Services.Api.Utils;

namespace Services.Api.Controllers.Security;

[Authorize]
[ApiExplorerSettings(GroupName = "SECURITY")]
[Route("api/[controller]")]
[ApiController]
public class RolesController(IRolesApplication _rolesApplication, IFormsApplication _formsApplication) : ControllerBase
{
    // GET api/Roles?nameRol=&description=
    [HttpGet]
    public async Task<ActionResult<Response<List<Roles>>>> GetRoles([FromQuery] string nameRol = "", [FromQuery] string description = "")
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var rolSearchRequest = new RolSearchRequest { NameRol = nameRol, Description = description };

        ValidationResult result = new RolSearchRequestValidator().Validate(rolSearchRequest);
        if (!result.IsValid) return ErrorsValidation<List<Roles>>.GetResponse(result.Errors);

        var resp = await _rolesApplication.GetRoles(rolSearchRequest);
        return Ok(resp);
    }

    // GET api/Roles/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<Roles>>> GetRoleById(int id)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        if (id <= 0) return BadRequest(new Response<Roles>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        var resp = await _rolesApplication.GetRoleById(id);
        return Ok(resp);
    }

    // POST api/Roles
    [HttpPost]
    public async Task<ActionResult<Response<int>>> CreateRole([FromBody] RolesRequest request)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        ValidationResult result = new RolesRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<int>.GetResponse(result.Errors);

        var resp = await _rolesApplication.CreateRole(request, datos.UserId);
        return Ok(resp);
    }

    // PUT api/Roles/5
    [HttpPut("{id}")]
    public async Task<ActionResult<Response<bool>>> UpdateRole(int id, [FromBody] RolesRequest request)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (id <= 0) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        ValidationResult result = new RolesRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

        request.Id = id;
        var resp = await _rolesApplication.UpdateRole(request, datos.UserId);
        return Ok(resp);
    }

    // DELETE api/Roles/5
    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> DeleteRole(int id)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (id <= 0) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        var resp = await _rolesApplication.DeleteRole(id, datos.UserId);
        return Ok(resp);
    }

    // GET api/Roles/5/forms
    [HttpGet("{id}/forms")]
    public async Task<ActionResult<Response<List<Forms>>>> GetFormsXRol(int id)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        if (id <= 0) return BadRequest(new Response<List<Forms>>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        var resp = await _formsApplication.GetFormsXRolId(id);
        return Ok(resp);
    }

    // POST api/Roles/5/forms
    [HttpPost("{id}/forms")]
    public async Task<ActionResult<Response<bool>>> AssignForms(int id, [FromBody] RolesFormsRequest request)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (id <= 0) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        ValidationResult result = new RolesFormsRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

        request.RolId = id;
        var resp = await _rolesApplication.AssignFormsToRole(request, datos.UserId);
        return Ok(resp);
    }
}
