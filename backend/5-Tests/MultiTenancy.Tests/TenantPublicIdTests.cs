using Dapper;
using Inventory.Infrastructure;
using Npgsql;

namespace MultiTenancy.Tests;

/// <summary>
/// El public_id del tenant: nombre opaco de su carpeta de medios. Tiene que ser
/// único, inmutable (las URLs se cachean por un año) y llegar al almacenamiento.
/// </summary>
[Collection("tenant-db")]
public class TenantPublicIdTests(TenantDatabaseFixture db)
{
    private const int Uno = TenantDatabaseFixture.TenantUno;

    private Guid PublicIdDe(int tenantId)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>("SELECT public_id FROM sec.tenants WHERE id = @tenantId", new { tenantId });
    }

    [Fact]
    public void Cada_tenant_tiene_su_propio_public_id_incluido_el_que_crea_la_provision()
    {
        var a = PublicIdDe(Uno);
        var b = PublicIdDe(db.TenantDos);

        Assert.NotEqual(Guid.Empty, a);
        Assert.NotEqual(Guid.Empty, b);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void El_public_id_no_se_puede_cambiar_ni_a_mano()
    {
        using var admin = db.AbrirComoAdmin();

        var ex = Assert.Throws<PostgresException>(() =>
            admin.Execute("UPDATE sec.tenants SET public_id = gen_random_uuid() WHERE id = @Uno", new { Uno }));

        Assert.Contains("no puede cambiar", ex.MessageText);
    }

    [Fact]
    public void Otras_columnas_del_tenant_siguen_pudiendo_actualizarse()
    {
        using var admin = db.AbrirComoAdmin();
        var antes = PublicIdDe(Uno);

        admin.Execute("UPDATE sec.tenants SET modified = now() WHERE id = @Uno", new { Uno });

        Assert.Equal(antes, PublicIdDe(Uno));
    }

    [Fact]
    public void La_carpeta_de_medios_es_el_public_id_y_no_el_id_numerico()
    {
        var resolver = new TenantFolderResolver(db.ContextoApp(Uno, db.SucursalDe(Uno)));

        var carpeta = resolver.FolderOf(Uno);

        Assert.Equal(PublicIdDe(Uno).ToString("D"), carpeta);
        Assert.NotEqual(Uno.ToString(), carpeta);
    }
}
