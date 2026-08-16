using Common.Utilities;
using FluentValidation.Results;
using Inventory.Application;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Api.Utils;

namespace Services.Api.Controllers.Inventory;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class StockMovementController(IStockMovementApplication _stockMovementApplication) : ControllerBase
{
    // GET api/StockMovement/expiring?dias=90
    // Existencias por vencer, de la más urgente a la menos.
    //
    // Va antes de la ruta {productId} a propósito: si estuviera después, "expiring"
    // entraría por esa ruta y fallaría como identificador inválido.
    [HttpGet("expiring")]
    public async Task<ActionResult<Response<List<StockExpiryResponse>>>> GetExpiring([FromQuery] int dias = 90)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        return await _stockMovementApplication.GetExpiring(dias);
    }

    // GET api/StockMovement/traceability?lote=ABC-123
    // A quién se le vendió un lote. Es la consulta de un retiro de mercado.
    //
    // Va antes de la ruta {productId} por lo mismo que "expiring": si estuviera
    // después, entraría por esa ruta y fallaría como identificador inválido.
    [HttpGet("traceability")]
    public async Task<ActionResult<Response<List<LotTraceabilityResponse>>>> GetTraceability([FromQuery] string lote = "")
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        return await _stockMovementApplication.GetTraceability(lote);
    }

    // GET api/StockMovement/{productId}
    [HttpGet("{productId}")]
    public async Task<ActionResult<Response<List<StockMovementResponse>>>> GetMovements(string productId)
    {
        if (!Guid.TryParse(productId, out _))
            return BadRequest(new Response<List<StockMovementResponse>>() { Message = new Msg() { MessageType = "error", Description = "Id de producto inválido." } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        return await _stockMovementApplication.GetMovementsByProduct(productId);
    }

    // POST api/StockMovement/adjust
    [HttpPost("adjust")]
    public async Task<ActionResult<Response<bool>>> CreateAdjustment([FromBody] StockAdjustmentRequest request)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        ValidationResult result = new StockAdjustmentRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

        var datos = TokenData.GetData(HttpContext);
        return await _stockMovementApplication.CreateAdjustment(request, datos.UserId);
    }
}
