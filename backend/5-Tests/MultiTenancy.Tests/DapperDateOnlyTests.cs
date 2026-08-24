using Dapper;

namespace MultiTenancy.Tests;

/// <summary>
/// DateOnly atravesando Dapper, de ida y de vuelta.
/// </summary>
/// <remarks>
/// Dapper 2.1.35 no maneja <see cref="DateOnly"/> por su cuenta: al recibirlo
/// como parámetro lanza "cannot be used as a parameter value". Las fechas de
/// compra son días del calendario y viven en columnas `date`, así que en C# son
/// DateOnly; sin el handler que registra DapperConfig, listar o guardar un
/// pedido revienta con el error genérico de la API.
///
/// Pasó desapercibido porque PurchaseDateMappingTests cubre el mapeo de Mapster
/// (string → DateOnly) pero no el paso por Dapper, que es donde fallaba.
/// </remarks>
[Collection("tenant-db")]
public class DapperDateOnlyTests(TenantDatabaseFixture db)
{
    [Fact]
    public void Un_DateOnly_sirve_como_parametro()
    {
        using var conn = db.AbrirComoAdmin();

        var dia = new DateOnly(2026, 8, 21);
        var leido = conn.QuerySingle<DateOnly>("SELECT @Dia::date", new { Dia = dia });

        Assert.Equal(dia, leido);
    }

    [Fact]
    public void Un_DateOnly_anulable_tambien_sirve()
    {
        using var conn = db.AbrirComoAdmin();

        DateOnly? dia = new DateOnly(2026, 12, 31);
        var leido = conn.QuerySingle<DateOnly?>("SELECT @Dia::date", new { Dia = dia });

        Assert.Equal(dia, leido);
    }

    [Fact]
    public void El_rango_de_un_filtro_por_dia_no_se_corre()
    {
        using var conn = db.AbrirComoAdmin();

        // Es la forma del filtro de pedidos: BETWEEN sobre columnas date. El
        // día del borde tiene que entrar, sin corrimientos por zona horaria.
        var desde = new DateOnly(2026, 8, 1);
        var hasta = new DateOnly(2026, 8, 31);

        var dentro = conn.QuerySingle<bool>(
            "SELECT date '2026-08-31' BETWEEN @Desde AND @Hasta",
            new { Desde = desde, Hasta = hasta });

        Assert.True(dentro);
    }
}
