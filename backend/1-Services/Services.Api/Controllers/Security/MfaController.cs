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
    /// Si <paramref name="rememberDevice"/> viene marcado, además emite un
    /// token de dispositivo de confianza para saltar el TOTP en logins futuros.
    /// </summary>
    private async Task IssueTokens(
        LoginResponse data, Seguridad.Domain.Enums.InicioSesionDesde from, string device, bool rememberDevice)
    {
        bool refreshable = LoginController.UsesRefreshToken(from);

        data.Token = TokenJwt.GetToken(data, _jwtSettings.Secret,
            refreshable ? _jwtSettings.TimeTokenRefreshable : _jwtSettings.TimeToken);

        if (refreshable)
        {
            string rawRefresh = await _authenticationApplication.IssueRefreshToken(
                data.UserId, data.TenantId, data.SesionId, device,
                Enum.GetName(typeof(Seguridad.Domain.Enums.InicioSesionDesde), from) ?? "",
                _jwtSettings.RefreshTokenDays);

            // Misma regla que LoginController: en web va en cookie HttpOnly, no
            // en el cuerpo. Antes de esto viajaba siempre en el cuerpo, así que
            // un login completado por TOTP le entregaba el refresh token a JS
            // (y a cualquier XSS) en vez de guardarlo fuera de su alcance.
            if (LoginController.UsesRefreshCookie(from))
            {
                Response.Cookies.Append(LoginController.RefreshCookie, rawRefresh, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/api/Login",
                    Expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
                });
            }
            else
            {
                data.RefreshToken = rawRefresh;
            }
        }

        if (!rememberDevice) return;

        string raw = await _authenticationApplication.IssueTrustedDevice(
            data.UserId, data.TenantId, device, _jwtSettings.TrustedDeviceDays);

        if (LoginController.UsesRefreshCookie(from))
        {
            // Misma cookie que el refresh token: HttpOnly, alcance /api/Login,
            // que es donde LoginController la lee en el siguiente intento.
            Response.Cookies.Append(LoginController.DeviceTrustCookie, raw, new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/api/Login",
                Expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.TrustedDeviceDays)
            });
        }
        else
        {
            // Móvil: no hay cookie, el valor en claro viaja en el cuerpo para
            // que la app lo guarde y lo reenvíe en el próximo LoginRequest.
            data.DeviceTrustToken = raw;
        }
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
            await IssueTokens(resp.Data!, request.LoginFrom, request.Device, request.RememberDevice);

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
            await IssueTokens(resp.Data!, request.LoginFrom, request.Device, request.RememberDevice);

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

    /// <summary>Autogestión: los dispositivos de confianza del propio usuario, sin exponer los de nadie más.</summary>
    [Authorize]
    [HttpGet("devices")]
    public async Task<ActionResult<Response<List<TrustedDeviceResponse>>>> GetDevices()
    {
        var tokenData = TokenData.GetData(HttpContext);
        if (!tokenData.ok) return Unauthorized();

        return Ok(await _authenticationApplication.GetTrustedDevices(tokenData.UserId));
    }

    /// <summary>Olvida un dispositivo puntual: en su próximo login desde ahí, ese dispositivo volverá a pedir TOTP.</summary>
    [Authorize]
    [HttpDelete("devices/{id}")]
    public async Task<ActionResult<Response<bool>>> RevokeDevice(long id)
    {
        var tokenData = TokenData.GetData(HttpContext);
        if (!tokenData.ok) return Unauthorized();

        return Ok(await _authenticationApplication.RevokeTrustedDevice(id, tokenData.UserId));
    }

    /// <summary>Olvida todos los dispositivos de confianza del usuario de una vez.</summary>
    [Authorize]
    [HttpDelete("devices")]
    public async Task<ActionResult<Response<bool>>> RevokeAllDevices()
    {
        var tokenData = TokenData.GetData(HttpContext);
        if (!tokenData.ok) return Unauthorized();

        await _authenticationApplication.RevokeAllTrustedDevicesForUser(tokenData.UserId);
        return Ok(new Response<bool> { ok = true, Data = true });
    }
}
