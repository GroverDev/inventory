using Dapper;
using Npgsql;

namespace MultiTenancy.Tests;

/// <summary>
/// Seguimiento por lotes: recepción, FEFO y trazabilidad.
/// </summary>
[Collection("tenant-db")]
public class LotesTests(TenantDatabaseFixture db)
{
    /// <summary>Producto propio del test, para no interferir con otros.</summary>
    private Guid CrearProductoConLotes(NpgsqlConnection cn, string nombre)
    {
        var id = cn.ExecuteScalar<Guid>(@"
            INSERT INTO products
                (id, product_name, description, sale_price, available_in_pos,
                 laboratory_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id, current_stock)
            SELECT gen_random_uuid(), @nombre, '', 10, true,
                   (SELECT id FROM laboratories        WHERE tenant_id = @t LIMIT 1),
                   (SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1),
                   true, true, 1, now(), 1, now(), @t, 0
            RETURNING id",
            new { nombre, t = TenantDatabaseFixture.TenantUno });

        cn.Execute("INSERT INTO stock_items (tenant_id, product_id, quantity) VALUES (@t, @p, 0)",
            new { t = TenantDatabaseFixture.TenantUno, p = id });
        cn.Execute("SELECT fn_activar_lotes(@p)", new { p = id });
        return id;
    }

    [Fact]
    public void FEFO_consume_primero_lo_que_vence_antes()
    {
        using var cn = db.AbrirComoAdmin();
        var producto = CrearProductoConLotes(cn, "TEST FEFO ORDEN");

        // A propósito en desorden: lo que manda es la fecha, no el orden de carga.
        cn.Execute("SELECT fn_recibir_lote(@p, 10, 'TARDE',  '2030-01-01', 1)", new { p = producto });
        cn.Execute("SELECT fn_recibir_lote(@p, 10, 'PRONTO', '2027-01-01', 1)", new { p = producto });
        cn.Execute("SELECT fn_recibir_lote(@p, 10, 'MEDIO',  '2028-01-01', 1)", new { p = producto });

        var orden = cn.Query<string>(
            "SELECT lot_code FROM fn_asignar_fefo(@p, 30)", new { p = producto }).ToList();

        Assert.Equal(["PRONTO", "MEDIO", "TARDE"], orden);
    }

    [Fact]
    public void El_stock_heredado_sin_vencimiento_se_consume_antes_que_los_lotes()
    {
        // Es stock viejo del que no se conoce el vencimiento. Sacarlo primero
        // permite revisarlo físicamente mientras todavía está en el estante.
        using var cn = db.AbrirComoAdmin();
        var producto = CrearProductoConLotes(cn, "TEST FEFO HEREDADO");

        // Por fn_mover_stock, no por UPDATE directo: escribir stock_items a mano
        // deja la caché de products desalineada, que es justo lo que vigila
        // ExistenciasTests.La_cache_de_stock_coincide_con_el_libro_mayor.
        cn.Execute("SELECT fn_mover_stock(@p, 5, 1)", new { p = producto });
        cn.Execute("SELECT fn_recibir_lote(@p, 10, 'CON-FECHA', '2027-01-01', 1)", new { p = producto });

        var primero = cn.QueryFirst<string?>(
            "SELECT lot_code FROM fn_asignar_fefo(@p, 1)", new { p = producto });

        Assert.Null(primero);
    }

    [Fact]
    public void El_reparto_respeta_las_cantidades_disponibles()
    {
        using var cn = db.AbrirComoAdmin();
        var producto = CrearProductoConLotes(cn, "TEST FEFO REPARTO");

        cn.Execute("SELECT fn_recibir_lote(@p, 8,  'A', '2027-01-01', 1)", new { p = producto });
        cn.Execute("SELECT fn_recibir_lote(@p, 20, 'B', '2028-01-01', 1)", new { p = producto });

        var reparto = cn.Query<(string Lote, decimal Cantidad)>(
            "SELECT lot_code, cantidad FROM fn_asignar_fefo(@p, 12)", new { p = producto }).ToList();

        Assert.Equal(2, reparto.Count);
        Assert.Equal(("A", 8m), reparto[0]);
        Assert.Equal(("B", 4m), reparto[1]);
        Assert.Equal(12m, reparto.Sum(r => r.Cantidad));
    }

