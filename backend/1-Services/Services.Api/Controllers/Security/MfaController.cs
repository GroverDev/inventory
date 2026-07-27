using Common.Utilities;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QRCoder;
using Seguridad.Application;
using Seguridad.Domain;
using Services.Api.jwt;
using Services.Api.Utils;
using Microsoft.Extensions.Options;

namespace Services.Api.Controllers.Security;

[ApiExplorerSettings(GroupName = "SECURITY")]
[Route("api/[controller]")]
[ApiController]
public class MfaController(
    IMfaApplication _mfaApplication,
    IAuthenticationApplication _authenticationApplication,
    IOptions<JwtSettings> jwtSettings) : ControllerBase
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    /// <summary>
    /// Completa la respuesta con el JWT y, para clientes que lo soportan, un
    /// refresh token. Es la vía normal del móvil cuando el 2FA está activo.
    /// </summary>
    private async Task IssueTokens(LoginResponse data, Seguridad.Domain.Enums.InicioSesionDesde from, string device)
    {
        bool refreshable = LoginController.UsesRefreshToken(from);

        data.Token = TokenJwt.GetToken(data, _jwtSettings.Secret,
            refreshable ? _jwtSettings.TimeTokenRefreshable : _jwtSettings.TimeToken);

        if (refreshable)
            data.RefreshToken = await _authenticationApplication.IssueRefreshToken(
                data.UserId, device,
                Enum.GetName(typeof(Seguridad.Domain.Enums.InicioSesionDesde), from) ?? "",
                _jwtSettings.RefreshTokenDays);
    }

    [Authorize]
    [HttpGet("setup")]
    public async Task<ActionResult<Response<TotpSetupResponse>>> Setup()
    {
        var tokenData = TokenData.GetData(HttpContext);
        if (!tokenData.ok) return Unauthorized();

        var resp = await _mfaApplication.SetupTotp(tokenData.UserId, tokenData.Email);
        if (resp.ok && resp.Data != null)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(resp.Data.QrCodeUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            resp.Data.QrCodeBase64 = Convert.ToBase64String(qrCode.GetGraphic(5));
        }
        return Ok(resp);
    }

    [Authorize]
    [HttpPost("enable")]
    public async Task<ActionResult<Response<MfaEnableResponse>>> Enable([FromBody] TotpEnableRequest request)
    {
        ValidationResult result = new TotpEnableRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<MfaEnableResponse>.GetResponse(result.Errors);

        var tokenData = TokenData.GetData(HttpContext);
        if (!tokenData.ok) return Unauthorized();

        var resp = await _mfaApplication.EnableTotp(tokenData.UserId, request.Code);
        return Ok(resp);
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("verify")]
    public async Task<ActionResult<Response<LoginResponse>>> Verify([FromBody] TotpVerifyRequest request)
    {
        ValidationResult result = new TotpVerifyRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<LoginResponse>.GetResponse(result.Errors);

        var userId = TokenJwt.ValidateTotpPendingToken(request.TotpSessionToken, _jwtSettings.Secret);
        if (userId == null)
        {
            var errResp = new Response<LoginResponse>();
            errResp.SetMessage(MessageTypes.Warning, "Token de sesión MFA inválido o expirado.");
            return Ok(errResp);
        }

        var resp = await _mfaApplication.VerifyTotpAndCompleteLogin(userId.Value, request);
        if (resp.ok)
            await IssueTokens(resp.Data!, request.LoginFrom, request.Device);

        return Ok(resp);
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("verify-recovery")]
    public async Task<ActionResult<Response<LoginResponse>>> VerifyRecovery([FromBody] MfaRecoveryRequest request)
    {
        ValidationResult result = new MfaRecoveryRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<LoginResponse>.GetResponse(result.Errors);

        var userId = TokenJwt.ValidateTotpPendingToken(request.TotpSessionToken, _jwtSettings.Secret);
        if (userId == null)
        {
            var errResp = new Response<LoginResponse>();
            errResp.SetMessage(MessageTypes.Warning, "Token de sesión MFA inválido o expirado.");
            return Ok(errResp);
        }

        var resp = await _mfaApplication.VerifyRecoveryAndCompleteLogin(userId.Value, request);
        if (resp.ok)
            await IssueTokens(resp.Data!, request.LoginFrom, request.Device);

        return Ok(resp);
    }

    [Authorize]
    [HttpDelete]
    public async Task<ActionResult<Response<bool>>> Disable()
    {
        var tokenData = TokenData.GetData(HttpContext);
        if (!tokenData.ok) return Unauthorized();

        var resp = await _mfaApplication.DisableTotp(tokenData.UserId);
        return Ok(resp);
    }
}
