using Common.Utilities;
using FluentValidation.Results;
using Inventory.Application;
using Inventory.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seguridad.Application;
using Services.Api.Utils;

namespace Services.Api.Controllers.Inventory;

[ApiExplorerSettings(GroupName = "POS")]
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class BranchController(IBranchApplication _branchApplication, IRolesApplication _rolesApplication) : ControllerBase
{
    // Formulario (route) al que pertenece este controlador, usado para verificar permisos por acción.
    private const string FormRoute = "branches-admin";

    // POST api/Branch
    [HttpPost]
    public async Task<ActionResult<Response<string>>> CreateBranch([FromBody] BranchRequest branchRequest)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "create"))
            return Denegado<string>("crear sucursales");

        ValidationResult result = new BranchRequestValidator().Validate(branchRequest);
        if (!result.IsValid) return ErrorsValidationString.GetResponseString(result.Errors);

        return await _branchApplication.CreateBranch(branchRequest, datos.UserId);
    }

    // PUT api/Branch/guid
    [HttpPut("{id}")]
    public async Task<ActionResult<Response<bool>>> UpdateBranch(Guid id, [FromBody] BranchRequest branchRequest)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "update"))
            return Denegado<bool>("modificar sucursales");

        ValidationResult result = new BranchRequestValidator().Validate(branchRequest);
        if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

        // El id de la ruta manda: el del cuerpo no puede apuntar a otra sucursal.
        branchRequest.Id = id.ToString();
        return await _branchApplication.UpdateBranch(branchRequest, datos.UserId);
    }

    // DELETE api/Branch/guid
    [HttpDelete("{id}")]
    public async Task<ActionResult<Response<bool>>> DeleteBranch(Guid id)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        if (!await _rolesApplication.HasFormPermission(datos.UserId, FormRoute, "delete"))
            return Denegado<bool>("eliminar sucursales");

        return await _branchApplication.DeleteBranch(id, datos.UserId);
    }

    // GET api/Branch?name=&includeInactive=
    // Sin chequeo de formulario: la lista la necesitan también la ficha de
    // usuarios y los traspasos, no solo la pantalla de sucursales.
    [HttpGet]
    public async Task<ActionResult<Response<List<BranchRequest>>>> GetBranches(string name = "", bool includeInactive = false)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        return await _branchApplication.GetBranches(name ?? "", includeInactive);
    }

    // GET api/Branch/guid
    [HttpGet("{id}")]
    public async Task<ActionResult<Response<BranchRequest>>> GetBranch(Guid id)
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");

        return await _branchApplication.GetBranch(id);
    }

    private static Response<T> Denegado<T>(string accion) => new()
    {
        ok = false,
        Message = new Msg { MessageType = "warning", Description = $"No tiene permiso para {accion}." }
    };
}
