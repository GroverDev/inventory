
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
public class ProductController(IProductApplication _productApplication, IRolesApplication _rolesApplication,
                               IProductBranchSettingsApplication _branchSettingsApplication,
                               IBranchApplication _branchApplication) : ControllerBase
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
        // El laboratorio es opcional: vacío es válido. Solo se rechaza un valor
        // presente pero mal formado, que sí es un error del cliente.
        if (!string.IsNullOrWhiteSpace(productRequest.LaboratoryId) && !Guid.TryParse(productRequest.LaboratoryId, out _))
            return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Laboratory ID no valido" } });
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
        if (!string.IsNullOrWhiteSpace(productRequest.LaboratoryId) && !Guid.TryParse(productRequest.LaboratoryId, out _))
            return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Laboratory ID no valido" } });
        if (!Guid.TryParse(productRequest.UomId, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "El Id de la unidad de medida es no valido" } });

         ValidationResult result = new ProductRequestValidator().Validate(productRequest);
        if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);


        productRequest.Id = id;
        var respuesta = await _productApplication.UpdateProduct(productRequest, datos.UserId);
        return respuesta;
    }

    // POST api/Product/5/tracking?modo=lot|serial
    // Activa el seguimiento por lotes o por números de serie. Va aparte del PUT
    // porque no es un campo más de la ficha: es irreversible y toca las
    // existencias, así que la UI lo pide como una acción deliberada y no como un
    // guardado cualquiera.
    [HttpPost("{id}/tracking")]
    public async Task<ActionResult<Response<bool>>> ActivateTracking(string id, [FromQuery] string modo = "lot")
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return new Response<bool>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para editar productos." } };

        if (!Guid.TryParse(id, out _)) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "Id no valido" } });

        return await _productApplication.ActivateTracking(id, modo);
    }

    // GET api/Product/guid/branch-settings
    // Precio, mínimo y stock del producto en cada sucursal activa.
    [HttpGet("{id:guid}/branch-settings")]
    public async Task<ActionResult<Response<List<ProductBranchSettingResponse>>>> GetBranchSettings(Guid id)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        return await _branchSettingsApplication.GetSettings(id);
    }

    // PUT api/Product/guid/branch-settings
    // Excepciones de precio y mínimo por sucursal. Una fila con los dos valores
    // vacíos devuelve esa sucursal a los valores base.
    [HttpPut("{id:guid}/branch-settings")]
    public async Task<ActionResult<Response<bool>>> SaveBranchSettings(Guid id, [FromBody] List<ProductBranchSettingRequest> settings)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return new Response<bool>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para editar productos." } };

        ValidationResult result = new ProductBranchSettingRequestValidator().Validate(settings ?? []);
        if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

        return await _branchSettingsApplication.SaveSettings(id, settings ?? [], datos.UserId);
    }

    // Tope de la subida. Se aplica al request completo (multipart incluido), así
    // que queda un poco por encima del archivo permitido.
    private const long MaxImageBytes = 5 * 1024 * 1024;

    // POST api/Product/guid/image  (multipart/form-data, campo "file")
    // Devuelve la ruta relativa de la imagen; el cliente la une a la URL de medios.
    [HttpPost("{id:guid}/image")]
    [RequestSizeLimit(MaxImageBytes + 64 * 1024)]
    public async Task<ActionResult<Response<string>>> UploadImage(Guid id, IFormFile? file, CancellationToken ct)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return new Response<string>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para editar productos." } };

        if (file is null || file.Length == 0)
            return BadRequest(new Response<string>() { Message = new Msg() { MessageType = "error", Description = "Debe enviar una imagen." } });
        if (file.Length > MaxImageBytes)
            return new Response<string>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "La imagen no puede pesar más de 5 MB." } };

        await using var stream = file.OpenReadStream();
        return await _productApplication.UploadImage(id, stream, datos.UserId, ct);
    }

    // DELETE api/Product/guid/image
    [HttpDelete("{id:guid}/image")]
    public async Task<ActionResult<Response<bool>>> DeleteImage(Guid id)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return new Response<bool>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para editar productos." } };

        return await _productApplication.DeleteImage(id, datos.UserId);
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
    // branch: de qué sucursales se suma el stock. Vacío = la activa (el POS y
    // los listados); un id o "all" para el reporte de stock (ver BranchScope).
    public async Task<ActionResult<Response<List<ProductResponse>>>> GetProducts(string productName="", string? branch = null)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");
        if(productName == "ALL")   productName = "";
        if (productName.Length > 100) return BadRequest(new Response<bool>() { Message = new Msg() { MessageType = "error", Description = "El nombre del producto no puede tener mas de 100 caracteres" } });

        var (branches, scopeError) = await BranchScope.Resolve(branch, datos.UserId, _branchApplication);
        if (scopeError != null) return new Response<List<ProductResponse>>() { ok = false, Message = new Msg() { MessageType = "warning", Description = scopeError } };

        var respuesta = await _productApplication.GetProducts(productName, branches);
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

