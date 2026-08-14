using Microsoft.Extensions.Configuration;

namespace Common.Utilities.MultiTenancy;

/// <summary>
/// Resuelve la cadena de conexión única de la aplicación.
/// </summary>
/// <remarks>
/// Antes había tres cadenas —<c>InventoryConnection</c>, <c>SeguridadConnection</c>
/// y <c>FacturacionConnection</c>— que apuntaban las tres a la misma base. Ahora
/// hay una sola: <c>DefaultConnection</c>.
/// <para>
/// No hay respaldo a las claves viejas a propósito. El valor por defecto en
/// appsettings.json es un placeholder, así que un respaldo silencioso haría que
/// la aplicación arrancara con una cadena inválida en lugar de avisar. Es
/// preferible fallar al arrancar, con un mensaje que diga qué falta.
/// </para>
/// </remarks>
public static class ConnectionStringResolver
{
    public const string Key = "DefaultConnection";

    /// <summary>Marcador de appsettings.json que el entorno debe reemplazar.</summary>
    private const string Placeholder = "REEMPLAZAR_VIA_ENV";

    public static string Resolve(IConfiguration configuration)
    {
        var cadena = configuration.GetConnectionString(Key);

        if (string.IsNullOrWhiteSpace(cadena))
            throw new InvalidOperationException(
                $"Falta la cadena de conexión '{Key}'. Configurala en appsettings.json o " +
                $"por variable de entorno ConnectionStrings__{Key}.");

        if (cadena.Contains(Placeholder, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"La cadena de conexión '{Key}' todavía tiene el marcador {Placeholder}. " +
                $"El entorno no la está sobrescribiendo: revisá que ConnectionStrings__{Key} " +
                "esté definida en el .env del despliegue.");

        return cadena;
    }
}
