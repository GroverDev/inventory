using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation.Results;
using Common.Utilities;
using Inventory.Application;
using Inventory.Domain;
using Seguridad.Application;
using Services.Api.Utils;

namespace Services.Api.Controllers.Inventory;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class SettingsController(IDiscountLimitsApplication _discountLimits, IRolesApplication _rolesApplication,
                                ICashSessionApplication _cashSessions) : ControllerBase
{
    private const string LimitsForm = "discount-limits";
    private const string CloseForm = "cash-close-settings";

    // GET api/Settings/cash-close
    // Sin chequeo de formulario: el punto de venta la lee para saber qué medios
    // pedir al cerrar la caja.
    [HttpGet("cash-close")]
    public async Task<ActionResult<Response<CashCloseSettings>>> GetCashCloseSettings()
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        return await _cashSessions.GetCloseSettings();
    }

    // PUT api/Settings/cash-close
    [HttpPut("cash-close")]
    public async Task<ActionResult<Response<bool>>> SaveCashCloseSettings([FromBody] CashCloseSettingsRequest request)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        if (!await _rolesApplication.HasFormPermission(datos.UserId, CloseForm, "update"))
            return Denegado<bool>("modificar la configuración del cierre de caja");

        return await _cashSessions.SaveCloseSettings(request, datos.UserId);
    }

    // GET api/Settings/pos
    // Topes del descuento manual vigentes en la sucursal activa: los usa el POS
    // para saber cuándo pedir supervisor y qué no dejar pasar.
    [HttpGet("pos")]
    public async Task<ActionResult<Response<PosSettingsResponse>>> GetPosSettings()
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var limites = await _discountLimits.GetEffective();
        return new Response<PosSettingsResponse>
        {
            ok = true,
            Data = new PosSettingsResponse
            {
                MaxCashierDiscountPct    = limites.CashierMaxPct,
                MaxCashierDiscountAmount = limites.CashierMaxAmount,
                MaxDiscountPct           = limites.GeneralMaxPct,
                MaxDiscountAmount        = limites.GeneralMaxAmount,
            }
        };
    }

    // GET api/Settings/discount-limits
    // Topes por nivel: la empresa y cada sucursal, con lo que tengan cargado.
    [HttpGet("discount-limits")]
    public async Task<ActionResult<Response<List<DiscountLimitLevel>>>> GetDiscountLimits()
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        if (!await _rolesApplication.HasFormPermission(datos.UserId, LimitsForm, "read"))
            return Denegado<List<DiscountLimitLevel>>("ver los límites de descuento");

        return await _discountLimits.GetLevels();
    }

    // PUT api/Settings/discount-limits
    [HttpPut("discount-limits")]
    public async Task<ActionResult<Response<bool>>> SaveDiscountLimits([FromBody] List<DiscountLimitLevel> levels)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        if (!await _rolesApplication.HasFormPermission(datos.UserId, LimitsForm, "update"))
            return Denegado<bool>("modificar los límites de descuento");

        ValidationResult result = new DiscountLimitLevelValidator().Validate(levels ?? []);
        if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

        return await _discountLimits.SaveLevels(levels ?? [], datos.UserId);
    }

    private static Response<T> Denegado<T>(string accion) => new()
    {
        ok = false,
        Message = new Msg { MessageType = "warning", Description = $"No tiene permiso para {accion}." }
    };
}
