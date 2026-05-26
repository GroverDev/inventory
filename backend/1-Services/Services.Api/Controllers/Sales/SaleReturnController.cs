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
public class SaleReturnController(ISaleReturnApplication _saleReturnApplication) : ControllerBase
{
    // POST api/SaleReturn
    [HttpPost]
    public async Task<ActionResult<Response<string>>> CreateReturn([FromBody] SaleReturnRequest request)
    {
        var token = TokenData.GetData(HttpContext);
        if (!token.ok) return Unauthorized("Acceso no Autorizado.");

        if (!Guid.TryParse(request.SaleId, out _))
            return BadRequest(new Response<bool> { Message = new Msg { MessageType = "error", Description = "Sale ID no válido." } });

        if (request.Detail == null || request.Detail.Count == 0)
            return BadRequest(new Response<bool> { Message = new Msg { MessageType = "warning", Description = "Debe seleccionar al menos un producto a devolver." } });

        var respuesta = await _saleReturnApplication.CreateReturn(request, token.UserId);
        return respuesta;
    }
}
