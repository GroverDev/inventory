namespace Common.Utilities;

/// <summary>
/// Traduce "un día del calendario" —lo que elige el usuario en un filtro— al
/// rango de instantes UTC que le corresponde.
/// </summary>
/// <remarks>
/// Las marcas de tiempo se guardan en UTC (ver <see cref="AuditHelper"/>), pero
/// el usuario razona en hora de Bolivia. Comparar sin convertir hace que el día
/// consultado empiece y termine cuatro horas antes de lo que la gente espera:
/// una venta de las 23:32 aparecía recién en el reporte del día siguiente.
/// <para>
/// Aplica solo a columnas que guardan un INSTANTE (<c>sale_date</c>,
/// <c>opened_at</c>). Las que guardan un DÍA como medianoche UTC —
/// <c>purchase_date</c>, <c>estimated_delivery_date</c>— no deben pasar por
/// acá: convertirlas correría la ventana y dejaría fuera justamente las filas
/// de ese día.
/// </para>
/// </remarks>
public static class BusinessTime
{
    private const string IanaId = "America/La_Paz";
    private const string WindowsId = "SA Western Standard Time";

    /// <summary>Zona horaria del negocio.</summary>
    public static TimeZoneInfo Zone { get; } = ResolverZona();

    private static TimeZoneInfo ResolverZona()
    {
        // El id cambia según el sistema: IANA en Linux (el contenedor), Windows
        // en las máquinas de desarrollo. .NET traduce entre ambos cuando hay ICU,
        // pero no en todos los despliegues, así que se prueban los dos.
        foreach (var id in new[] { IanaId, WindowsId })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        // Último recurso: Bolivia no tiene horario de verano, así que un offset
        // fijo es equivalente a la zona real.
        return TimeZoneInfo.CreateCustomTimeZone("Bolivia", TimeSpan.FromHours(-4), "Bolivia", "Bolivia");
    }

    /// <summary>Hoy según el calendario del negocio, no según la zona del servidor.</summary>
    public static DateTime Today => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone).Date;

    /// <summary>Instante UTC en que empieza <paramref name="dia"/>.</summary>
    public static DateTime StartOfDayUtc(DateTime dia) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dia.Date, DateTimeKind.Unspecified), Zone);

    /// <summary>
    /// Instante UTC en que empieza el día siguiente a <paramref name="dia"/>.
    /// Es un límite superior EXCLUSIVO: se usa con <c>&lt;</c>, no con
    /// <c>&lt;=</c>, para no perder lo ocurrido en el último segundo del día.
    /// </summary>
    public static DateTime EndOfDayUtcExclusive(DateTime dia) => StartOfDayUtc(dia.AddDays(1));
}
