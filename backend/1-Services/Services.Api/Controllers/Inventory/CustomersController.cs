
using Common.Utilities;
using Inventory.Application;
using Inventory.Domain.Entities.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Api.Utils;

namespace Services.Api.Controllers.Inventory;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class CustomersController(ICustomersApplication _customersApplication) : ControllerBase
{
    // POST api/Client
    [HttpPost()]
    public async Task<ActionResult<Response<bool>>> CreateCustomer([FromBody] CustomerRequest customerVM)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        var respuesta = await _customersApplication.CreateCustomer(customerVM, datos.UserId);
        return respuesta;
    }

    // PUT api/Client/guid
    [HttpPut("{id}")]
    public async Task<ActionResult<Response<bool>>> UpdateCustomer(string id, [FromBody] CustomerRequest customerVM)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        customerVM.Id = id;
        var respuesta = await _customersApplication.UpdateCustomer(customerVM, datos.UserId);
        return respuesta;
    }

    // DELETE api/Client/GUID
    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> DeleteCustomer(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        var respuesta = await _customersApplication.DeleteCustomer(id, datos.UserId);
        return respuesta;
    }

    // GET: api/Client
    [HttpGet]
    public async Task<ActionResult<Response<List<CustomerRequest>>>> GetCustomers(string CustomerName)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var respuesta = await _customersApplication.GetCustomers(CustomerName);
        return respuesta;
    }

    // GET api/Client/GUID
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<CustomerRequest>>> GetCustomer(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var respuesta = await _customersApplication.GetCustomer(id);
        return respuesta;
    }
}