    [Fact]
    public void Con_lotes_no_se_puede_vender_mas_de_lo_que_hay()
    {
        // Vender de un lote que no se recibió no significa nada, y rompería la
        // trazabilidad que justifica todo el modelo.
        using var cn = db.AbrirComoAdmin();
        var producto = CrearProductoConLotes(cn, "TEST FEFO INSUFICIENTE");
        cn.Execute("SELECT fn_recibir_lote(@p, 5, 'UNICO', '2027-01-01', 1)", new { p = producto });

        var ex = Assert.Throws<PostgresException>(() =>
            cn.Query("SELECT * FROM fn_asignar_fefo(@p, 6)", new { p = producto }).ToList());

        Assert.Contains("insuficiente", ex.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sin_lotes_se_mantiene_la_politica_actual_de_permitir_sobreventa()
    {
        // La base tiene productos con saldo negativo heredado: el sistema hoy lo
        // permite, y cambiarlo es una decisión de negocio, no de este modelo.
        using var cn = db.AbrirComoAdmin();
        var producto = cn.ExecuteScalar<Guid>(
            "SELECT id FROM products WHERE tracking_mode = 'none' AND tenant_id = @t LIMIT 1",
            new { t = TenantDatabaseFixture.TenantUno });

        var disponible = cn.ExecuteScalar<decimal>(
            "SELECT COALESCE(sum(quantity),0) FROM stock_items WHERE product_id = @p", new { p = producto });

        var reparto = cn.Query("SELECT * FROM fn_asignar_fefo(@p, @c)",
            new { p = producto, c = disponible + 100 }).ToList();

        Assert.Single(reparto);   // la existencia implícita absorbe el faltante
    }

    [Fact]
    public void Recibir_el_mismo_lote_dos_veces_suma_sobre_la_misma_existencia()
    {
        // Caso habitual: un pedido parcial y su reposición traen el mismo lote.
        using var cn = db.AbrirComoAdmin();
        var producto = CrearProductoConLotes(cn, "TEST LOTE REPETIDO");

        cn.Execute("SELECT fn_recibir_lote(@p, 10, 'MISMO', '2027-05-05', 1)", new { p = producto });
        cn.Execute("SELECT fn_recibir_lote(@p, 7,  'MISMO', '2027-05-05', 1)", new { p = producto });

        var existencias = cn.Query<decimal>(
            "SELECT quantity FROM stock_items WHERE product_id = @p AND lot_code = 'MISMO'",
            new { p = producto }).ToList();

        Assert.Single(existencias);
        Assert.Equal(17m, existencias[0]);
    }

    [Fact]
    public void El_lote_es_obligatorio_al_recibir()
    {
        using var cn = db.AbrirComoAdmin();
        var producto = CrearProductoConLotes(cn, "TEST LOTE OBLIGATORIO");

        var ex = Assert.Throws<PostgresException>(() =>
            cn.Execute("SELECT fn_recibir_lote(@p, 5, '  ', NULL, 1)", new { p = producto }));

        Assert.Contains("obligatorio", ex.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void La_recepcion_deja_la_cache_alineada_con_el_libro_mayor()
    {
        using var cn = db.AbrirComoAdmin();
        var producto = CrearProductoConLotes(cn, "TEST LOTE CACHE");

        cn.Execute("SELECT fn_recibir_lote(@p, 25, 'X', '2027-01-01', 1)", new { p = producto });
        cn.Execute("SELECT fn_recibir_lote(@p, 15, 'Y', '2028-01-01', 1)", new { p = producto });

        Assert.Equal(40m, cn.ExecuteScalar<decimal>(
            "SELECT current_stock FROM products WHERE id = @p", new { p = producto }));

        Assert.Equal(0, cn.ExecuteScalar<int>(
            "SELECT count(*) FROM v_stock_descuadrado WHERE product_id = @p", new { p = producto }));
    }

    [Fact]
    public void La_vista_de_vencimientos_clasifica_por_urgencia()
    {
        using var cn = db.AbrirComoAdmin();
        var producto = CrearProductoConLotes(cn, "TEST VENCIMIENTOS");

        cn.Execute("SELECT fn_recibir_lote(@p, 1, 'V', CURRENT_DATE - 1,  1)", new { p = producto });
        cn.Execute("SELECT fn_recibir_lote(@p, 1, 'C', CURRENT_DATE + 10, 1)", new { p = producto });
        cn.Execute("SELECT fn_recibir_lote(@p, 1, 'P', CURRENT_DATE + 60, 1)", new { p = producto });
        cn.Execute("SELECT fn_recibir_lote(@p, 1, 'G', CURRENT_DATE + 400, 1)", new { p = producto });

        var estados = cn.Query<(string Lote, string Estado)>(
            "SELECT lot_code, estado FROM v_stock_por_vencer WHERE product_id = @p ORDER BY expiry_date",
            new { p = producto }).ToDictionary(x => x.Lote, x => x.Estado);

        Assert.Equal("VENCIDO",  estados["V"]);
        Assert.Equal("CRITICO",  estados["C"]);
        Assert.Equal("PROXIMO",  estados["P"]);
        Assert.Equal("VIGENTE",  estados["G"]);
    }

    [Fact]
    public void Los_lotes_de_una_farmacia_no_son_visibles_para_otra()
    {
        using var admin = db.AbrirComoAdmin();
        var producto = CrearProductoConLotes(admin, "TEST LOTE AISLADO");
        admin.Execute("SELECT fn_recibir_lote(@p, 5, 'SECRETO', '2027-01-01', 1)", new { p = producto });

        using var otra = db.AbrirComoApp(db.TenantDos);
        Assert.Equal(0, otra.ExecuteScalar<int>(
            "SELECT count(*) FROM stock_items WHERE lot_code = 'SECRETO'"));
        Assert.Equal(0, otra.ExecuteScalar<int>(
            "SELECT count(*) FROM v_stock_por_vencer WHERE lot_code = 'SECRETO'"));
    }
}
