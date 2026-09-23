using Dapper;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace MultiTenancy.Tests;

/// <summary>Ventas en espera: por sucursal, y retomables una sola vez.</summary>
[Collection("tenant-db")]
public class VentasEnEsperaTests(TenantDatabaseFixture db)
{
    private const int Uno = TenantDatabaseFixture.TenantUno;

    private HeldSaleRepository Espera(Guid sucursal) => new(db.ContextoApp(Uno, sucursal));

    private Guid NuevaSucursal(string nombre)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>(
            "INSERT INTO branches (tenant_id, name) VALUES (@t, @n) RETURNING id", new { t = Uno, n = nombre });
    }

    private int UnUsuario()
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<int>("SELECT id FROM sec.users WHERE tenant_id = @t AND is_active LIMIT 1", new { t = Uno });
    }

    private static HeldSaleRequest Carrito(string nota) => new()
    {
        Label = nota, ItemsCount = 2, Total = 35.5m,
        Payload = """{"cart":[{"ProductId":"x","Quantity":2}]}"""
    };

    [Fact]
    public async Task Una_venta_en_espera_se_ve_en_su_sucursal_y_no_en_otra()
    {
        var sur = NuevaSucursal("VE Sur");
        var otra = NuevaSucursal("VE Otra");
        var id = await Espera(sur).Hold(Carrito("señor de camisa azul"), UnUsuario());

        var enSur = (await Espera(sur).GetHeld()).Single(h => h.Id == id);
        Assert.Equal("señor de camisa azul", enSur.Label);
        Assert.Equal(35.5m, enSur.Total);

        Assert.DoesNotContain(await Espera(otra).GetHeld(), h => h.Id == id);
        Assert.Null(await Espera(otra).Take(id));
    }

    [Fact]
    public async Task Retomarla_la_saca_de_la_espera_y_solo_se_puede_una_vez()
    {
        var sucursal = NuevaSucursal("VE Retomar");
        var id = await Espera(sucursal).Hold(Carrito("mesa 3"), UnUsuario());

        var retomada = await Espera(sucursal).Take(id);
        Assert.NotNull(retomada);
        Assert.Contains("\"Quantity\": 2", retomada!.Payload);   // jsonb normaliza los espacios

        // Otra caja que intente retomarla después ya no la encuentra.
        Assert.Null(await Espera(sucursal).Take(id));
        Assert.DoesNotContain(await Espera(sucursal).GetHeld(), h => h.Id == id);
    }

    [Fact]
    public async Task Se_puede_descartar()
    {
        var sucursal = NuevaSucursal("VE Descartar");
        var id = await Espera(sucursal).Hold(Carrito(""), UnUsuario());

        Assert.True(await Espera(sucursal).Discard(id));
        Assert.False(await Espera(sucursal).Discard(id));
    }
}
