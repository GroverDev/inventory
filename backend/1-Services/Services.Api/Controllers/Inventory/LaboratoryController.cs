using System;
using System.Collections.Generic;
using Common;
using Common.Utilities;
using Inventory.Application;
using Inventory.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Api.Utils;



namespace Services.Api.Controllers.Inventory;


[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class LaboratoryController (ILaboratoryApplication _laboratoryApplication): ControllerBase
{
    // POST api/Laboratory
    [HttpPost()]
    public async Task<ActionResult<Response<bool>>> CreateLaboratory([FromBody] LaboratoryRequest laboratoryRequest)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var datos = TokenData.GetData(HttpContext);

        var respuesta = await _laboratoryApplication.CreateLaboratory(laboratoryRequest, Convert.ToInt32(datos.UserId));
        return respuesta;
    }

    // PUT api/Laboratory/guid
    [HttpPut("{id}")]
    public async Task<ActionResult<Response<bool>>> UpdateLaboratory(string id, [FromBody] LaboratoryRequest laboratoryRequest)
    {
         if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var datos = TokenData.GetData(HttpContext);

        var respuesta = await _laboratoryApplication.UpdateLaboratory(laboratoryRequest,Convert.ToInt32(datos.UserId));
        return respuesta;
    }

    // DELETE api/Laboratory/GUID
    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> DeleteLaboratory(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var datos = TokenData.GetData(HttpContext);
        var respuesta = await _laboratoryApplication.DeleteLaboratory(id, Convert.ToInt32(datos.UserId));
        return respuesta;
    }

    // GET: api/Laboratory
    [HttpGet]
    public async Task<ActionResult<Response<List<LaboratoryRequest>>>> GetLaboratories(string laboratoryName="ALL")
    {
        //if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var respuesta = await _laboratoryApplication.GetLaboratories(laboratoryName == "ALL" ? "":laboratoryName);
        return respuesta;
    }

    // GET api/Laboratory/GUID
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<LaboratoryRequest>>> GetLaboratory(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var respuesta = await _laboratoryApplication.GetLaboratory(id);
        return respuesta;
    }
}

