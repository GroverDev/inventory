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

    /// <summary>
    /// Días de vida del refresh token. En web sin "mantener sesión" dura solo
    /// <see cref="JwtSettings.WebSessionHours"/>; en el resto (móvil incluido)
    /// no cambia: <see cref="JwtSettings.RefreshTokenDays"/>.
    /// </summary>
    internal static double RefreshLifetimeDays(JwtSettings settings, InicioSesionDesde from, bool rememberMe) =>
        UsesRefreshCookie(from) && !rememberMe
            ? settings.WebSessionHours / 24.0
            : settings.RefreshTokenDays;

    /// <summary>
    /// Escribe la cookie del refresh token. Con <paramref name="persistent"/>
    /// falso no lleva Expires: es cookie de sesión y el navegador la descarta
    /// al cerrarse, que es lo que se busca en un equipo compartido.
    /// </summary>
    internal static void AppendRefreshCookie(
        HttpRequest request, HttpResponse response, string token, bool persistent, int days)
    {
        response.Cookies.Append(RefreshCookie, token, new CookieOptions
        {
            HttpOnly = true,
            // En desarrollo la API va por http; exigir Secure impediría que el
            // navegador guardara la cookie.
            Secure = request.IsHttps,
            // Web y API comparten dominio registrable (ideanueva.com), así que
            // Lax alcanza y no hace falta exponerla a contextos de terceros.
            SameSite = SameSiteMode.Lax,
            Path = "/api/Login",
            Expires = persistent ? DateTimeOffset.UtcNow.AddDays(days) : null
        });
    }

    /// <summary>
    /// Nombre del dispositivo. El móvil manda el suyo; la web no manda nada, así
    /// que se arma desde el User-Agent ("Chrome en Windows") para poder
    /// distinguir un equipo de otro en dispositivos de confianza y sesiones.
    /// </summary>
    internal static string ResolveDevice(HttpRequest request, InicioSesionDesde from, string? declared) =>
        UsesRefreshCookie(from) && string.IsNullOrWhiteSpace(declared)
            ? DeviceLabel.FromUserAgent(request.Headers.UserAgent.ToString())
            : declared ?? "";

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

    /// <summary>Misma cookie que escribe MfaController, con las mismas opciones, ya vencida.</summary>
    private void ClearDeviceTrustCookie()
    {
        Response.Cookies.Append(DeviceTrustCookie, "", new CookieOptions
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

        login.Device = ResolveDevice(Request, login.LoginFrom, login.Device);

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

                long? trustedDeviceId = await _authenticationApplication.FindTrustedDevice(resp.Data.UserId, deviceToken);

                if (trustedDeviceId != null)
                {
                    resp.Data.RequireTotp = false;

                    // Login() tampoco resolvió la sucursal: con TOTP no se
                    // revelan las sucursales antes del segundo factor.
                    var branch = await _authenticationApplication.AssignBranch(resp.Data);
                    if (!branch.ok)
                        return Ok(new Response<LoginResponse> { ok = false, Message = branch.Message });

                    // Login() no registró la sesión: se cortó temprano al ver
                    // RequireTotp, antes de la auditoría y el last_access que
                    // hace el camino normal. Sin esto el JWT queda con
                    // SessionId 0 y cualquier endpoint autenticado lo rechaza.
                    resp.Data.SesionId = await _authenticationApplication.RecordSuccessfulLogin(login, resp.Data.UserId);
                    await IssueTokens(resp.Data, login.LoginFrom, login.Device, login.RememberMe, trustedDeviceId);
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
                await IssueTokens(resp.Data, login.LoginFrom, login.Device, login.RememberMe);
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
        {
            request.RefreshToken = Request.Cookies[RefreshCookie] ?? "";

            // El canal manda, no lo que declare el cuerpo: quien llega con la
            // cookie es web y se rige por las reglas de vida de la web.
            if (!UsesRefreshCookie(request.LoginFrom))
                request.LoginFrom = InicioSesionDesde.ReconexionWeb;
        }

        request.Device = ResolveDevice(Request, request.LoginFrom, request.Device);

        ValidationResult result = new RefreshTokenRequestValidator().Validate(request);
        if (!result.IsValid) return ErrorsValidation<LoginResponse>.GetResponse(result.Errors);

        var resp = await _authenticationApplication.Refresh(
            request, _jwtSettings.RefreshTokenDays, _jwtSettings.WebSessionHours);

        if (resp.ok)
        {
            resp.Data!.Token = TokenJwt.GetToken(resp.Data, _jwtSettings.Secret, _jwtSettings.TimeTokenRefreshable);

            // Se responde por el mismo canal por el que llegó: si vino en la
            // cookie, el token rotado vuelve a la cookie y no al cuerpo.
            if (fromCookie)
            {
                AppendRefreshCookie(Request, Response, resp.Data.RefreshToken,
                    resp.Data.RememberSession, _jwtSettings.RefreshTokenDays);
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

    /// <summary>
    /// Cierre de sesión explícito: invalida el refresh token y, en web, olvida
    /// el equipo (revoca el dispositivo de confianza y borra su cookie). Así,
    /// tras cerrar sesión el siguiente login vuelve a pedir contraseña y TOTP.
    /// El móvil no manda esa cookie, por lo que su token de dispositivo no se toca.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("revoke")]
    public async Task<ActionResult<Response<bool>>> Revoke([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
            request.RefreshToken = Request.Cookies[RefreshCookie] ?? "";

        // Cierre automático (inactividad): no es una decisión del usuario, así
        // que el equipo conserva su confianza y su cookie.
        string deviceTrustToken = request.KeepDevice ? "" : Request.Cookies[DeviceTrustCookie] ?? "";

        ClearRefreshCookie();
        if (!request.KeepDevice) ClearDeviceTrustCookie();

        // Cerrar sesión sin token vigente no es un error: el objetivo (no
        // quedar con sesión abierta) igual se cumple. Si aún queda la cookie
        // del dispositivo se olvida igual.
        if (string.IsNullOrEmpty(request.RefreshToken) && string.IsNullOrEmpty(deviceTrustToken))
            return Ok(new Response<bool> { ok = true, Data = true });

        return Ok(await _authenticationApplication.RevokeRefreshToken(request.RefreshToken, deviceTrustToken, request.KeepDevice));
    }

    /// <summary>
    /// Pasa la sesión en curso a otra sucursal: devuelve un JWT nuevo con esa
    /// sucursal. El refresh token no cambia; su fila recuerda la sucursal nueva.
    /// </summary>
    [Authorize]
    [DisableRateLimiting]
    [HttpPost("switch-branch")]
    public async Task<ActionResult<Response<LoginResponse>>> SwitchBranch([FromBody] SwitchBranchRequest request)
    {
        var datos = TokenData.GetData(HttpContext);
        if (!datos.ok) return Unauthorized("Acceso no Autorizado.");

        var (resp, renewable) = await _authenticationApplication.SwitchBranch(datos, request.BranchId);

        if (resp.ok)
        {
            // Misma duración que el token actual: corto si la sesión se renueva
            // con refresh, largo si no tiene cómo (se quedaría sin sesión al
            // vencer).
            resp.Data!.Token = TokenJwt.GetToken(resp.Data, _jwtSettings.Secret,
                renewable ? _jwtSettings.TimeTokenRefreshable : _jwtSettings.TimeToken);
        }

        return Ok(resp);
    }

    private async Task IssueTokens(LoginResponse data, InicioSesionDesde from, string device, bool rememberMe, long? trustedDeviceId = null)
    {
        bool refreshable = UsesRefreshToken(from);

        data.Token = TokenJwt.GetToken(data, _jwtSettings.Secret,
            refreshable ? _jwtSettings.TimeTokenRefreshable : _jwtSettings.TimeToken);

        if (!refreshable) return;

        string raw = await _authenticationApplication.IssueRefreshToken(
            data.UserId, data.TenantId, data.BranchId, data.SesionId, device, Enum.GetName(typeof(InicioSesionDesde), from) ?? "",
            RefreshLifetimeDays(_jwtSettings, from, rememberMe),
            // El enlace con el dispositivo es solo de la web: el móvil no se toca.
            UsesRefreshCookie(from) ? trustedDeviceId : null);

        if (UsesRefreshCookie(from))
            AppendRefreshCookie(Request, Response, raw, rememberMe, _jwtSettings.RefreshTokenDays);
        else
            data.RefreshToken = raw;
    }
}
