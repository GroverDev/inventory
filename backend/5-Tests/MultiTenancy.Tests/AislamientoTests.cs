using Dapper;
using Npgsql;

namespace MultiTenancy.Tests;

/// <summary>
/// El aislamiento entre farmacias, verificado sobre datos reales.
/// </summary>
/// <remarks>
/// Ninguna de estas consultas lleva <c>WHERE tenant_id</c>. Ese es justamente el
/// punto: el filtrado lo aplica PostgreSQL con Row-Level Security, no la consulta.
/// Si estas pruebas empiezan a fallar es porque el aislamiento dejó de existir,
/// que es un fallo silencioso en producción.
/// </remarks>
[Collection("tenant-db")]
public class AislamientoTests(TenantDatabaseFixture db)
{
    [Fact]
    public void Una_farmacia_no_ve_los_productos_de_otra()
    {
        Guid producto = SembrarProductoEn(db.TenantDos, "EXCLUSIVO DE LA OTRA FARMACIA");

        using var cn = db.AbrirComoApp(TenantDatabaseFixture.TenantUno);
        var visto = cn.QueryFirstOrDefault<Guid?>(
            "SELECT id FROM products WHERE id = @id", new { id = producto });

        Assert.Null(visto);
    }

    [Fact]
    public void Cada_farmacia_ve_sus_propios_productos()
    {
        Guid producto = SembrarProductoEn(db.TenantDos, "PROPIO DE LA FARMACIA DOS");

        using var cn = db.AbrirComoApp(db.TenantDos);
        var visto = cn.QueryFirstOrDefault<Guid?>(
            "SELECT id FROM products WHERE id = @id", new { id = producto });

        Assert.Equal(producto, visto);
    }

    [Fact]
    public void Sin_tenant_fijado_no_se_ve_nada()
    {
        // Falla cerrado. Una conexión sin tenant no debe ver TODO por omisión,
        // que es el error clásico al escribir políticas permisivas.
        using var cn = db.AbrirComoApp();

        Assert.Equal(0, cn.ExecuteScalar<int>("SELECT count(*) FROM products"));
        Assert.Equal(0, cn.ExecuteScalar<int>("SELECT count(*) FROM sales"));
        Assert.Equal(0, cn.ExecuteScalar<int>("SELECT count(*) FROM sec.users"));
    }

    [Fact]
    public void No_se_puede_insertar_marcando_otra_farmacia()
    {
        using var cn = db.AbrirComoApp(db.TenantDos);

        var ex = Assert.Throws<PostgresException>(() => cn.Execute(@"
            INSERT INTO categories
                (id, category_name, description, is_active, state,
                 created_by, created, modified_by, modified, tenant_id)
            VALUES (gen_random_uuid(), 'INVASORA', '', true, true, 1, now(), 1, now(), @ajeno)",
            new { ajeno = TenantDatabaseFixture.TenantUno }));

        // 42501 = insufficient_privilege, que es como PostgreSQL reporta una
        // violación de WITH CHECK.
        Assert.Equal("42501", ex.SqlState);
    }

    [Fact]
    public void El_insert_toma_el_tenant_de_la_sesion()
    {
        // Es lo que permite que las 219 consultas del backend sigan sin tocar:
        // ningún INSERT manda tenant_id, lo pone el DEFAULT.
        using var cn = db.AbrirComoApp(db.TenantDos);

        var id = cn.ExecuteScalar<Guid>(@"
            INSERT INTO categories
                (id, category_name, description, is_active, state,
                 created_by, created, modified_by, modified)
            VALUES (gen_random_uuid(), 'SIN TENANT EXPLICITO', '', true, true, 1, now(), 1, now())
            RETURNING id");

        using var admin = db.AbrirComoAdmin();
        var tenant = admin.ExecuteScalar<int>(
            "SELECT tenant_id FROM categories WHERE id = @id", new { id });

        Assert.Equal(db.TenantDos, tenant);
    }

    [Fact]
    public void No_se_pueden_modificar_filas_de_otra_farmacia()
    {
        Guid producto = SembrarProductoEn(db.TenantDos, "INTOCABLE");

        using var cn = db.AbrirComoApp(TenantDatabaseFixture.TenantUno);
        int afectadas = cn.Execute(
            "UPDATE products SET product_name = 'SECUESTRADO' WHERE id = @id", new { id = producto });

        Assert.Equal(0, afectadas);

        using var admin = db.AbrirComoAdmin();
        Assert.Equal("INTOCABLE", admin.ExecuteScalar<string>(
            "SELECT product_name FROM products WHERE id = @id", new { id = producto }));
    }

    [Fact]
    public void No_se_pueden_borrar_filas_de_otra_farmacia()
    {
        Guid producto = SembrarProductoEn(db.TenantDos, "NO BORRABLE");

        using var cn = db.AbrirComoApp(TenantDatabaseFixture.TenantUno);
        Assert.Equal(0, cn.Execute("DELETE FROM products WHERE id = @id", new { id = producto }));

        using var admin = db.AbrirComoAdmin();
        Assert.Equal(1, admin.ExecuteScalar<int>(
            "SELECT count(*) FROM products WHERE id = @id", new { id = producto }));
    }

    [Fact]
    public void Una_clave_foranea_no_puede_cruzar_farmacias()
    {
        // Sin FK compuesta esto tendría éxito: la integridad referencial corre por
        // debajo de RLS. El producto quedaría invisible para su propio dueño,
        // porque el JOIN al laboratorio no devolvería nada.
        using var admin = db.AbrirComoAdmin();

        var labAjeno = admin.ExecuteScalar<Guid>(
            "SELECT id FROM laboratories WHERE tenant_id = @t LIMIT 1", new { t = db.TenantDos });
        var uomPropia = admin.ExecuteScalar<Guid>(
            "SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1",
            new { t = TenantDatabaseFixture.TenantUno });

        var ex = Assert.Throws<PostgresException>(() => admin.Execute(@"
            INSERT INTO products
                (id, product_name, description, sale_price, available_in_pos,
                 laboratory_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id)
            VALUES (gen_random_uuid(), 'CRUZADO', '', 1, true, @lab, @uom, true, true,
                    1, now(), 1, now(), @tenant)",
            new { lab = labAjeno, uom = uomPropia, tenant = TenantDatabaseFixture.TenantUno }));

        Assert.Equal("23503", ex.SqlState);   // foreign_key_violation
    }

    /// <summary>Crea un producto en la farmacia indicada, saltando RLS.</summary>
    private Guid SembrarProductoEn(int tenantId, string nombre)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>(@"
            INSERT INTO products
                (id, product_name, description, sale_price, available_in_pos,
                 laboratory_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id)
            SELECT gen_random_uuid(), @nombre, '', 1, true,
                   (SELECT id FROM laboratories        WHERE tenant_id = @t LIMIT 1),
                   (SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1),
                   true, true, 1, now(), 1, now(), @t
            RETURNING id",
            new { nombre, t = tenantId });
    }
}
