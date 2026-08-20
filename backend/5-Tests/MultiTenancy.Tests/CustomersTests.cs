using Dapper;

namespace MultiTenancy.Tests;

/// <summary>
/// Cliente "Consumidor Final" (is_generic) que el POS precarga por defecto
/// para no bloquear una venta sin cliente identificado.
/// </summary>
[Collection("tenant-db")]
public class CustomersTests(TenantDatabaseFixture db)
{
    [Fact]
    public void Fn_seed_tenant_master_data_deja_un_cliente_generico_por_tenant()
    {
        using var admin = db.AbrirComoAdmin();

        foreach (var tenant in new[] { TenantDatabaseFixture.TenantUno, db.TenantDos })
        {
            var generico = admin.QuerySingle<(string FullName, string DocumentNumber)>(
                "SELECT full_name, document_number FROM customers WHERE tenant_id = @t AND is_generic",
                new { t = tenant });

            Assert.Equal("Consumidor Final", generico.FullName);
            Assert.Equal("0", generico.DocumentNumber);
        }
    }

    [Fact]
    public async Task No_se_puede_eliminar_el_cliente_generico()
    {
        var repo = new Inventory.Infrastructure.CustomersRepository(db.ContextoApp(TenantDatabaseFixture.TenantUno));

        using var admin = db.AbrirComoAdmin();
        var genericoId = admin.QuerySingle<Guid>(
            "SELECT id FROM customers WHERE tenant_id = @t AND is_generic",
            new { t = TenantDatabaseFixture.TenantUno });

        await Assert.ThrowsAsync<Common.Utilities.Exceptions.CustomException>(
            () => repo.DeleteCustomer(genericoId, idUserModified: 1));

        // Sigue activo: el intento de borrado no lo tocó.
        var sigueActivo = admin.ExecuteScalar<bool>(
            "SELECT state FROM customers WHERE id = @id", new { id = genericoId });
        Assert.True(sigueActivo);
    }

    [Fact]
    public async Task GetDefaultCustomer_devuelve_el_generico_del_tenant_activo()
    {
        var repo = new Inventory.Infrastructure.CustomersRepository(db.ContextoApp(TenantDatabaseFixture.TenantUno));

        var generico = await repo.GetDefaultCustomer();

        Assert.Equal("Consumidor Final", generico.FullName);
        Assert.True(generico.IsGeneric);
    }
}
