using Common.Utilities;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Seguridad.Application;
using Seguridad.Domain;
using Services.Api.jwt;
using Services.Api.Utils;

namespace Services.Api.Controllers.Security;

[ApiExplorerSettings(GroupName = "SECURITY")]
[Route("api/[controller]")]
[ApiController]
public class LoginController(IAuthenticationApplication _authenticationApplication, IOptions<JwtSettings> jwtSettings) : ControllerBase
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<Response<LoginResponse>>> Authenticate([FromBody] LoginRequest login)
    {
        ValidationResult result = new LoginRequestValidator().Validate(login);
        if (!result.IsValid) return ErrorsValidation<LoginResponse>.GetResponse(result.Errors);

        var resp = await _authenticationApplication.Login(login);
        if (resp.ok)
        {
            if (resp.Data!.RequireTotp)
            {
                resp.Data.TotpSessionToken = TokenJwt.GetTotpPendingToken(resp.Data.UserId, _jwtSettings.Secret);
                resp.Data.UserId = 0;
            }
            else
            {
                resp.Data.Token = TokenJwt.GetToken(resp.Data, _jwtSettings.Secret, _jwtSettings.TimeToken);
            }
        }

        return Ok(resp);
    }
}
