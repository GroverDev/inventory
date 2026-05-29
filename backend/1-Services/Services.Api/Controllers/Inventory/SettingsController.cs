using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Common.Utilities;
using Inventory.Application;
using Inventory.Domain;
using Services.Api.Utils;

namespace Services.Api.Controllers.Inventory;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class SettingsController(IOptions<PosSettings> _posSettings) : ControllerBase
{
    // GET api/Settings/pos
    [HttpGet("pos")]
    public ActionResult<Response<PosSettingsResponse>> GetPosSettings()
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        return new Response<PosSettingsResponse>
        {
            ok = true,
            Data = new PosSettingsResponse
            {
                MaxCashierDiscountPct    = _posSettings.Value.MaxCashierDiscountPct,
                MaxCashierDiscountAmount = _posSettings.Value.MaxCashierDiscountAmount,
            }
        };
    }
}
