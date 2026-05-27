using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Utilities;
using Inventory.Application;
using Inventory.Domain;
using Services.Api.Utils;

namespace Services.Api.Controllers.Sales;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class SalesController(ISalesApplication _salesApplication) : ControllerBase
{
    // POST api/Sales
    [HttpPost()]
    public async Task<ActionResult<Response<string>>> CreateSale([FromBody] SaleRequest saleRequest)
    {
         if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
       
        var datos = TokenData.GetData(HttpContext);

        if (!Guid.TryParse(saleRequest.CustomerId, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Laboratory ID no valido" } });
        foreach (var item in saleRequest.Detail)
        {
            if (!Guid.TryParse(item.ProductId, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Product ID no valido" } });
        }

        var respuesta = await _salesApplication.CreateSale(saleRequest, datos.UserId);
        return respuesta;
    }

    //// PUT api/Sales/guid
    [HttpPut("{id}")]
    public async Task<ActionResult<Response<bool>>> UpdateSale(string id, [FromBody] SaleRequest saleVM)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        var respuesta = await _salesApplication.UpdateSale(saleVM, datos.UserId);
        return respuesta;
    }

    // DELETE api/Sales/GUID
    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> Delete(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        // if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        // var datos = TokenData.GetData(HttpContext);

        var respuesta = await _salesApplication.DeleteSale(id, 1); //datos.IdUsuario);
        return respuesta;
    }

    // GET: api/Sales
    [HttpGet]
    public async Task<ActionResult<Response<List<SaleProductResponse>>>> GetSales(string saleDateInitial, string saleDateEnd)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        return await _salesApplication.GetSales(saleDateInitial, saleDateEnd, datos.UserId, datos.Rol);
    }

    // GET api/Sales/GUID
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<SaleProductResponse>>> GetSale(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var respuesta = await _salesApplication.GetSale(id);
        return respuesta;
    }
}

