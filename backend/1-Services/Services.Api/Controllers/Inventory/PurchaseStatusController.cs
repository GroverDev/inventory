using Common.Utilities;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Services.Api.Controllers.Inventory;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class PurchaseStatusController(IPurchaseStatusApplication _purchaseStatusApplication) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Response<List<PurchaseStatusResponse>>>> GetPurchasesStatus()
    {
        // if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var respuesta = await  _purchaseStatusApplication.GetPurchaseStatus( );
        return respuesta;
    }

}
