using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Services.Api.Security;

/// <summary>Resultado de verificar un token de Turnstile.</summary>
public enum TurnstileResult
{
    /// <summary>Cloudflare confirmó el token.</summary>
    Valid,

    /// <summary>
    /// Rechazo explícito: token ausente, inválido, vencido o ya usado. Siempre
    /// bloquea, sin importar la configuración: es información sobre la
    /// petición, no una falla nuestra.
    /// </summary>
    Rejected,

    /// <summary>
    /// No se pudo verificar y el circuito está abierto (caída confirmada de
    /// Cloudflare). Se deja pasar y queda registrado en el log.
    /// </summary>
    Unavailable
}

public interface ITurnstileValidator
{
    /// <summary>
    /// ¿Esta petición está alcanzada por el captcha? Verdadero solo si la
    /// función está activa y el <c>Origin</c> es el de nuestra web.
    /// </summary>
    bool AppliesTo(string? origin);

    /// <summary>
    /// Con esos intentos fallidos recientes, ¿corresponde exigir el desafío?
    /// </summary>
    bool RequiresChallenge(int recentFailedAttempts);

    /// <summary>Verifica el token contra Cloudflare.</summary>
    Task<TurnstileResult> VerifyAsync(string token, string? remoteIp, CancellationToken ct = default);
}

/// <summary>
/// Verifica los tokens de Turnstile contra el endpoint <c>siteverify</c>.
/// </summary>
public class TurnstileValidator(
    HttpClient http,
    IOptionsMonitor<TurnstileSettings> options,
    TurnstileCircuitBreaker breaker,
    ILogger<TurnstileValidator> logger) : ITurnstileValidator
{
    private const string VerifyUrl =
        "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    /// <summary>
    /// El captcha alcanza a los navegadores parados en nuestra propia web,
    /// identificados por su cabecera <c>Origin</c>.
    /// <para>
    /// Se usa el Origin y no el <c>LoginFrom</c> del cuerpo porque el Origin lo
    /// pone el navegador y el JavaScript de la página no puede modificarlo: así
    /// la web no puede saltearse el desafío cambiando el payload, ni un sitio
    /// ajeno puede hacerse pasar por ella.
    /// </para>
    /// <para>
    /// El móvil no manda Origin, así que nunca queda alcanzado — Turnstile es
    /// un widget de navegador y no existe para clientes nativos.
    /// </para>
    /// </summary>
    public bool AppliesTo(string? origin)
    {
        var settings = options.CurrentValue;

        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.SecretKey))
            return false;

        if (string.IsNullOrWhiteSpace(origin)) return false;

        return settings.WebOrigins.Any(o =>
            string.Equals(o, origin, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// El desafío se exige solo ante señal de abuso en esa cuenta. El login
    /// limpio no se verifica, y por eso una caída de Cloudflare no puede dejar
    /// a nadie afuera en el camino normal.
    /// </summary>
    public bool RequiresChallenge(int recentFailedAttempts) =>
        recentFailedAttempts >= options.CurrentValue.ChallengeAfterFailedAttempts;

    public async Task<TurnstileResult> VerifyAsync(string token, string? remoteIp, CancellationToken ct = default)
    {
        // Caída ya confirmada: no se insiste contra un servicio caído, así el
        // login tampoco arrastra el timeout de cada intento.
        if (breaker.IsOpen) return TurnstileResult.Unavailable;

        // Sin token no hay nada que consultar, y es un rechazo legítimo: el
        // navegador debía haberlo enviado.
        if (string.IsNullOrWhiteSpace(token)) return TurnstileResult.Rejected;

        try
        {
            var form = new Dictionary<string, string>
            {
                ["secret"] = options.CurrentValue.SecretKey,
                ["response"] = token,
            };
            // Ata el token a la IP que lo resolvió. Llega correcta gracias a
            // ForwardedHeaders cuando la API corre detrás de nginx.
            if (!string.IsNullOrWhiteSpace(remoteIp)) form["remoteip"] = remoteIp!;

            using var content = new FormUrlEncodedContent(form);
            using var response = await http.PostAsync(VerifyUrl, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Turnstile: siteverify respondió {Status}.", (int)response.StatusCode);
                return OnInfrastructureFailure();
            }

            var result = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>(ct);
            if (result is null)
            {
                logger.LogWarning("Turnstile: respuesta ilegible de siteverify.");
                return OnInfrastructureFailure();
            }

            breaker.RecordSuccess();

            if (result.Success) return TurnstileResult.Valid;

            logger.LogInformation(
                "Turnstile rechazó el token. Códigos: {Codes}",
                string.Join(", ", result.ErrorCodes ?? []));

            return TurnstileResult.Rejected;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Turnstile: no se pudo contactar a siteverify.");
            return OnInfrastructureFailure();
        }
    }

    /// <summary>
    /// Una falla aislada bloquea igual; solo una caída confirmada deja pasar.
    /// </summary>
    private TurnstileResult OnInfrastructureFailure() =>
        breaker.RecordFailure() ? TurnstileResult.Unavailable : TurnstileResult.Rejected;

    private sealed class SiteVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
