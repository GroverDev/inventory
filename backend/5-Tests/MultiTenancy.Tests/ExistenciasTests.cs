using Dapper;

namespace MultiTenancy.Tests;

/// <summary>
/// Invariantes del libro mayor de existencias.
/// </summary>
/// <remarks>
/// <c>stock_items</c> es la fuente de verdad del stock; <c>products.current_stock</c>
/// quedó como caché porque lo leen unos veinte lugares del backend. Mientras
/// convivan, lo que hay que vigilar es que no se separen: un camino de escritura
/// que toque la caché sin pasar por el libro mayor no rompe nada visible, pero
/// deja el inventario mintiendo.
/// </remarks>
[Collection("tenant-db")]
public class ExistenciasTests(TenantDatabaseFixture db)
{
    [Fact]
    public void La_cache_de_stock_coincide_con_el_libro_mayor()
    {
        using var cn = db.AbrirComoAdmin();

        var descuadres = cn.Query<(int Tenant, Guid Producto, string Nombre, decimal Cache, decimal Real)>(
            "SELECT tenant_id, product_id, product_name, cache, real FROM v_stock_descuadrado").ToList();

        Assert.True(descuadres.Count == 0,
            "products.current_stock no coincide con la suma de stock_items. Algo escribió " +
            "el stock sin pasar por el libro mayor:\n  " +
            string.Join("\n  ", descuadres.Select(d => $"{d.Nombre}: caché={d.Cache}, real={d.Real}")));
    }

    [Fact]
    public void Todo_producto_tiene_al_menos_una_existencia()
    {
        // Con tracking_mode = 'none' es exactamente una, la implícita. Un producto
        // sin ninguna no se puede vender ni ajustar.
        using var cn = db.AbrirComoAdmin();

        var huerfanos = cn.ExecuteScalar<int>(@"
            SELECT count(*) FROM products p
             WHERE NOT EXISTS (SELECT 1 FROM stock_items si
                                WHERE si.product_id = p.id AND si.tenant_id = p.tenant_id)");

        Assert.Equal(0, huerfanos);
    }

    [Fact]
    public void Los_productos_sin_seguimiento_tienen_una_sola_existencia()
    {
        using var cn = db.AbrirComoAdmin();

        var duplicados = cn.ExecuteScalar<int>(@"
            SELECT count(*) FROM (
                SELECT si.product_id
                  FROM stock_items si
                  JOIN products p ON p.id = si.product_id
                 WHERE p.tracking_mode = 'none'
                 GROUP BY si.product_id
                HAVING count(*) > 1) x");

        Assert.Equal(0, duplicados);
    }

    [Fact]
    public void No_se_puede_repetir_lote_para_un_mismo_producto()
    {
        using var cn = db.AbrirComoAdmin();
        var producto = cn.ExecuteScalar<Guid>(
            "SELECT id FROM products WHERE tenant_id = @t LIMIT 1",
            new { t = TenantDatabaseFixture.TenantUno });

        cn.Execute(@"
            INSERT INTO stock_items (tenant_id, product_id, lot_code, expiry_date, quantity)
            VALUES (@t, @p, 'LOTE-A', '2027-01-01', 10)",
            new { t = TenantDatabaseFixture.TenantUno, p = producto });

        var ex = Assert.Throws<Npgsql.PostgresException>(() => cn.Execute(@"
            INSERT INTO stock_items (tenant_id, product_id, lot_code, expiry_date, quantity)
            VALUES (@t, @p, 'LOTE-A', '2027-01-01', 5)",
            new { t = TenantDatabaseFixture.TenantUno, p = producto }));

        Assert.Equal("23505", ex.SqlState);   // unique_violation

        cn.Execute("DELETE FROM stock_items WHERE lot_code = 'LOTE-A'");
    }

    [Fact]
    public void La_existencia_sin_lote_es_unica_por_producto()
    {
        // Depende de NULLS NOT DISTINCT en el índice. Sin eso PostgreSQL trata
        // cada NULL como distinto y admitiría dos existencias implícitas para el
        // mismo producto, que es como el stock empieza a duplicarse en silencio.
        using var cn = db.AbrirComoAdmin();
        var producto = cn.ExecuteScalar<Guid>(
            "SELECT id FROM products WHERE tenant_id = @t LIMIT 1",
            new { t = TenantDatabaseFixture.TenantUno });

        var ex = Assert.Throws<Npgsql.PostgresException>(() => cn.Execute(@"
            INSERT INTO stock_items (tenant_id, product_id, quantity) VALUES (@t, @p, 1)",
            new { t = TenantDatabaseFixture.TenantUno, p = producto }));

        Assert.Equal("23505", ex.SqlState);
    }

    [Fact]
    public void Una_existencia_no_puede_apuntar_al_producto_de_otra_farmacia()
    {
        using var cn = db.AbrirComoAdmin();
        var productoAjeno = cn.ExecuteScalar<Guid>(
            "SELECT id FROM products WHERE tenant_id = @t LIMIT 1", new { t = db.TenantDos });

        var ex = Assert.Throws<Npgsql.PostgresException>(() => cn.Execute(@"
            INSERT INTO stock_items (tenant_id, product_id, quantity) VALUES (@t, @p, 1)",
            new { t = TenantDatabaseFixture.TenantUno, p = productoAjeno }));

        Assert.Equal("23503", ex.SqlState);   // foreign_key_violation
    }

    [Fact]
    public void El_modo_de_seguimiento_solo_admite_los_valores_previstos()
    {
        using var cn = db.AbrirComoAdmin();

        var ex = Assert.Throws<Npgsql.PostgresException>(() => cn.Execute(
            "UPDATE products SET tracking_mode = 'lote' WHERE tenant_id = @t",
            new { t = TenantDatabaseFixture.TenantUno }));

        Assert.Equal("23514", ex.SqlState);   // check_violation
    }
}
