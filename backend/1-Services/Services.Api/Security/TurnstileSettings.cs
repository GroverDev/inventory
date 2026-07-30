namespace Services.Api.Security;

/// <summary>
/// Cloudflare Turnstile: captcha del login web.
/// </summary>
public class TurnstileSettings
{
    /// <summary>
    /// Interruptor general. Apagado, no se le pide token a nadie y el login se
    /// comporta como antes de existir esta funcionalidad. Se lee con
    /// <c>IOptionsMonitor</c>, así que cambiarlo en appsettings.json tiene
    /// efecto sin reiniciar el servicio (siempre que el archivo esté montado y
    /// no horneado en la imagen).
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Clave secreta del widget. Se inyecta por variable de entorno
    /// (<c>Turnstile__SecretKey</c>), nunca se versiona en appsettings.
    /// </summary>
    public string SecretKey { get; set; } = "";

    /// <summary>
    /// A partir de cuántos intentos fallidos recientes de esa cuenta se exige
    /// resolver el captcha. La ventana es la de <c>LoginSettings.LockoutMinutes</c>.
    /// <para>
    /// Con 1 (por defecto), el login limpio nunca se verifica contra Cloudflare
    /// y por lo tanto no depende de que Cloudflare esté disponible; el captcha
    /// aparece recién cuando hay señal de abuso. En 0 se le exigiría a todos.
    /// </para>
    /// </summary>
    public int ChallengeAfterFailedAttempts { get; set; } = 1;

    /// <summary>
    /// Fallas de infraestructura consecutivas (timeout, DNS, 5xx) para dar por
    /// caído el servicio y abrir el circuito. Mientras el circuito está
    /// cerrado, no poder verificar bloquea el login: solo una caída confirmada
    /// deja pasar.
    /// </summary>
    public int OutageThreshold { get; set; } = 3;

    /// <summary>
    /// Minutos que el circuito permanece abierto antes de volver a probar. Al
    /// vencer, el siguiente login reintenta contra Cloudflare: si responde, el
    /// captcha se reactiva solo; si no, la ventana se renueva.
    /// </summary>
    public int OutageMinutes { get; set; } = 5;

    /// <summary>
    /// Orígenes que se consideran "nuestra web". Se completan en Program.cs con
    /// la lista de CORS para no mantener dos listados de lo mismo.
    /// </summary>
    public string[] WebOrigins { get; set; } = [];
}
