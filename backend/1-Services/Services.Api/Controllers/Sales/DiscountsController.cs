using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Utilities;
using Inventory.Application.Interfaces;
using Inventory.Domain;
using Services.Api.Utils;

namespace Services.Api.Controllers.Sales;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class DiscountsController(IDiscountApplication _discountApplication) : ControllerBase
{
    // GET api/Discounts
    [HttpGet]
    public async Task<ActionResult<Response<List<DiscountResponse>>>> GetDiscounts()
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        return await _discountApplication.GetDiscounts();
    }

    // GET api/Discounts/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<DiscountResponse>>> GetDiscount(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        return await _discountApplication.GetDiscount(id);
    }

    // POST api/Discounts
    [HttpPost]
    public async Task<ActionResult<Response<string>>> CreateDiscount([FromBody] DiscountRequest request)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        return await _discountApplication.CreateDiscount(request, datos.UserId);
    }

    // PUT api/Discounts/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<Response<bool>>> UpdateDiscount(string id, [FromBody] DiscountRequest request)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        return await _discountApplication.UpdateDiscount(id, request, datos.UserId);
    }

    // DELETE api/Discounts/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> DeleteDiscount(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        return await _discountApplication.DeleteDiscount(id, datos.UserId);
    }
}
