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

    // GET api/StockMovement/serials/{productId}
    // Unidades serializadas disponibles, para que el mostrador elija cuál entrega.
    //
    // Dos segmentos, así que no compite con la ruta {productId} de abajo.
    [HttpGet("serials/{productId}")]
    public async Task<ActionResult<Response<List<StockSerialResponse>>>> GetAvailableSerials(string productId)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        return await _stockMovementApplication.GetAvailableSerials(productId);
    }

    // GET api/StockMovement/{productId}?stockItemId=...
    // stockItemId es opcional: sin él, el historial completo del producto (todos
    // sus lotes mezclados); con él, el kardex de una existencia puntual.
    [HttpGet("{productId}")]
    public async Task<ActionResult<Response<List<StockMovementResponse>>>> GetMovements(string productId, [FromQuery] string? stockItemId = null)
    {
        if (!Guid.TryParse(productId, out _))
            return BadRequest(new Response<List<StockMovementResponse>>() { Message = new Msg() { MessageType = "error", Description = "Id de producto inválido." } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        return await _stockMovementApplication.GetMovementsByProduct(productId, stockItemId);
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

    // POST api/StockMovement/write-off
    [HttpPost("write-off")]
    public async Task<ActionResult<Response<bool>>> CreateWriteOff([FromBody] StockWriteOffRequest request)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        ValidationResult result = new StockWriteOffRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

        var datos = TokenData.GetData(HttpContext);
        return await _stockMovementApplication.CreateWriteOff(request, datos.UserId);
    }

    // GET api/StockMovement/write-offs?desde=&hasta=&productId=
    [HttpGet("write-offs")]
    public async Task<ActionResult<Response<WriteOffReportResponse>>> GetWriteOffs(
        [FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] string? productId = null)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        return await _stockMovementApplication.GetWriteOffs(desde, hasta, productId);
    }
}
