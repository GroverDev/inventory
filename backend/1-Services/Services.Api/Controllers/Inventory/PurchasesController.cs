
using Common.Utilities;
using Inventory.Application.Interfaces;
using Inventory.Domain;
using Inventory.Domain.Entities.Requests;
using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace Services.Api.Controllers.Inventory;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class PurchasesController(IPurchaseApplication _purchaseApplication) : ControllerBase
{
    // POST api/Purchases
    [HttpPost()]
    public async Task<ActionResult<Response<bool>>> CreatePurchase([FromBody] PurchaseRequest purchaseRequest)
    {
        // if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        // var datos = TokenData.GetData(HttpContext);

        var respuesta = await _purchaseApplication.CreatePurchase(purchaseRequest, 1); // datos.IdUsuario);
        return respuesta;
    }

    //// PUT api/Purchases/guid
    [HttpPut("{id}")]
    public async Task<ActionResult<Response<bool>>> UpdatePurchase(string id, [FromBody] PurchaseRequest purchaseRequest)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        // if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        // var datos = TokenData.GetData(HttpContext);

        var respuesta = await _purchaseApplication.UpdatePurchase(purchaseRequest, 1);// datos.IdUsuario);
        return respuesta;
    }

    // DELETE api/Purchases/GUID
    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> DeletePurchase(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        // if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        // var datos = TokenData.GetData(HttpContext);

        var respuesta = await _purchaseApplication.DeletePurchase(id, 1); //datos.IdUsuario);
        return respuesta;
    }

    // GET: api/Purchases
    [HttpGet]
    public async Task<ActionResult<Response<List<PurchaseProductResponse>>>> GetPurchases(string purchaseDateInitial, string purchaseDateEnd, PurchaseStatusEnum purchaseStatus)
    {
        // if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        // var datos = TokenData.GetData(HttpContext);

        var respuesta = await _purchaseApplication.GetPurchases(purchaseDateInitial, purchaseDateEnd, purchaseStatus);
        return respuesta;
    }

    // GET api/Purchases/GUID
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<PurchaseRequest>>> GetPurchase(string id)
    {
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });
        
        // if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        // var datos = TokenData.GetData(HttpContext);

        var respuesta = await _purchaseApplication.GetPurchase(id);
        return respuesta;
    }

    // PUT api/Purchases/reciveOrders/id
    [HttpPut("reciveOrders/{id}")]
    public async Task<ActionResult<Response<bool>>> reciveOrders(string id, [FromBody] PurchaseDeliveryRequest purchaseDeliveryRequest)
    {
        if (!Guid.TryParse(purchaseDeliveryRequest.PurchaseId, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        // if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        // var datos = TokenData.GetData(HttpContext);

        var respuesta = await _purchaseApplication.ReceiveOrders(purchaseDeliveryRequest, 1);//datos.IdUsuario);
        return respuesta;
    }
}

