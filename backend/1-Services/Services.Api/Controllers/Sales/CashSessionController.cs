using Common.Utilities;
using Inventory.Application;
using Inventory.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Api.Utils;

namespace Services.Api.Controllers.Sales;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class CashSessionController(
    ICashSessionApplication _cashSessionApplication,
    ICashMovementApplication _cashMovementApplication) : ControllerBase
{
    // GET api/CashSession/active  — sesión activa del usuario actual
    [HttpGet("active")]
    public async Task<ActionResult<Response<CashSessionResponse>>> GetActiveSession()
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        return await _cashSessionApplication.GetActiveSession(datos.UserId);
    }

    // GET api/CashSession/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<CashSessionResponse>>> GetById(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no válido" } });
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        return await _cashSessionApplication.GetSessionById(id);
    }

    // GET api/CashSession?dateFrom=&dateTo=
    [HttpGet]
    public async Task<ActionResult<Response<List<CashSessionResponse>>>> GetSessions([FromQuery] string dateFrom, [FromQuery] string dateTo)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        return await _cashSessionApplication.GetSessions(dateFrom, dateTo, datos.UserId, datos.Roles);
    }

    // POST api/CashSession/open
    [HttpPost("open")]
    public async Task<ActionResult<Response<string>>> OpenSession([FromBody] OpenCashSessionRequest request)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        return await _cashSessionApplication.OpenSession(request, datos.UserId);
    }

    // PUT api/CashSession/{id}/close
    [HttpPut("{id}/close")]
    public async Task<ActionResult<Response<CashSessionResponse>>> CloseSession(string id, [FromBody] CloseCashSessionRequest request)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no válido" } });
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        return await _cashSessionApplication.CloseSession(id, request, datos.UserId);
    }

    // GET api/CashSession/{id}/sales
    [HttpGet("{id}/sales")]
    public async Task<ActionResult<Response<List<SaleProductResponse>>>> GetSessionSales(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no válido" } });
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        return await _cashSessionApplication.GetSessionSales(id);
    }

    // POST api/CashSession/{id}/movements
    [HttpPost("{id}/movements")]
    public async Task<ActionResult<Response<string>>> AddMovement(string id, [FromBody] CashMovementRequest request)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no válido" } });
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        request.CashSessionId = id;
        return await _cashMovementApplication.CreateMovement(request, datos.UserId);
    }
}
