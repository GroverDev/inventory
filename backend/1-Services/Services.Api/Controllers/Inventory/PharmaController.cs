using Common.Utilities;
using FluentValidation.Results;
using Inventory.Application;
using Inventory.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seguridad.Application;
using Services.Api.Utils;

namespace Services.Api.Controllers.Inventory;

/// <summary>
/// Datos del rubro farmacia. Va en su propio controlador y no dentro de
/// ProductController porque es de un rubro: una ferretería no lo usa.
/// </summary>
[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class PharmaController(IPharmaApplication _pharmaApplication, IRolesApplication _rolesApplication) : ControllerBase
{
    // Se apoya en el permiso de productos: quien puede editar un producto puede
    // editar su ficha farmacéutica. No merece un permiso propio.
    private const string FormRoute = "products-admin";

    // GET api/Pharma/forms
    [HttpGet("forms")]
    public async Task<ActionResult<Response<List<PharmaCatalogItem>>>> GetForms()
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        return await _pharmaApplication.GetForms();
    }

    // GET api/Pharma/routes
    [HttpGet("routes")]
    public async Task<ActionResult<Response<List<PharmaCatalogItem>>>> GetRoutes()
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        return await _pharmaApplication.GetRoutes();
    }

    // GET api/Pharma/substances?nombre=ibu
    [HttpGet("substances")]
    public async Task<ActionResult<Response<List<PharmaSubstance>>>> SearchSubstances([FromQuery] string nombre = "")
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        return await _pharmaApplication.SearchSubstances(nombre);
    }

    // GET api/Pharma/product/{id}
    // Va antes de las rutas con verbo para que no se confunda el segmento.
    [HttpGet("product/{productId}")]
    public async Task<ActionResult<Response<ProductPharma>>> GetByProduct(string productId)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        return await _pharmaApplication.GetByProduct(productId);
    }

    // GET api/Pharma/product/{id}/equivalents
    // Equivalentes por composición: no se cargan a mano, se deducen.
    [HttpGet("product/{productId}/equivalents")]
    public async Task<ActionResult<Response<List<ProductEquivalentResponse>>>> GetEquivalents(string productId)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        return await _pharmaApplication.GetEquivalents(productId);
    }

    // POST api/Pharma/product/{id}/alternatives
    // Alternativa definida a mano. Las equivalentes por composición NO se cargan
    // acá: se deducen solas.
    [HttpPost("product/{productId}/alternatives")]
    public async Task<ActionResult<Response<bool>>> AddAlternative(string productId, [FromBody] AlternativeRequest request)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return new Response<bool>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para editar productos." } };

        return await _pharmaApplication.AddAlternative(productId, request?.AlternativeId ?? "", request?.Reason, datos.UserId);
    }

    // DELETE api/Pharma/product/{id}/alternatives/{alternativeId}
    [HttpDelete("product/{productId}/alternatives/{alternativeId}")]
    public async Task<ActionResult<Response<bool>>> RemoveAlternative(string productId, string alternativeId)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return new Response<bool>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para editar productos." } };

        return await _pharmaApplication.RemoveAlternative(productId, alternativeId);
    }

    // GET api/Pharma/product/{id}/leaflet
    // Aparte de la ficha: el texto pesa y la mayoría de los productos no lo tiene.
    [HttpGet("product/{productId}/leaflet")]
    public async Task<ActionResult<Response<string>>> GetLeaflet(string productId)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        return await _pharmaApplication.GetLeaflet(productId);
    }

    // PUT api/Pharma/product/{id}/leaflet
    [HttpPut("product/{productId}/leaflet")]
    public async Task<ActionResult<Response<bool>>> SaveLeaflet(string productId, [FromBody] LeafletRequest request)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return new Response<bool>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para editar productos." } };

        return await _pharmaApplication.SaveLeaflet(productId, request?.Content, datos.UserId);
    }

    // PUT api/Pharma/product/{id}
    [HttpPut("product/{productId}")]
    public async Task<ActionResult<Response<bool>>> Save(string productId, [FromBody] ProductPharmaRequest request)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return new Response<bool>() { ok = false, Message = new Msg() { MessageType = "warning", Description = "No tiene permiso para editar productos." } };

        ValidationResult result = new ProductPharmaRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

        return await _pharmaApplication.Save(productId, request, datos.UserId);
    }
}

/// <summary>
/// El prospecto viaja en un objeto y no como texto suelto: un string crudo en
/// el cuerpo obliga a content-type raros y se rompe con los saltos de línea.
/// </summary>
public class LeafletRequest
{
    /// <summary>Markdown. Vacío borra el prospecto.</summary>
    public string? Content { get; set; }
}

/// <summary>Alta de una alternativa definida a mano.</summary>
public class AlternativeRequest
{
    public string? AlternativeId { get; set; }

    /// <summary>"más económico", "misma acción". Para quien venda después.</summary>
    public string? Reason { get; set; }
}
