using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;
using Inventory.Infrastructure;
using Npgsql;

namespace MultiTenancy.Tests;

/// <summary>
/// Sucursales, paso 4: traspasos de stock entre sucursales.
/// </summary>
[Collection("tenant-db")]
public class TraspasosTests(TenantDatabaseFixture db)
{
    private const int Uno = TenantDatabaseFixture.TenantUno;
    private const int Usuario = 1;

    private Guid Principal => db.SucursalDe(Uno);

    private StockTransferRepository Traspasos(Guid sucursal) => new(db.ContextoApp(Uno, sucursal));

    private Guid NuevaSucursal(string nombre)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>(
            "INSERT INTO branches (tenant_id, name) VALUES (@t, @n) RETURNING id", new { t = Uno, n = nombre });
    }

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

    private void EnSucursal(Guid sucursal, string sql, object parametros)
    {
        using var cn = db.AbrirComoApp(Uno, sucursal);
        cn.Execute(sql, parametros);
    }

    private static decimal StockEn(NpgsqlConnection cn, Guid producto, Guid sucursal) =>
        cn.ExecuteScalar<decimal>(
            "SELECT COALESCE(sum(quantity), 0) FROM stock_items WHERE product_id = @producto AND branch_id = @sucursal AND state",
            new { producto, sucursal });

    private static StockTransferRequest Pedido(Guid destino, params (Guid Producto, decimal Cantidad)[] lineas) => new()
    {
        DestBranchId = destino,
        Detail = lineas.Select(l => new StockTransferLineRequest { ProductId = l.Producto, Quantity = l.Cantidad }).ToList()
    };

    [Fact]
    public async Task Enviar_y_recibir_mueve_el_stock_y_conserva_lote_y_vencimiento()
    {
        var sur = NuevaSucursal("TR Sur");
        var producto = NuevoProducto("TR LOTES", "lot");
        EnSucursal(Principal, "SELECT fn_recibir_lote(@p, 4, 'TR-VIEJO', '2027-02-01', 1)", new { p = producto });
        EnSucursal(Principal, "SELECT fn_recibir_lote(@p, 10, 'TR-NUEVO', '2029-02-01', 1)", new { p = producto });

        var id = await Traspasos(Principal).CreateDraft(Pedido(sur, (producto, 6)), Usuario);
        await Traspasos(Principal).Send(id, Usuario);

        using (var admin = db.AbrirComoAdmin())
        {
            // En tránsito: salió del origen y todavía no está en ningún lado.
            Assert.Equal(8, StockEn(admin, producto, Principal));
            Assert.Equal(0, StockEn(admin, producto, sur));
        }

        await Traspasos(sur).Receive(id, [], Usuario);

        using var cn = db.AbrirComoAdmin();
        Assert.Equal(8, StockEn(cn, producto, Principal));
        Assert.Equal(6, StockEn(cn, producto, sur));

        // FEFO en el origen: se fue primero el lote que vence antes, y llegó con su fecha.
        var lotesSur = cn.Query<(string Lote, DateTime Vence, decimal Cantidad)>(@"
            SELECT lot_code, expiry_date, quantity FROM stock_items
             WHERE product_id = @producto AND branch_id = @sur ORDER BY lot_code",
            new { producto, sur }).ToList();
        Assert.Equal([("TR-NUEVO", new DateTime(2029, 2, 1), 2m), ("TR-VIEJO", new DateTime(2027, 2, 1), 4m)], lotesSur);

        Assert.Equal(0, cn.ExecuteScalar<int>("SELECT count(*) FROM v_stock_descuadrado WHERE product_id = @producto", new { producto }));
        Assert.Equal("recibido", cn.ExecuteScalar<string>("SELECT status FROM stock_transfers WHERE id = @id", new { id }));
    }

    [Fact]
    public async Task Lo_que_no_llega_queda_como_merma_del_origen()
    {
        var sur = NuevaSucursal("TR Faltante");
        var producto = NuevoProducto("TR FALTANTE");
        EnSucursal(Principal, "SELECT fn_mover_stock(@p, 10, 1)", new { p = producto });

        var id = await Traspasos(Principal).CreateDraft(Pedido(sur, (producto, 5)), Usuario);
        await Traspasos(Principal).Send(id, Usuario);

        var lote = (await Traspasos(sur).GetTransfer(id))!.Detail.Single().Lots.Single();
        await Traspasos(sur).Receive(id, [new ReceivedLotRequest { LotId = lote.Id, QuantityReceived = 3 }], Usuario);

        using var cn = db.AbrirComoAdmin();
        Assert.Equal(5, StockEn(cn, producto, Principal));
        Assert.Equal(3, StockEn(cn, producto, sur));

        var merma = cn.QueryFirst<(decimal Cantidad, Guid Sucursal)>(
            "SELECT cantidad, branch_id FROM v_mermas WHERE product_id = @producto AND reason = 'TRASPASO'", new { producto });
        Assert.Equal((2m, Principal), merma);
    }

    [Fact]
    public async Task No_se_puede_enviar_mas_de_lo_que_hay_ni_siquiera_sin_seguimiento()
    {
        // La venta sin seguimiento admite dejar el saldo en negativo; un traspaso
        // no: mandaría al destino stock que no existe.
        var sur = NuevaSucursal("TR Sin Stock");
        var producto = NuevoProducto("TR SIN STOCK");
        EnSucursal(Principal, "SELECT fn_mover_stock(@p, 2, 1)", new { p = producto });

        var id = await Traspasos(Principal).CreateDraft(Pedido(sur, (producto, 3)), Usuario);

        var ex = await Assert.ThrowsAsync<CustomException>(() => Traspasos(Principal).Send(id, Usuario));
        Assert.Contains("No hay stock suficiente", ex.Message);

        using var cn = db.AbrirComoAdmin();
        Assert.Equal(2, StockEn(cn, producto, Principal));
    }

    [Fact]
    public async Task Se_envia_desde_el_origen_y_se_recibe_en_el_destino()
    {
        var sur = NuevaSucursal("TR Roles");
        var producto = NuevoProducto("TR ROLES");
        EnSucursal(Principal, "SELECT fn_mover_stock(@p, 5, 1)", new { p = producto });
        var id = await Traspasos(Principal).CreateDraft(Pedido(sur, (producto, 1)), Usuario);

        await Assert.ThrowsAsync<CustomException>(() => Traspasos(sur).Send(id, Usuario));
        await Traspasos(Principal).Send(id, Usuario);
        await Assert.ThrowsAsync<CustomException>(() => Traspasos(Principal).Receive(id, [], Usuario));
        await Traspasos(sur).Receive(id, [], Usuario);

        // Recibido una vez, no se recibe de nuevo.
        await Assert.ThrowsAsync<CustomException>(() => Traspasos(sur).Receive(id, [], Usuario));
    }

    [Fact]
    public async Task Una_serie_traspasada_cambia_de_sucursal_y_sigue_siendo_unica()
    {
        var sur = NuevaSucursal("TR Series");
        var producto = NuevoProducto("TR SERIES", "serial");
        EnSucursal(Principal, "SELECT fn_recibir_serie(@p, 'TR-SERIE-1', NULL, 1)", new { p = producto });

        var id = await Traspasos(Principal).CreateDraft(Pedido(sur, (producto, 1)), Usuario);
        await Traspasos(Principal).Send(id, Usuario);
        await Traspasos(sur).Receive(id, [], Usuario);

        using (var cn = db.AbrirComoAdmin())
        {
            var activa = cn.QuerySingle<Guid>(
                "SELECT branch_id FROM stock_items WHERE serial_number = 'TR-SERIE-1' AND state");
            Assert.Equal(sur, activa);
        }

        // Sigue siendo la misma unidad: no se puede volver a dar de alta en ningún lado.
        using var cnPrincipal = db.AbrirComoApp(Uno, Principal);
        var ex = Assert.Throws<PostgresException>(() =>
            cnPrincipal.Execute("SELECT fn_recibir_serie(@p, 'TR-SERIE-1', NULL, 1)", new { p = producto }));
        Assert.Contains("ya está registrado", ex.MessageText);
    }

    [Fact]
    public async Task Solo_se_anula_o_modifica_un_borrador()
    {
        var sur = NuevaSucursal("TR Anular");
        var producto = NuevoProducto("TR ANULAR");
        EnSucursal(Principal, "SELECT fn_mover_stock(@p, 5, 1)", new { p = producto });

        var borrador = await Traspasos(Principal).CreateDraft(Pedido(sur, (producto, 1)), Usuario);
        await Traspasos(Principal).UpdateDraft(borrador, Pedido(sur, (producto, 2)), Usuario);
        Assert.Equal(2, (await Traspasos(Principal).GetTransfer(borrador))!.TotalQuantity);
        await Traspasos(Principal).Cancel(borrador, Usuario);

        var enviado = await Traspasos(Principal).CreateDraft(Pedido(sur, (producto, 1)), Usuario);
        await Traspasos(Principal).Send(enviado, Usuario);
        await Assert.ThrowsAsync<CustomException>(() => Traspasos(Principal).Cancel(enviado, Usuario));
        await Assert.ThrowsAsync<CustomException>(() =>
            Traspasos(Principal).UpdateDraft(enviado, Pedido(sur, (producto, 3)), Usuario));
    }

    [Fact]
    public async Task No_se_puede_traspasar_a_la_misma_sucursal()
    {
        var producto = NuevoProducto("TR MISMA");
        var ex = await Assert.ThrowsAsync<CustomException>(() =>
            Traspasos(Principal).CreateDraft(Pedido(Principal, (producto, 1)), Usuario));
        Assert.Contains("otra sucursal", ex.Message);
    }

    [Fact]
    public async Task Cada_sucursal_ve_sus_traspasos_y_una_tercera_no()
    {
        var sur = NuevaSucursal("TR Listado Sur");
        var otra = NuevaSucursal("TR Listado Otra");
        var producto = NuevoProducto("TR LISTADO");
        var id = await Traspasos(Principal).CreateDraft(Pedido(sur, (producto, 1)), Usuario);

        var desde = DateTime.UtcNow.AddDays(-1);
        var hasta = DateTime.UtcNow.AddDays(1);

        var enOrigen = (await Traspasos(Principal).GetTransfers(desde, hasta, "")).Single(t => t.Id == id);
        var enDestino = (await Traspasos(sur).GetTransfers(desde, hasta, "")).Single(t => t.Id == id);
        Assert.Equal("salida", enOrigen.Direction);
        Assert.Equal("entrada", enDestino.Direction);

        Assert.DoesNotContain(await Traspasos(otra).GetTransfers(desde, hasta, ""), t => t.Id == id);
        Assert.Null(await Traspasos(otra).GetTransfer(id));
    }

    [Fact]
    public async Task El_numero_es_correlativo_por_farmacia()
    {
        var sur = NuevaSucursal("TR Numeros");
        var producto = NuevoProducto("TR NUMEROS");

        var a = await Traspasos(Principal).GetTransfer(await Traspasos(Principal).CreateDraft(Pedido(sur, (producto, 1)), Usuario));
        var b = await Traspasos(Principal).GetTransfer(await Traspasos(Principal).CreateDraft(Pedido(sur, (producto, 1)), Usuario));

        Assert.Equal(a!.Number + 1, b!.Number);
    }
}
