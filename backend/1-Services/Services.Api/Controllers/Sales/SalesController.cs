using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Common.Utilities;
using Inventory.Application;
using Inventory.Domain;
using Services.Api.Utils;
using Services.Api.jwt;

namespace Services.Api.Controllers.Sales;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class SalesController(
    ISalesApplication _salesApplication,
    IOptions<JwtSettings> _jwtSettings) : ControllerBase
{
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

        bool supervisorApproved = ValidateSupervisorToken(saleRequest.SupervisorAuthToken);

        var respuesta = await _salesApplication.CreateSale(saleRequest, datos.UserId, datos.Rol, supervisorApproved);
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
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        var respuesta = await _salesApplication.DeleteSale(id, 1);
        return respuesta;
    }

    // GET: api/Sales
    [HttpGet]
    public async Task<ActionResult<Response<SalesPagedResponse>>> GetSales(
        string saleDateInitial, string saleDateEnd,
        int page = 1, int pageSize = 50, string? sellerName = null)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        return await _salesApplication.GetSales(saleDateInitial, saleDateEnd, datos.UserId, datos.Rol, page, pageSize, sellerName);
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

    // Valida que el token pertenezca a un usuario con rol distinto a Cajero
    private bool ValidateSupervisorToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.Secret));
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out _);

            var jwt = handler.ReadJwtToken(token);
            var role = jwt.Claims.FirstOrDefault(c => c.Type == "Rol")?.Value;
            return !string.IsNullOrEmpty(role) && role != "Cajero";
        }
        catch { return false; }
    }
}
