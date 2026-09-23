using Common.Utilities;
using FluentValidation.Results;
using Inventory.Application;
using Inventory.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seguridad.Application;
using Services.Api.Utils;

namespace Services.Api.Controllers.Inventory;

/// <summary>
/// Traspasos de stock entre sucursales. Todo se resuelve desde la sucursal
/// activa: se arma y envía en el origen, se recibe en el destino.
/// </summary>
[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class StockTransferController(IStockTransferApplication _application, IRolesApplication _rolesApplication) : ControllerBase
{
    private const string FormRoute = "stock-transfers";

    // GET api/StockTransfer?dateFrom=2026-09-01&dateTo=2026-09-30&status=
    [HttpGet]
    public async Task<ActionResult<Response<List<StockTransferResponse>>>> GetTransfers(
        DateOnly? dateFrom = null, DateOnly? dateTo = null, string status = "")
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "read"))
            return Denegado<List<StockTransferResponse>>("ver traspasos");

        // Días del calendario boliviano, convertidos a instantes UTC (ver BusinessTime).
        var hasta = dateTo ?? DateOnly.FromDateTime(BusinessTime.Today);
        var desde = dateFrom ?? hasta.AddDays(-30);
        return await _application.GetTransfers(
            BusinessTime.StartOfDayUtc(desde.ToDateTime(TimeOnly.MinValue)),
            BusinessTime.EndOfDayUtcExclusive(hasta.ToDateTime(TimeOnly.MinValue)),
            status ?? "");
    }

    // GET api/StockTransfer/guid
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Response<StockTransferResponse>>> GetTransfer(Guid id)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "read"))
            return Denegado<StockTransferResponse>("ver traspasos");

        return await _application.GetTransfer(id);
    }

    // POST api/StockTransfer
    [HttpPost]
    public async Task<ActionResult<Response<string>>> CreateDraft([FromBody] StockTransferRequest request)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "create"))
            return Denegado<string>("crear traspasos");

        ValidationResult result = new StockTransferRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidationString.GetResponseString(result.Errors);

        return await _application.CreateDraft(request, datos.UserId);
    }

    // PUT api/StockTransfer/guid
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Response<bool>>> UpdateDraft(Guid id, [FromBody] StockTransferRequest request)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return Denegado<bool>("modificar traspasos");

        ValidationResult result = new StockTransferRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

        return await _application.UpdateDraft(id, request, datos.UserId);
    }

    // POST api/StockTransfer/guid/send
    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult<Response<bool>>> Send(Guid id)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return Denegado<bool>("enviar traspasos");

        return await _application.Send(id, datos.UserId);
    }

    // POST api/StockTransfer/guid/receive
    [HttpPost("{id:guid}/receive")]
    public async Task<ActionResult<Response<bool>>> Receive(Guid id, [FromBody] ReceiveStockTransferRequest request)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return Denegado<bool>("recibir traspasos");

        return await _application.Receive(id, request ?? new(), datos.UserId);
    }

    // DELETE api/StockTransfer/guid  (anula un borrador)
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Response<bool>>> Cancel(Guid id)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "delete"))
            return Denegado<bool>("anular traspasos");

        return await _application.Cancel(id, datos.UserId);
    }

    private static Response<T> Denegado<T>(string accion) => new()
    {
        ok = false,
        Message = new Msg { MessageType = "warning", Description = $"No tiene permiso para {accion}." }
    };
}
