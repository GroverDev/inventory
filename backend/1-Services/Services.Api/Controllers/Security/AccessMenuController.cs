using Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seguridad.Application;
using Seguridad.Domain;
using Services.Api.Utils;

namespace Services.Api.Controllers.Security;

[Authorize]
[ApiExplorerSettings(GroupName = "SECURITY")]
[Route("api/[controller]")]
[ApiController]
public class AccessMenuController(IAccessMenuApplication _accessMenuApplication) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Response<List<AccessMenu>>>> Get()
    {
        if (!TokenData.GetData(HttpContext).ok) return Unauthorized("Acceso no Autorizado.");
        var datos = TokenData.GetData(HttpContext);
        var resp = await _accessMenuApplication.GetAccesMenuXUserId(datos.UserId);
        AddObjectInMenu(resp.Data??[]);
        resp.Data = resp.Data!.Where(x => x.IdFormularioPadre == 0).ToList();
        return Ok(resp);
    }

    private void AddObjectInMenu(List<AccessMenu> vListaAccesos)
    {
        foreach (var acceso in vListaAccesos)
        {
            var hijos = vListaAccesos.Where(x => x.IdFormularioPadre == acceso.IdFormulario).ToList();
            if (hijos.Count > 0)
            {
                acceso.dataToggle = true;
                acceso.classItem = "nav-link collapsed";
                acceso.dataTarget = acceso.IdFormulario;

                AddObjectInMenu(hijos);
                foreach (var hijo in hijos)
                {
                    acceso.Children.Add(hijo);
                }
            }
        }
    }
}
