
using Common.Utilities;
using Inventory.Application;
using Inventory.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Api.Utils;

namespace Services.Api.Controllers.Inventory;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class ProviderController(IProviderApplication _providerApplication) : ControllerBase
{
    // POST api/Provider
    [HttpPost()]
    public async Task<ActionResult<Response<bool>>> CreateProvider([FromBody] ProviderRequest provider)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var datos = TokenData.GetData(HttpContext);
        var respuesta = await _providerApplication.CreateProvider(provider, Convert.ToInt32(datos.UserId));
        return respuesta;
    }

    // PUT api/Provider/guid
    [HttpPut("{id}")]
    public async Task<ActionResult<Response<bool>>> UpdateProvider(string id, [FromBody] ProviderRequest providerVM)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

         var datos = TokenData.GetData(HttpContext);

        var respuesta = await _providerApplication.UpdateProvider(providerVM, Convert.ToInt32(datos.UserId)); 
        return respuesta;
    }

    // DELETE api/Provider/GUID
    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> DeleteProvider(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var datos = TokenData.GetData(HttpContext);
        var respuesta = await _providerApplication.DeleteProvider(id, Convert.ToInt32(datos.UserId));
        return respuesta;
    }

    // GET: api/Provider
    [HttpGet]
    public async Task<ActionResult<Response<List<ProviderRequest>>>> GetProviders(string providerName="ALL")
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var respuesta = await _providerApplication.GetProviders(providerName == "ALL" ? "":providerName);
        return respuesta;
    }

    // GET api/Provider/GUID
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<ProviderRequest>>> GetProvider(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var respuesta = await _providerApplication.GetProvider(id);
        return respuesta;
    }
}

