using Common.Utilities;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Seguridad.Application;
using Seguridad.Domain;
using Seguridad.Domain.Enums;
using Services.Api.jwt;
using Services.Api.Security;
using Services.Api.Utils;

namespace Services.Api.Controllers.Security;

[ApiExplorerSettings(GroupName = "SECURITY")]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Login)]
[ApiController]
public class LoginController(
    IAuthenticationApplication _authenticationApplication,
    IOptions<JwtSettings> jwtSettings,
    ITurnstileValidator _turnstile) : ControllerBase
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    /// <summary>
    /// Nombre de la cookie que transporta el refresh token en web. MfaController
    /// también la escribe: el login puede completarse tanto acá (sin 2FA) como
    /// ahí (verificando TOTP), y ambos caminos deben entregarla igual.
    /// </summary>
    internal const string RefreshCookie = "refresh_token";

    /// <summary>
    /// Nombre de la cookie que transporta el token de dispositivo de confianza
    /// en web. MfaController la escribe al verificar el TOTP con "recordar este
    /// dispositivo"; este controlador la lee en el siguiente login.
    /// </summary>
    internal const string DeviceTrustCookie = "device_trust";

    /// <summary>
    /// Clientes que sostienen la sesión con refresh token. Reciben un access
    /// token corto. Solo queda fuera Postman, para no romper pruebas manuales.
    /// </summary>
    internal static bool UsesRefreshToken(InicioSesionDesde from) =>
        from is not InicioSesionDesde.Postman;

    /// <summary>
    /// En web el refresh token viaja en una cookie HttpOnly, nunca en el
    /// cuerpo: así JavaScript no puede leerlo y un XSS no puede robarlo.
    /// El móvil no tiene navegador, así que lo recibe en el JSON.
    /// </summary>
    internal static bool UsesRefreshCookie(InicioSesionDesde from) =>
        from is InicioSesionDesde.Web or InicioSesionDesde.ReconexionWeb;

    private void SetRefreshCookie(string token)
    {
        Response.Cookies.Append(RefreshCookie, token, new CookieOptions
        {
            HttpOnly = true,
            // En desarrollo la API va por http; exigir Secure impediría que el
            // navegador guardara la cookie.
            Secure = Request.IsHttps,
            // Web y API comparten dominio registrable (ideanueva.com), así que
            // Lax alcanza y no hace falta exponerla a contextos de terceros.
            SameSite = SameSiteMode.Lax,
            Path = "/api/Login",
            Expires = DateTimeOffset.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
        });
    }

    private void ClearRefreshCookie()
    {
        Response.Cookies.Append(RefreshCookie, "", new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/api/Login",
            Expires = DateTimeOffset.UnixEpoch
        });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<Response<LoginResponse>>> Authenticate([FromBody] LoginRequest login)
    {
        // Captcha. Dos filtros, en este orden:
        //
        //  1. Alcance: se decide por la cabecera Origin, que pone el navegador y
        //     la página no puede alterar. Así la web no puede saltearlo
        //     cambiando el cuerpo del request, y el móvil (que no manda Origin)
        //     queda fuera sin depender de lo que declare.
        //  2. Sospecha: solo se verifica si la cuenta acumula intentos fallidos
        //     recientes. El login limpio no consulta a Cloudflare, así que una
        //     caída del servicio no puede dejar a nadie afuera en el camino
        //     normal; y ante fuerza bruta la verificación es estricta.
        if (_turnstile.AppliesTo(Request.Headers.Origin.ToString()))
        {
            int failedAttempts = await _authenticationApplication.RecentFailedAttempts(login.Email);

            if (_turnstile.RequiresChallenge(failedAttempts))
            {
                string? remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var verdict = await _turnstile.VerifyAsync(login.TurnstileToken, remoteIp);

                if (verdict == TurnstileResult.Rejected)
                {
                    return Ok(new Response<LoginResponse>
                    {
                        ok = false,
                        Message = new Msg
                        {
                            MessageType = "warning",
                            Description = "No pudimos completar la validación de seguridad. Vuelve a intentarlo."
                        }
                    });
                }
            }
        }

        ValidationResult result = new LoginRequestValidator().Validate(login);
        if (!result.IsValid) return ErrorsValidation<LoginResponse>.GetResponse(result.Errors);

        var resp = await _authenticationApplication.Login(login);
        if (resp.ok)
        {
            if (resp.Data!.RequireTotp)
            {
                // Dispositivo ya verificado en un login anterior ("recordar
                // este dispositivo" al validar el TOTP): se salta el segundo
                // factor, igual que si estuviera deshabilitado. Cualquier duda
                // sobre el token (ausente, vencido, revocado, de otro usuario)
                // cae al flujo TOTP normal: fail-closed.
                string deviceToken = UsesRefreshCookie(login.LoginFrom)
                    ? (Request.Cookies[DeviceTrustCookie] ?? "")
                    : login.DeviceTrustToken;

                bool trustedDevice = !string.IsNullOrEmpty(deviceToken)
                    && await _authenticationApplication.IsTrustedDevice(resp.Data.UserId, deviceToken);

                if (trustedDevice)
                {
                    resp.Data.RequireTotp = false;
                    // Login() no registró la sesión: se cortó temprano al ver
                    // RequireTotp, antes de la auditoría y el last_access que
                    // hace el camino normal. Sin esto el JWT queda con
                    // SessionId 0 y cualquier endpoint autenticado lo rechaza.
                    resp.Data.SesionId = await _authenticationApplication.RecordSuccessfulLogin(login, resp.Data.UserId);
                    await IssueTokens(resp.Data, login.LoginFrom, login.Device);
                }
                else
                {
                    resp.Data.TotpSessionToken = TokenJwt.GetTotpPendingToken(resp.Data.UserId, _jwtSettings.Secret);
                    resp.Data.UserId = 0;
                }
            }
            else if (resp.Data.TotpSetupRequired)
            {
                // Aún debe configurar el 2FA: se mantiene el token largo de
                // siempre para que alcance a completarlo, y no se entrega
                // refresh token hasta que la cuenta quede protegida.
                resp.Data.Token = TokenJwt.GetToken(resp.Data, _jwtSettings.Secret, _jwtSettings.TimeToken);
            }
            else
            {
                await IssueTokens(resp.Data, login.LoginFrom, login.Device);
            }
        }

        return Ok(resp);
    }

    /// <summary>
    /// Canjea un refresh token por un access token nuevo. Anónimo a propósito:
    /// se invoca justamente cuando el access token ya venció.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<Response<LoginResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        // La web no puede leer su cookie desde JavaScript, así que manda el
        // cuerpo vacío y el token se toma de la cookie que envía el navegador.
        bool fromCookie = string.IsNullOrEmpty(request.RefreshToken);
        if (fromCookie)
            request.RefreshToken = Request.Cookies[RefreshCookie] ?? "";

        ValidationResult result = new RefreshTokenRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<LoginResponse>.GetResponse(result.Errors);

        var resp = await _authenticationApplication.Refresh(request, _jwtSettings.RefreshTokenDays);

        if (resp.ok)
        {
            resp.Data!.Token = TokenJwt.GetToken(resp.Data, _jwtSettings.Secret, _jwtSettings.TimeTokenRefreshable);

            // Se responde por el mismo canal por el que llegó: si vino en la
            // cookie, el token rotado vuelve a la cookie y no al cuerpo.
            if (fromCookie)
            {
                SetRefreshCookie(resp.Data.RefreshToken);
                resp.Data.RefreshToken = "";
            }
        }
        else if (fromCookie)
        {
            // Sesión no recuperable: se limpia la cookie para no reintentar
            // con un token que ya no sirve.
            ClearRefreshCookie();
        }

        return Ok(resp);
    }

    /// <summary>Cierre de sesión explícito: invalida el refresh token.</summary>
    [AllowAnonymous]
    [HttpPost("revoke")]
    public async Task<ActionResult<Response<bool>>> Revoke([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            request.RefreshToken = Request.Cookies[RefreshCookie] ?? "";

        ClearRefreshCookie();

        // Cerrar sesión sin token vigente no es un error: el objetivo (no
        // quedar con sesión abierta) igual se cumple.
        if (string.IsNullOrEmpty(request.RefreshToken))
            return Ok(new Response<bool> { ok = true, Data = true });

        return Ok(await _authenticationApplication.RevokeRefreshToken(request.RefreshToken));
    }

    private async Task IssueTokens(LoginResponse data, InicioSesionDesde from, string device)
    {
        bool refreshable = UsesRefreshToken(from);

        data.Token = TokenJwt.GetToken(data, _jwtSettings.Secret,
            refreshable ? _jwtSettings.TimeTokenRefreshable : _jwtSettings.TimeToken);

        if (!refreshable) return;

        string raw = await _authenticationApplication.IssueRefreshToken(
            data.UserId, data.TenantId, data.SesionId, device, Enum.GetName(typeof(InicioSesionDesde), from) ?? "",
            _jwtSettings.RefreshTokenDays);

        if (UsesRefreshCookie(from))
            SetRefreshCookie(raw);
        else
            data.RefreshToken = raw;
    }
}
