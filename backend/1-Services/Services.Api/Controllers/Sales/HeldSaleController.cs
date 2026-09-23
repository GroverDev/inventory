using Common.Utilities;
using FluentValidation.Results;
using Inventory.Application;
using Inventory.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Api.Utils;

namespace Services.Api.Controllers.Sales;

/// <summary>
/// Ventas en espera de la sucursal activa. Sin chequeo de formulario, igual que
/// registrar una venta: lo usa cualquiera que opere el punto de venta.
/// </summary>
[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class HeldSaleController(IHeldSaleApplication _application) : ControllerBase
{
    // GET api/HeldSale
    [HttpGet]
    public async Task<ActionResult<Response<List<HeldSaleResponse>>>> GetHeld()
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        return await _application.GetHeld();
    }

    // POST api/HeldSale
    [HttpPost]
    public async Task<ActionResult<Response<string>>> Hold([FromBody] HeldSaleRequest request)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        ValidationResult result = new HeldSaleRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidationString.GetResponseString(result.Errors);

        return await _application.Hold(request, datos.UserId);
    }

    // POST api/HeldSale/guid/take
    // La saca de la espera y devuelve su carrito.
    [HttpPost("{id:guid}/take")]
    public async Task<ActionResult<Response<HeldSaleResponse>>> Take(Guid id)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        return await _application.Take(id);
    }

    // DELETE api/HeldSale/guid
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Response<bool>>> Discard(Guid id)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        return await _application.Discard(id);
    }
}
