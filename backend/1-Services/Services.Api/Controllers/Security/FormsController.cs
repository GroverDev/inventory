using Common.Utilities;
using FluentValidation.Results;
using Seguridad.Application;
using Seguridad.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Api.Utils;

namespace Services.Api.Controllers.Security;

[ApiExplorerSettings(GroupName = "Security")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class FormsController(IFormsApplication _formsApplication) : ControllerBase
{
    // POST api/Forms
    [HttpPost()]
public async Task<ActionResult<Response<int>>> CreateForm([FromBody] FormsRequest formsRequest)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

    var datos = TokenData.GetData(HttpContext);

    ValidationResult result = new FormsRequestValidator().Validate(formsRequest);
    if (!result.IsValid) return ErrorsValidation<int>.GetResponse(result.Errors);

    var respuesta = await _formsApplication.CreateForm(formsRequest, datos.UserId);
    return respuesta;
}

// PUT api/Forms/5
[HttpPut("{id}")]
public async Task<ActionResult<Response<bool>>> UpdateForm(int id, [FromBody] FormsRequest formsRequest)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

    var datos = TokenData.GetData(HttpContext);

    if (id <= 0) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

    ValidationResult result = new FormsRequestValidator().Validate(formsRequest);
    if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

    formsRequest.Id = id;
    var respuesta = await _formsApplication.UpdateForm(formsRequest, datos.UserId);
    return respuesta;
}

// DELETE api/Forms/5
[HttpDelete("{id}")]
public async Task<ActionResult<Response<bool>>> Delete(int id)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var datos = TokenData.GetData(HttpContext);

    if (id <= 0) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

    var respuesta = await _formsApplication.DeleteForm(id, datos.UserId);
    return respuesta;
}

// GET: api/Forms
[HttpGet]
public async Task<ActionResult<Response<List<FormsResponse>>>> GetForms(string nameForm = "")
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    if (nameForm == "ALL") nameForm = "";

    var respuesta = await _formsApplication.GetForms(nameForm);
    return respuesta;
}

// GET api/Forms/5
[HttpGet("{id}")]
public async Task<ActionResult<Response<FormsResponse>>> GetForm(int id)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

    if (id <= 0) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

    var respuesta = await _formsApplication.GetForm(id);
    return respuesta;
}
}
