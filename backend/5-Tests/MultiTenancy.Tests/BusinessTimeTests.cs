using Common.Utilities;

namespace MultiTenancy.Tests;

/// <summary>
/// La traducción de "día del calendario boliviano" a ventana UTC.
/// </summary>
/// <remarks>
/// No necesita base de datos. Sostiene los filtros por fecha de Ventas, Turnos
/// de Caja, Dashboard y el reporte de mermas, así que los bordes importan: un
/// error de un segundo o de cuatro horas manda las ventas de la noche al día
/// equivocado, que es justamente el bug que este helper vino a cerrar.
/// </remarks>
public class BusinessTimeTests
{
    [Fact]
    public void La_zona_es_UTC_menos_4()
    {
        // Bolivia no tiene horario de verano: el offset es el mismo todo el año.
        Assert.Equal(TimeSpan.FromHours(-4), BusinessTime.Zone.GetUtcOffset(new DateTime(2026, 1, 15)));
        Assert.Equal(TimeSpan.FromHours(-4), BusinessTime.Zone.GetUtcOffset(new DateTime(2026, 7, 15)));
    }

    [Fact]
    public void El_dia_empieza_a_las_04_UTC()
    {
        var inicio = BusinessTime.StartOfDayUtc(new DateTime(2026, 8, 20));

        Assert.Equal(new DateTime(2026, 8, 20, 4, 0, 0), inicio);
        // Kind=Utc es lo que hace que Npgsql guarde el instante correcto.
        Assert.Equal(DateTimeKind.Utc, inicio.Kind);
    }

    [Fact]
    public void El_tope_es_el_inicio_del_dia_siguiente()
    {
        var fin = BusinessTime.EndOfDayUtcExclusive(new DateTime(2026, 8, 20));

        Assert.Equal(new DateTime(2026, 8, 21, 4, 0, 0), fin);
        Assert.Equal(DateTimeKind.Utc, fin.Kind);
    }

    [Fact]
    public void La_hora_del_dia_no_altera_el_rango()
    {
        // Los filtros llegan como 'yyyy-MM-dd', pero si alguien manda la fecha
        // con hora el rango debe seguir siendo el del día completo.
        var conHora = new DateTime(2026, 8, 20, 17, 43, 9);

        Assert.Equal(BusinessTime.StartOfDayUtc(new DateTime(2026, 8, 20)), BusinessTime.StartOfDayUtc(conHora));
        Assert.Equal(BusinessTime.EndOfDayUtcExclusive(new DateTime(2026, 8, 20)), BusinessTime.EndOfDayUtcExclusive(conHora));
    }

    [Fact]
    public void Una_venta_de_la_noche_cae_en_su_propio_dia()
    {
        // El caso real que motivó el helper: 23:32 del 20/08 en Bolivia se guarda
        // como 03:32 UTC del 21/08. Antes salía en el reporte del día siguiente.
        var venta = new DateTime(2026, 8, 21, 3, 32, 25, DateTimeKind.Utc);

        Assert.InRange(venta,
            BusinessTime.StartOfDayUtc(new DateTime(2026, 8, 20)),
            BusinessTime.EndOfDayUtcExclusive(new DateTime(2026, 8, 20)).AddTicks(-1));

        Assert.True(venta < BusinessTime.StartOfDayUtc(new DateTime(2026, 8, 21)));
    }

    [Fact]
    public void Los_dias_consecutivos_no_se_pisan_ni_dejan_huecos()
    {
        var dia = new DateTime(2026, 8, 20);

        // El tope exclusivo de un día es exactamente el inicio del siguiente:
        // ningún instante queda fuera de ambos ni dentro de los dos.
        Assert.Equal(BusinessTime.StartOfDayUtc(dia.AddDays(1)), BusinessTime.EndOfDayUtcExclusive(dia));
    }

    [Fact]
    public void Funciona_en_el_cambio_de_mes_y_de_anio()
    {
        Assert.Equal(new DateTime(2026, 9, 1, 4, 0, 0), BusinessTime.EndOfDayUtcExclusive(new DateTime(2026, 8, 31)));
        Assert.Equal(new DateTime(2027, 1, 1, 4, 0, 0), BusinessTime.EndOfDayUtcExclusive(new DateTime(2026, 12, 31)));
    }

    [Fact]
    public void Today_es_el_dia_boliviano_no_el_del_servidor()
    {
        var esperado = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BusinessTime.Zone).Date;

        Assert.Equal(esperado, BusinessTime.Today);
        // Entre las 20:00 y medianoche en Bolivia el día UTC ya avanzó: ahí es
        // donde Today y DateTime.UtcNow.Date discrepan, y Today es el correcto.
        Assert.Equal(default, BusinessTime.Today.TimeOfDay);
    }
}
