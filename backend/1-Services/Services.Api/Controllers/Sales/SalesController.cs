using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Common.Utilities;
using Inventory.Application;
using Inventory.Domain;
using Seguridad.Application;
using Services.Api.Utils;
using Services.Api.jwt;

namespace Services.Api.Controllers.Sales;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class SalesController(
    global::Inventory.Application.IBranchApplication _branchApplication,
    ISalesApplication _salesApplication,
    IRolesApplication _rolesApplication,
    IOptions<JwtSettings> _jwtSettings) : ControllerBase
{
    private const string FormRoute = "sales-admin";

    // POST api/Sales
    [HttpPost()]
    public async Task<ActionResult<Response<string>>> CreateSale([FromBody] SaleRequest saleRequest)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!Guid.TryParse(saleRequest.CustomerId, out _))
            return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Customer ID no valido" } });

        foreach (var item in saleRequest.Detail)
        {
            if (!Guid.TryParse(item.ProductId, out _))
                return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Product ID no valido" } });
        }

        bool supervisorApproved = SupervisorToken.Validate(saleRequest.SupervisorAuthToken, _jwtSettings.Value.Secret, datos.TenantId) is not null;

        var respuesta = await _salesApplication.CreateSale(saleRequest, datos.UserId, datos.Roles, supervisorApproved);
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
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        // Eliminar una venta la saca de los totales sin reponer stock ni mover
        // caja: es una operación de corrección, no la vía normal para deshacer
        // una venta (para eso está la devolución).
        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "delete"))
            return new Response<bool>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para eliminar ventas." } };

        var respuesta = await _salesApplication.DeleteSale(id, datos.UserId);
        return respuesta;
    }

    // GET: api/Sales
    [HttpGet]
    public async Task<ActionResult<Response<SalesPagedResponse>>> GetSales(
        string saleDateInitial, string saleDateEnd,
        int page = 1, int pageSize = 50, string? sellerName = null, string? branch = null)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        // branch: vacío = sucursal activa, un id o "all" (ver BranchScope).
        var (branches, scopeError) = await BranchScope.Resolve(branch, datos.UserId, _branchApplication);
        if (scopeError != null) return new Response<SalesPagedResponse>() { ok = false, Message = new Msg() { MessageType = "warning", Description = scopeError } };

        return await _salesApplication.GetSales(saleDateInitial, saleDateEnd, datos.UserId, datos.Roles, page, pageSize, sellerName, branches);
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
