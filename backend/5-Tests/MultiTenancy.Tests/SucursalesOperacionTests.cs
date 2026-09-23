using Dapper;
using Inventory.Infrastructure;
using Npgsql;

namespace MultiTenancy.Tests;

/// <summary>
/// Sucursales, paso 3: el stock, la caja y los movimientos son de la sucursal
/// donde se opera.
/// </summary>
[Collection("tenant-db")]
public class SucursalesOperacionTests(TenantDatabaseFixture db)
{
    private const int Uno = TenantDatabaseFixture.TenantUno;

    private Guid Principal => db.SucursalDe(Uno);

    /// <summary>Una sucursal propia del test, para no depender de las de otros.</summary>
    private Guid NuevaSucursal(string nombre)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>(
            "INSERT INTO branches (tenant_id, name) VALUES (@t, @n) RETURNING id", new { t = Uno, n = nombre });
    }

    /// <summary>Producto sin existencias en ninguna sucursal, como uno recién dado de alta.</summary>
    private Guid NuevoProducto(string nombre, string modo = "none")
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>(@"
            INSERT INTO products
                (id, product_name, description, sale_price, available_in_pos,
                 laboratory_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id, current_stock, tracking_mode)
            SELECT gen_random_uuid(), @nombre, '', 10, true,
                   (SELECT id FROM laboratories        WHERE tenant_id = @t LIMIT 1),
                   (SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1),
                   true, true, 1, now(), 1, now(), @t, 0, @modo
            RETURNING id", new { nombre, t = Uno, modo });
    }

    private static decimal StockEn(NpgsqlConnection cn, Guid producto, Guid sucursal) =>
        cn.ExecuteScalar<decimal>(
            "SELECT COALESCE(sum(quantity), 0) FROM stock_items WHERE product_id = @producto AND branch_id = @sucursal AND state",
            new { producto, sucursal });

    [Fact]
    public void Mover_stock_en_una_sucursal_no_toca_el_de_otra()
    {
        var sur = NuevaSucursal("OP Sur");
        var producto = NuevoProducto("OP PRODUCTO SUR");

        using (var cnSur = db.AbrirComoApp(Uno, sur))
            cnSur.Execute("SELECT fn_mover_stock(@p, 10, 1)", new { p = producto });
        using (var cnPrincipal = db.AbrirComoApp(Uno, Principal))
            cnPrincipal.Execute("SELECT fn_mover_stock(@p, -3, 1)", new { p = producto });

        using var admin = db.AbrirComoAdmin();
        Assert.Equal(10, StockEn(admin, producto, sur));
        Assert.Equal(-3, StockEn(admin, producto, Principal));

        // El total de la farmacia sigue siendo la suma de las sucursales.
        Assert.Equal(7, admin.ExecuteScalar<decimal>("SELECT current_stock FROM products WHERE id = @producto", new { producto }));
        Assert.Equal(0, admin.ExecuteScalar<int>("SELECT count(*) FROM v_stock_descuadrado WHERE product_id = @producto", new { producto }));
    }

    [Fact]
    public void Un_producto_nuevo_se_puede_vender_sin_existencia_previa()
    {
        // Antes del paso 3 la existencia implícita solo la creaba la migración de
        // existencias, así que un producto dado de alta después fallaba en su
        // primera venta.
        var producto = NuevoProducto("OP PRODUCTO RECIEN CREADO");

        using var cn = db.AbrirComoApp(Uno);
        var saldo = cn.QueryFirst<(Guid Item, decimal Antes, decimal Despues)>(
            "SELECT stock_item_id, stock_before, stock_after FROM fn_mover_stock(@p, -1, 1)", new { p = producto });

        Assert.Equal(0, saldo.Antes);
        Assert.Equal(-1, saldo.Despues);
    }

    [Fact]
    public void El_stock_que_ve_la_sucursal_es_el_suyo()
    {
        var sur = NuevaSucursal("OP Vista");
        var producto = NuevoProducto("OP PRODUCTO VISTA");
        using (var cnSur = db.AbrirComoApp(Uno, sur))
            cnSur.Execute("SELECT fn_mover_stock(@p, 4, 1)", new { p = producto });

        using var cnSur2 = db.AbrirComoApp(Uno, sur);
        using var cnPrincipal = db.AbrirComoApp(Uno, Principal);
        const string sql = "SELECT COALESCE((SELECT quantity FROM v_stock_actual WHERE product_id = @p), 0)";

        Assert.Equal(4, cnSur2.ExecuteScalar<decimal>(sql, new { p = producto }));
        Assert.Equal(0, cnPrincipal.ExecuteScalar<decimal>(sql, new { p = producto }));
    }

    [Fact]
    public async Task El_repositorio_de_productos_devuelve_el_stock_de_la_sucursal_activa()
    {
        var sur = NuevaSucursal("OP Repositorio");
        var producto = NuevoProducto("OP PRODUCTO REPO");
        using (var cnSur = db.AbrirComoApp(Uno, sur))
            cnSur.Execute("SELECT fn_mover_stock(@p, 6, 1)", new { p = producto });

        var enSur = await new ProductRepository(db.ContextoApp(Uno, sur)).GetProductStockPrice(producto);
        var enPrincipal = await new ProductRepository(db.ContextoApp(Uno, Principal)).GetProductStockPrice(producto);

        Assert.Equal(6, enSur.CurrentStock);
        Assert.Equal(0, enPrincipal.CurrentStock);
    }

    [Fact]
    public void El_mismo_lote_en_dos_sucursales_son_dos_existencias()
    {
        var sur = NuevaSucursal("OP Lote Doble");
        var producto = NuevoProducto("OP LOTE DOBLE", "lot");

        using (var cn = db.AbrirComoApp(Uno, Principal))
            cn.Execute("SELECT fn_recibir_lote(@p, 10, 'L-DOBLE', '2030-01-01', 1)", new { p = producto });
        using (var cn = db.AbrirComoApp(Uno, sur))
            cn.Execute("SELECT fn_recibir_lote(@p, 5, 'L-DOBLE', '2030-01-01', 1)", new { p = producto });

        using var admin = db.AbrirComoAdmin();
        Assert.Equal(10, StockEn(admin, producto, Principal));
        Assert.Equal(5, StockEn(admin, producto, sur));
    }

    [Fact]
    public void FEFO_solo_reparte_lo_que_hay_en_la_sucursal()
    {
        var sur = NuevaSucursal("OP FEFO");
        var producto = NuevoProducto("OP FEFO SUCURSAL", "lot");

        // En la Principal hay un lote que vence antes: igual no se puede entregar desde Sur.
        using (var cn = db.AbrirComoApp(Uno, Principal))
            cn.Execute("SELECT fn_recibir_lote(@p, 10, 'L-PRINCIPAL', '2027-01-01', 1)", new { p = producto });
        using (var cn = db.AbrirComoApp(Uno, sur))
            cn.Execute("SELECT fn_recibir_lote(@p, 5, 'L-SUR', '2030-01-01', 1)", new { p = producto });

        using var cnSur = db.AbrirComoApp(Uno, sur);
        var lotes = cnSur.Query<string>("SELECT lot_code FROM fn_asignar_fefo(@p, 5)", new { p = producto }).ToList();
        Assert.Equal(["L-SUR"], lotes);

        var ex = Assert.Throws<PostgresException>(() =>
            cnSur.Query("SELECT * FROM fn_asignar_fefo(@p, 6)", new { p = producto }).ToList());
        Assert.Contains("Stock insuficiente en esta sucursal", ex.MessageText);
    }

    [Fact]
    public void No_se_puede_mover_una_existencia_de_otra_sucursal()
    {
        var sur = NuevaSucursal("OP Ajena");
        var producto = NuevoProducto("OP EXISTENCIA AJENA", "lot");

        Guid itemPrincipal;
        using (var cn = db.AbrirComoApp(Uno, Principal))
            itemPrincipal = cn.QueryFirst<Guid>(
                "SELECT stock_item_id FROM fn_recibir_lote(@p, 3, 'L-AJENO', '2030-01-01', 1)", new { p = producto });

        using var cnSur = db.AbrirComoApp(Uno, sur);
        var ex = Assert.Throws<PostgresException>(() =>
            cnSur.Execute("SELECT fn_mover_stock(@p, -1, 1, @item)", new { p = producto, item = itemPrincipal }));
        Assert.Contains("pertenece a otra sucursal", ex.MessageText);
    }

    [Fact]
    public void Un_movimiento_de_stock_no_puede_registrarse_sobre_la_existencia_de_otra_sucursal()
    {
        // La regla en la estructura: aunque el backend insertara el movimiento a
        // mano, la FK compuesta lo rechaza.
        var sur = NuevaSucursal("OP Movimiento");
        var producto = NuevoProducto("OP MOVIMIENTO AJENO");
        Guid itemPrincipal;
        using (var cn = db.AbrirComoApp(Uno, Principal))
            itemPrincipal = cn.QueryFirst<Guid>("SELECT stock_item_id FROM fn_mover_stock(@p, 1, 1)", new { p = producto });

        using var cnSur = db.AbrirComoApp(Uno, sur);
        var ex = Assert.Throws<PostgresException>(() => cnSur.Execute(@"
            INSERT INTO stock_movements
                (product_id, movement_type, quantity, stock_before, stock_after, stock_item_id)
            VALUES (@p, 'AJUSTE', 1, 0, 1, @item)",
            new { p = producto, item = itemPrincipal }));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ex.SqlState);
    }

    [Fact]
    public void Se_puede_tener_una_caja_abierta_por_sucursal_pero_no_dos_en_la_misma()
    {
        var sur = NuevaSucursal("OP Caja");
        int usuario;
        using (var admin = db.AbrirComoAdmin())
        {
            usuario = admin.ExecuteScalar<int>("SELECT set_sequences_key('sec.users')");
            admin.Execute(@"
                INSERT INTO sec.users (id, user_name, password, email, full_name, change_password, is_active,
                                       created_by, modified_by, uuid, tenant_id)
                VALUES (@usuario, 'op-caja@test.local', 'x', 'op-caja@test.local', 'Caja', false, true, 1, 1, gen_random_uuid(), @t)",
                new { usuario, t = Uno });
        }

        const string abrir = "INSERT INTO cash_sessions (user_id, created_by, modified_by) VALUES (@usuario, @usuario, @usuario)";
        using (var cn = db.AbrirComoApp(Uno, Principal)) cn.Execute(abrir, new { usuario });
        using (var cn = db.AbrirComoApp(Uno, sur)) cn.Execute(abrir, new { usuario });

        using var cnSur = db.AbrirComoApp(Uno, sur);
        var ex = Assert.Throws<PostgresException>(() => cnSur.Execute(abrir, new { usuario }));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
    }

    [Fact]
    public void Un_movimiento_de_caja_no_puede_usar_la_caja_de_otra_sucursal()
    {
        var sur = NuevaSucursal("OP Caja Ajena");
        Guid cajaPrincipal;
        using (var cn = db.AbrirComoApp(Uno, Principal))
            cajaPrincipal = cn.ExecuteScalar<Guid>(@"
                INSERT INTO cash_sessions (user_id, created_by, modified_by, closed_at)
                SELECT id, id, id, now() FROM sec.users WHERE tenant_id = @t AND is_active LIMIT 1
                RETURNING id", new { t = Uno });

        using var cnSur = db.AbrirComoApp(Uno, sur);
        var ex = Assert.Throws<PostgresException>(() => cnSur.Execute(@"
            INSERT INTO cash_movements (cash_session_id, movement_type, amount, description, created_by, modified_by)
            VALUES (@caja, 'income', 5, 'ingreso cruzado', 1, 1)",
            new { caja = cajaPrincipal }));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ex.SqlState);
    }
}
