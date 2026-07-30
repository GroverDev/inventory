using Microsoft.Extensions.Options;

namespace Services.Api.Security;

/// <summary>
/// Corta la dependencia con Cloudflare cuando se confirma que no responde.
///
/// <para>
/// Una falla suelta (un timeout, un 5xx) no habilita nada: el login se bloquea
/// igual, porque podría ser ruido. Recién cuando se acumulan
/// <see cref="TurnstileSettings.OutageThreshold"/> fallas seguidas se da la
/// caída por confirmada, el circuito se abre y durante
/// <see cref="TurnstileSettings.OutageMinutes"/> los logins pasan sin captcha.
/// </para>
///
/// <para>
/// Mientras está abierto ni siquiera se llama a Cloudflare, así que el login no
/// arrastra el timeout de cada petición. Al vencer la ventana, el siguiente
/// intento vuelve a probar (medio abierto): si Cloudflare responde, el captcha
/// se reactiva solo, sin que nadie toque una configuración.
/// </para>
///
/// Singleton: el estado tiene que ser compartido por todas las peticiones.
/// </summary>
public class TurnstileCircuitBreaker(
    IOptionsMonitor<TurnstileSettings> options,
    ILogger<TurnstileCircuitBreaker> logger)
{
    private readonly Lock _gate = new();
    private int _consecutiveFailures;
    private DateTimeOffset _openUntil = DateTimeOffset.MinValue;

    /// <summary>¿Está abierto ahora mismo? (no modifica el estado)</summary>
    public bool IsOpen
    {
        get
        {
            lock (_gate) return DateTimeOffset.UtcNow < _openUntil;
        }
    }

    /// <summary>
    /// Registra una verificación exitosa: cierra el circuito y reinicia la
    /// cuenta de fallas.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_gate)
        {
            if (_consecutiveFailures == 0 && _openUntil == DateTimeOffset.MinValue) return;

            if (_openUntil > DateTimeOffset.MinValue)
                logger.LogWarning("Turnstile: el servicio volvió a responder, se restablece la verificación.");

            _consecutiveFailures = 0;
            _openUntil = DateTimeOffset.MinValue;
        }
    }

    /// <summary>
    /// Registra una falla de infraestructura. Devuelve <c>true</c> si el
    /// circuito quedó abierto, es decir si este login debe dejarse pasar.
    /// </summary>
    public bool RecordFailure()
    {
        var settings = options.CurrentValue;

        lock (_gate)
        {
            if (DateTimeOffset.UtcNow < _openUntil) return true;

            _consecutiveFailures++;
            if (_consecutiveFailures < Math.Max(1, settings.OutageThreshold)) return false;

            _openUntil = DateTimeOffset.UtcNow.AddMinutes(Math.Max(1, settings.OutageMinutes));
            logger.LogError(
                "Turnstile: {Failures} fallas consecutivas al verificar. Se da el servicio por caído y " +
                "los logins pasarán sin captcha hasta {OpenUntil:u}.",
                _consecutiveFailures, _openUntil);

            return true;
        }
    }
}
