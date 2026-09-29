namespace Common.Utilities;

/// <summary>
/// Nombre legible del navegador y sistema ("Chrome en Windows") a partir de la
/// cabecera <c>User-Agent</c>. La web no manda un nombre de dispositivo, así que
/// sin esto los dispositivos de confianza y las sesiones activas aparecen todos
/// como "sin nombre" y no hay forma de distinguirlos.
/// </summary>
/// <remarks>
/// Es una etiqueta para reconocer un equipo, no una identificación: dos PCs con
/// el mismo navegador y sistema dan el mismo texto. No es dato de seguridad.
/// </remarks>
public static class DeviceLabel
{
    /// <summary>Etiqueta para mostrar cuando no hay nada que reconocer.</summary>
    public const string Unknown = "Navegador web";

    public static string FromUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return Unknown;

        string browser = Browser(userAgent);
        string? os = OperatingSystem(userAgent);

        if (browser == "" && os == null) return Unknown;
        if (browser == "") return os!;
        return os == null ? browser : $"{browser} en {os}";
    }

    // El orden importa: casi todos los navegadores dicen ser Chrome y Safari.
    private static string Browser(string ua) =>
        ua.Contains("Edg/") || ua.Contains("EdgA/") || ua.Contains("EdgiOS/") ? "Edge"
        : ua.Contains("OPR/") || ua.Contains("Opera") ? "Opera"
        : ua.Contains("SamsungBrowser/") ? "Samsung Internet"
        : ua.Contains("Firefox/") || ua.Contains("FxiOS/") ? "Firefox"
        : ua.Contains("Chrome/") || ua.Contains("CriOS/") ? "Chrome"
        : ua.Contains("Safari/") ? "Safari"
        : "";

    // iPhone/iPad antes que macOS: su User-Agent también menciona "Mac OS X".
    // Android antes que Linux por la misma razón.
    private static string? OperatingSystem(string ua) =>
        ua.Contains("Windows") ? "Windows"
        : ua.Contains("iPhone") || ua.Contains("iPad") ? "iOS"
        : ua.Contains("Android") ? "Android"
        : ua.Contains("Mac OS X") || ua.Contains("Macintosh") ? "macOS"
        : ua.Contains("CrOS") ? "ChromeOS"
        : ua.Contains("Linux") ? "Linux"
        : null;
}
