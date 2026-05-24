using Common.Utilities;
using Inventory.Application.Interfaces;
using Inventory.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Api.Utils;

namespace Services.Api.Controllers.Inventory;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class DashboardController(IDashboardApplication _dashboardApplication) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Response<DashboardResponse>>> GetDashboard()
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        return await _dashboardApplication.GetDashboard();
    }
}
