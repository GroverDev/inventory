
using Common.Utilities;
using FluentValidation.Results;
using Inventory.Application;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seguridad.Application;
using Services.Api.Utils;

namespace Services.Api.Controllers.Inventory;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class ProductController(IProductApplication _productApplication, IRolesApplication _rolesApplication) : ControllerBase
{
    // Formulario (route) al que pertenece este controlador, usado para verificar permisos por acción.
    private const string FormRoute = "products-admin";
    // POST api/Product
    [HttpPost()]
    public async Task<ActionResult<Response<string>>> CreateProduct([FromBody] ProductRequest productRequest)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "create"))
            return new Response<string>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para crear productos." } };

        if (!Guid.TryParse(productRequest.Id, out _)) productRequest.Id = "";
        if (!Guid.TryParse(productRequest.LaboratoryId, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Laboratory ID no valido" } });
        if (!Guid.TryParse(productRequest.UomId, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "El Id de la unidad de medida es no valido" } });

        ValidationResult result = new ProductRequestValidator().Validate(productRequest);
        if (!result.IsValid) return ErrorsValidationString.GetResponseString(result.Errors);

        var respuesta = await _productApplication.CreateProduct(productRequest, datos.UserId); 
        return respuesta;
    }

    // PUT api/Product/bulk
    [HttpPut("bulk")]
    public async Task<ActionResult<Response<int>>> BulkUpdateProducts([FromBody] List<ProductBulkUpdateRequest> items)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return new Response<int>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para actualizar productos." } };

        if (items == null || items.Count == 0)
            return BadRequest(new Response<int>() { Message = new Msg() { MessageType = "error", Description = "La lista de productos no puede estar vacía." } });

        if (items.Count > 500)
            return BadRequest(new Response<int>() { Message = new Msg() { MessageType = "error", Description = "No se pueden actualizar más de 500 productos a la vez." } });

        var validator = new ProductBulkUpdateRequestValidator();
        foreach (var item in items)
        {
            ValidationResult result = validator.Validate(item);
            if (!result.IsValid) return ErrorsValidation<int>.GetResponse(result.Errors);
        }

        var respuesta = await _productApplication.BulkUpdateProducts(items, datos.UserId);
        return respuesta;
    }

    // PUT api/Product/5
    [HttpPut("{id}")]
    public async Task<ActionResult<Response<bool>>> UpdateProduct(string id, [FromBody] ProductRequest productRequest)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
       
        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return new Response<bool>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para editar productos." } };

        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });
        if (!Guid.TryParse(productRequest.LaboratoryId, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "ProviderId no valido" } });
        if (!Guid.TryParse(productRequest.UomId, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "El Id de la unidad de medida es no valido" } });

         ValidationResult result = new ProductRequestValidator().Validate(productRequest);
        if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);


        productRequest.Id = id;
        var respuesta = await _productApplication.UpdateProduct(productRequest, datos.UserId);
        return respuesta;
    }

    // DELETE api/Product/5
    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> Delete(string id)
    {
        if(!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "delete"))
            return new Response<bool>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para eliminar productos." } };

        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        var respuesta = await _productApplication.DeleteProduct(id, datos.UserId);
        return respuesta;
    }

    // GET: api/Product
    [HttpGet]
    public async Task<ActionResult<Response<List<ProductResponse>>>> GetProducts(string productName="")
    {
       if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        if(productName == "ALL")   productName = "";
        if (productName.Length > 100) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "El nombre del producto no puede tener mas de 100 caracteres" } });
        var respuesta = await _productApplication.GetProducts(productName);
        return respuesta;
    }

    // GET api/Product/stock?productName=&page=1&pageSize=15
    [HttpGet("stock")]
    public async Task<ActionResult<PagedResponse<List<ProductResponse>>>> GetProductsStock(
        string productName = "", int page = 1, int pageSize = 15)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 15;
        return await _productApplication.GetProductsStock(productName, page, pageSize);
    }

    // GET api/Product/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<ProductResponse>>> GetProduct(string id)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        var respuesta = await _productApplication.GetProduct(id);
        return respuesta;
    }

    [HttpGet("{id}/validate")]
    public async Task<ActionResult<Response<ProductStockPriceResponse>>> GetProductStockPrice(string id)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        
        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        var respuesta = await _productApplication.GetProductStockPrice(id);
        return respuesta;
    }
}

