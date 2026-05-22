using Common.Utilities;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seguridad.Application;
using Seguridad.Domain;
using Services.Api.Utils;

namespace Services.Api.Controllers.Security;

[ApiExplorerSettings(GroupName = "Security")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class ModulesController(IModulesApplication _modulesApplication) : ControllerBase
{
    // POST api/Modules
    [HttpPost()]
public async Task<ActionResult<Response<int>>> CreateModule([FromBody] ModulesRequest modulesRequest)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

    var datos = TokenData.GetData(HttpContext);

    ValidationResult result = new ModulesRequestValidator().Validate(modulesRequest);
    if (!result.IsValid) return ErrorsValidation<int>.GetResponse(result.Errors);

    var respuesta = await _modulesApplication.CreateModule(modulesRequest, datos.UserId);
    return respuesta;
}

// PUT api/Modules/5
[HttpPut("{id}")]
public async Task<ActionResult<Response<bool>>> UpdateModule(int id, [FromBody] ModulesRequest modulesRequest)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

    var datos = TokenData.GetData(HttpContext);

    if (id <= 0) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

    ValidationResult result = new ModulesRequestValidator().Validate(modulesRequest);
    if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

    modulesRequest.Id = id;
    var respuesta = await _modulesApplication.UpdateModule(modulesRequest, datos.UserId);
    return respuesta;
}

// DELETE api/Modules/5
[HttpDelete("{id}")]
public async Task<ActionResult<Response<bool>>> Delete(int id)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var datos = TokenData.GetData(HttpContext);

    if (id <= 0) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

    var respuesta = await _modulesApplication.DeleteModule(id, datos.UserId);
    return respuesta;
}

// GET: api/Modules
[HttpGet]
public async Task<ActionResult<Response<List<ModulesResponse>>>> GetModules(string nameModule = "")
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    if (nameModule == "ALL") nameModule = "";

    var respuesta = await _modulesApplication.GetModules(nameModule);
    return respuesta;
}

// GET api/Modules/5
[HttpGet("{id}")]
public async Task<ActionResult<Response<ModulesResponse>>> GetModule(int id)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

    if (id <= 0) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

    var respuesta = await _modulesApplication.GetModule(id);
    return respuesta;
}
}
