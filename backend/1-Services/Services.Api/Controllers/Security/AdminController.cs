using Common.Utilities;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Seguridad.Application;
using Seguridad.Domain.Entities.requests;
using Services.Api.Utils;

namespace Services.Api.Controllers.Security;

[Authorize]
[ApiExplorerSettings(GroupName = "SECURITY")]
[Route("api/[controller]")]
[ApiController]
public class AdminController(IAdminApplication _adminApplication) : ControllerBase
{
    // POST api/Admin/ResetCompany
    // Operación destructiva: reinicia toda la base para una empresa nueva. Solo rol SuperAdmin.
    [HttpPost("ResetCompany")]
    public async Task<ActionResult<Response<bool>>> ResetCompany([FromBody] ResetCompanyRequest request)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        ValidationResult result = new ResetCompanyRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<bool>.GetResponse(result.Errors);

        var resp = await _adminApplication.ResetCompany(request, datos.UserId);
        return Ok(resp);
    }
}
