using Common.Utilities;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities.Responses;
using Inventory.Domain.Entities.Requests;
using FluentValidation.Results;
using Services.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Services.Api.Controllers.Inventory;

[Route("api/[controller]")]
[ApiController]
//[Authorize]
public class UnitOfMeasurementController(IUnitsOfMeasurementApplication _unitsOfMeasurementApplication) : ControllerBase
    {
        // GET: api/UnitOfMeasurement
        [HttpGet]
public async Task<ActionResult<Response<List<UnitOfMeasurementResponse>>>> GetUnitsOfMeasurement(string UnitOfMeasurementName = "ALL")
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

    var respuesta = await _unitsOfMeasurementApplication.GetUnitsOfMeasurement(UnitOfMeasurementName == "ALL" ? "" : UnitOfMeasurementName);
    return respuesta;
}

// POST api/UnitOfMeasurement
[HttpPost]
public async Task<ActionResult<Response<string>>> CreateUnitOfMeasurement([FromBody] UnitOfMeasurementRequest unitOfMeasurementRequest)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

    var datos = TokenData.GetData(HttpContext);

    ValidationResult result = new UnitOfMeasurementRequestValidator().Validate(unitOfMeasurementRequest);
    if (!result.IsValid) return ErrorsValidationString.GetResponseString(result.Errors);

    var respuesta = await _unitsOfMeasurementApplication.CreateUnitOfMeasurement(unitOfMeasurementRequest, datos.UserId);
    return respuesta;
}

// PUT api/UnitOfMeasurement/5
[HttpPut("{id}")]
public async Task<ActionResult<Response<bool>>> UpdateUnitOfMeasurement(string id, [FromBody] UnitOfMeasurementRequest unitOfMeasurementRequest)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

    var datos = TokenData.GetData(HttpContext);

    if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

    ValidationResult result = new UnitOfMeasurementRequestValidator().Validate(unitOfMeasurementRequest);
    if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

    unitOfMeasurementRequest.Id = id;
    var respuesta = await _unitsOfMeasurementApplication.UpdateUnitOfMeasurement(unitOfMeasurementRequest, datos.UserId);
    return respuesta;
}

// DELETE api/UnitOfMeasurement/5
[HttpDelete("{id}")]
public async Task<ActionResult<Response<bool>>> Delete(string id)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
    var datos = TokenData.GetData(HttpContext);

    if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

    var respuesta = await _unitsOfMeasurementApplication.DeleteUnitOfMeasurement(id, datos.UserId);
    return respuesta;
}

// GET api/UnitOfMeasurement/5
[HttpGet("{id}")]
public async Task<ActionResult<Response<UnitOfMeasurementResponse>>> GetUnitOfMeasurement(string id)
{
    if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

    if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

    var respuesta = await _unitsOfMeasurementApplication.GetUnitOfMeasurement(id);
    return respuesta;
}
    }