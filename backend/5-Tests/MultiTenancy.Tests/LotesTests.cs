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

    /// <summary>
    /// Regresión del bug que dejó la recepción con lotes inservible desde el día
    /// que se escribió: <c>fn_recibir_lote</c> se declara
    /// <c>(uuid, numeric, varchar, date, integer)</c>, pero Npgsql manda el
    /// string como <c>text</c> y el DateTime como <c>timestamp</c>, y PostgreSQL
    /// no convierte timestamp→date al resolver una función. Sin los casts la
    /// llamada respondía "function does not exist", que además
    /// <c>ExceptionHandler</c> disfrazaba de "no se puede conectar a la base".
    ///
    /// El resto de las pruebas no lo detectaba porque llaman a la función con
    /// LITERALES ('2027-01-01'), y a esos PostgreSQL les asigna el tipo desde el
    /// contexto. Por eso esta prueba ejercita el repositorio real y no una copia
    /// de su SQL: es la única forma de que falle si alguien quita los casts.
    /// </summary>
    [Fact]
    public async Task La_recepcion_registra_el_lote_con_los_tipos_que_manda_Npgsql()
    {
        using var cn = db.AbrirComoAdmin();
        // El INSERT del repositorio no nombra tenant_id: lo resuelve el DEFAULT
        // current_tenant(), igual que en la aplicación.
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var producto = CrearProductoConLotes(cn, "TEST RECEPCION TIPADA");
        var compra = cn.ExecuteScalar<Guid>(
            "SELECT id FROM purchases WHERE tenant_id = @t LIMIT 1",
            new { t = TenantDatabaseFixture.TenantUno });
        var entrega = Guid.NewGuid();
        cn.Execute(@"
            INSERT INTO purchases_delivery
                (id, purchase_id, delivery_date, operation_uid, state,
                 created_by, created, modified_by, modified, tenant_id)
            VALUES (@id, @compra, now(), gen_random_uuid(), true,
                    1, now(), 1, now(), @t)",
            new { id = entrega, compra, t = TenantDatabaseFixture.TenantUno });

        var detalle = new Inventory.Domain.PurchaseDeliveryDetail
        {
            PurchaseDeliveryId = entrega,
            ProductId = producto,
            DeliveryQuantity = 12,
            OrderedQuantity = 12,
            UnitPrice = 4.5m,
            LotCode = "PARAM-1",
            ExpiryDate = new DateTime(2027, 3, 15),
            DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow),
            State = true,
            CreatedBy = 1,
            ModifiedBy = 1,
            Created = DateTime.Now,
            Modified = DateTime.Now,
        };

        using var tx = cn.BeginTransaction();
        var ok = await new Inventory.Infrastructure.PurchaseDetailRepository()
            .ReceiveOrdersDetail(compra, detalle, cn, tx);
        tx.Commit();

        Assert.True(ok);

        var lote = cn.QueryFirst<(string Lote, DateTime Vence, decimal Cantidad)>(
            "SELECT lot_code, expiry_date, quantity FROM stock_items WHERE product_id = @p AND lot_code IS NOT NULL",
            new { p = producto });

        Assert.Equal("PARAM-1", lote.Lote);
        Assert.Equal(new DateTime(2027, 3, 15), lote.Vence);
        Assert.Equal(12m, lote.Cantidad);
    }

    /// <summary>
    /// La venta que abarca varios lotes se parte en una línea por lote, que es lo
    /// que permite responder a un retiro del laboratorio. Ejercita el repositorio
    /// real por el mismo motivo que la prueba de recepción: este camino tampoco
    /// se había ejecutado nunca con lotes de verdad.
    ///
    /// Verifica además que el prorrateo no pierde ni inventa centavos: 69 entre 7
    /// unidades no da un importe exacto, y el sobrante del redondeo tiene que ir
    /// a la última porción para que las líneas sumen el total que envió el punto
    /// de venta.
    /// </summary>
    [Fact]
    public async Task La_venta_que_abarca_dos_lotes_se_parte_y_los_importes_cierran()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var producto = CrearProductoConLotes(cn, "TEST VENTA DOS LOTES");
        cn.Execute("SELECT fn_recibir_lote(@p, 4,  'VENCE-ANTES',   '2027-01-01', 1)", new { p = producto });
        cn.Execute("SELECT fn_recibir_lote(@p, 10, 'VENCE-DESPUES', '2028-01-01', 1)", new { p = producto });

        var venta = Guid.NewGuid();
        cn.Execute(@"
            INSERT INTO sales
                (id, customer_id, sale_date, is_active, state, created_by, created,
                 modified_by, modified, header_discount_amount, tenant_id)
            SELECT @id, (SELECT id FROM customers WHERE tenant_id = @t LIMIT 1),
                   now(), true, true, 1, now(), 1, now(), 0, @t",
            new { id = venta, t = TenantDatabaseFixture.TenantUno });

        var detalle = new Inventory.Domain.SaleDetail
        {
            SaleId = venta,
            ProductId = producto,
            Quantity = 7,
            UnitPrice = 10m,
            LineSubtotal = 70m,
            LineTotalDiscounts = 1m,
            LineTotal = 69m,
            State = true,
            CreatedBy = 1,
            ModifiedBy = 1,
            Created = DateTime.Now,
            Modified = DateTime.Now,
        };

        using var tx = cn.BeginTransaction();
        await new Inventory.Infrastructure.SalesDetailRepository()
            .CreateSaleDetail(detalle, cn, tx);
        tx.Commit();

        var lineas = cn.Query<(string Lote, int Cantidad, decimal Total)>(@"
            SELECT si.lot_code, sd.quantity, sd.line_total
              FROM sales_detail sd
                   INNER JOIN stock_items si ON si.id = sd.stock_item_id
             WHERE sd.sale_id = @v
             ORDER BY si.expiry_date", new { v = venta }).ToList();

        Assert.Equal(2, lineas.Count);
        Assert.Equal(("VENCE-ANTES", 4), (lineas[0].Lote, lineas[0].Cantidad));
        Assert.Equal(("VENCE-DESPUES", 3), (lineas[1].Lote, lineas[1].Cantidad));
        Assert.Equal(69m, lineas.Sum(l => l.Total));

        // El lote que vence antes queda consumido; del otro sale el resto.
        var saldos = cn.Query<(string Lote, decimal Cantidad)>(
            "SELECT lot_code, quantity FROM stock_items WHERE product_id = @p AND lot_code IS NOT NULL",
            new { p = producto }).ToDictionary(x => x.Lote, x => x.Cantidad);

        Assert.Equal(0m, saldos["VENCE-ANTES"]);
        Assert.Equal(7m, saldos["VENCE-DESPUES"]);
    }

    /// <summary>
    /// Lo devuelto vuelve al lote del que salió, no a la existencia sin lote.
    ///
    /// Es lo que hacía el código antes: <c>fn_mover_stock</c> sin existencia
    /// indicada usa la que no tiene lote, así que una unidad del lote X volvía
    /// como "sin lote" y perdía su vencimiento. El dato para hacerlo bien
    /// siempre estuvo — <c>sales_detail.stock_item_id</c> es NOT NULL —, solo
    /// que no se pasaba.
    /// </summary>
    [Fact]
    public async Task Lo_devuelto_vuelve_al_lote_del_que_salio()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var producto = CrearProductoConLotes(cn, "TEST DEVOLUCION LOTE");
        cn.Execute("SELECT fn_recibir_lote(@p, 10, 'LOTE-DEV', '2027-05-01', 1)", new { p = producto });

        var venta = Guid.NewGuid();
        cn.Execute(@"
            INSERT INTO sales
                (id, customer_id, sale_date, is_active, state, created_by, created,
                 modified_by, modified, header_discount_amount, tenant_id)
            SELECT @id, (SELECT id FROM customers WHERE tenant_id = @t LIMIT 1),
                   now(), true, true, 1, now(), 1, now(), 0, @t",
            new { id = venta, t = TenantDatabaseFixture.TenantUno });

        var detalleVenta = new Inventory.Domain.SaleDetail
        {
            SaleId = venta,
            ProductId = producto,
            Quantity = 6,
            UnitPrice = 10m,
            LineSubtotal = 60m,
            LineTotalDiscounts = 0m,
            LineTotal = 60m,
            State = true,
            CreatedBy = 1,
            ModifiedBy = 1,
            Created = DateTime.Now,
            Modified = DateTime.Now,
        };

        using (var tx = cn.BeginTransaction())
        {
            await new Inventory.Infrastructure.SalesDetailRepository()
                .CreateSaleDetail(detalleVenta, cn, tx);
            tx.Commit();
        }

        var lineaVenta = cn.QueryFirst<Guid>(
            "SELECT id FROM sales_detail WHERE sale_id = @v", new { v = venta });

        var devolucion = new Inventory.Domain.SaleReturn
        {
            SaleId = venta,
            ReturnDate = DateTime.Now,
            Reason = "Prueba",
            TotalReturned = 20m,
            IsFullReturn = false,
            State = true,
            CreatedBy = 1,
            ModifiedBy = 1,
            Created = DateTime.Now,
            Modified = DateTime.Now,
            Detail =
            [
                new Inventory.Domain.SaleReturnDetail
                {
                    SaleDetailId = lineaVenta,
                    ProductId = producto,
                    QuantityReturned = 2,
                    UnitPrice = 10m,
                    LineTotal = 20m,
                    State = true,
                    CreatedBy = 1,
                    ModifiedBy = 1,
                    Created = DateTime.Now,
                    Modified = DateTime.Now,
                }
            ],
        };

        await new Inventory.Infrastructure.SaleReturnRepository(
            db.ContextoApp(TenantDatabaseFixture.TenantUno)).CreateReturn(devolucion);

        var saldos = cn.Query<(string? Lote, decimal Cantidad)>(
            "SELECT lot_code, quantity FROM stock_items WHERE product_id = @p",
            new { p = producto }).ToList();

        // Vendidas 6 de 10 y devueltas 2: el lote tiene que quedar en 6, y la
        // existencia sin lote intacta en 0. Antes del arreglo era al revés.
        Assert.Equal(6m, saldos.Single(s => s.Lote == "LOTE-DEV").Cantidad);
        Assert.Equal(0m, saldos.Single(s => s.Lote is null).Cantidad);

        // El movimiento queda atado a la existencia concreta, no a la implícita.
        var itemDelMovimiento = cn.QueryFirst<Guid>(
            "SELECT stock_item_id FROM stock_movements WHERE product_id = @p AND movement_type = 'DEVOLUCION'",
            new { p = producto });
        var itemDelLote = cn.QueryFirst<Guid>(
            "SELECT id FROM stock_items WHERE product_id = @p AND lot_code = 'LOTE-DEV'",
            new { p = producto });

        Assert.Equal(itemDelLote, itemDelMovimiento);
    }

    /// <summary>
    /// La consulta de un retiro de mercado: dado un lote, a qué clientes se les
    /// vendió. El código se compara sin distinguir mayúsculas ni espacios porque
    /// en un retiro llega dictado por teléfono o copiado de un correo.
    /// </summary>
    [Fact]
    public async Task La_trazabilidad_devuelve_a_quien_se_le_vendio_el_lote()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var producto = CrearProductoConLotes(cn, "TEST TRAZABILIDAD");
        cn.Execute("SELECT fn_recibir_lote(@p, 10, 'RETIRO-1', '2027-06-01', 1)", new { p = producto });

        var venta = Guid.NewGuid();
        var cliente = cn.QueryFirst<string>(
            "SELECT full_name FROM customers WHERE tenant_id = @t LIMIT 1",
            new { t = TenantDatabaseFixture.TenantUno });

        cn.Execute(@"
            INSERT INTO sales
                (id, customer_id, sale_date, is_active, state, created_by, created,
                 modified_by, modified, header_discount_amount, tenant_id)
            SELECT @id, (SELECT id FROM customers WHERE tenant_id = @t LIMIT 1),
                   now(), true, true, 1, now(), 1, now(), 0, @t",
            new { id = venta, t = TenantDatabaseFixture.TenantUno });

        using (var tx = cn.BeginTransaction())
        {
            await new Inventory.Infrastructure.SalesDetailRepository().CreateSaleDetail(
                new Inventory.Domain.SaleDetail
                {
                    SaleId = venta,
                    ProductId = producto,
                    Quantity = 3,
                    UnitPrice = 10m,
                    LineSubtotal = 30m,
                    LineTotalDiscounts = 0m,
                    LineTotal = 30m,
                    State = true,
                    CreatedBy = 1,
                    ModifiedBy = 1,
                    Created = DateTime.Now,
                    Modified = DateTime.Now,
                }, cn, tx);
            tx.Commit();
        }

        var repo = new Inventory.Infrastructure.StockMovementRepository(
            db.ContextoApp(TenantDatabaseFixture.TenantUno));

        // Tal cual está escrito.
        var exacto = await repo.GetTraceability("RETIRO-1");
        Assert.Single(exacto);
        Assert.Equal(cliente, exacto[0].Cliente);
        Assert.Equal(3, exacto[0].Quantity);
        Assert.Equal(venta, exacto[0].SaleId);

        // Y como suele llegar: en minúsculas y con espacios de más.
        var sucio = await repo.GetTraceability("  retiro-1 ");
        Assert.Single(sucio);

        // Un lote que no existe no es un error, es una respuesta vacía.
        Assert.Empty(await repo.GetTraceability("NO-EXISTE"));
    }

    /// <summary>Producto con seguimiento por número de serie.</summary>
    private Guid CrearProductoConSeries(NpgsqlConnection cn, string nombre)
    {
        var id = CrearProductoConLotes(cn, nombre);
        // CrearProductoConLotes deja el producto en 'lot'; se revierte a 'none'
        // para poder activar series, porque fn_activar_series rechaza a
        // propósito el paso desde lotes.
        cn.Execute("UPDATE products SET tracking_mode = 'none' WHERE id = @p", new { p = id });
        cn.Execute("SELECT fn_activar_series(@p)", new { p = id });
        return id;
    }

    /// <summary>
    /// Una unidad, una existencia: recibir 3 aparatos crea 3 existencias de
    /// cantidad 1 y 3 movimientos, no una sola línea de 3. Es lo que después
    /// permite decir qué unidad concreta se entregó ante una garantía.
    ///
    /// Ejercita el repositorio real por el mismo motivo que la prueba de lotes:
    /// fn_recibir_serie recibe varchar y date, y Npgsql manda text y timestamp.
    /// </summary>
    [Fact]
    public async Task La_recepcion_por_series_crea_una_existencia_por_unidad()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var producto = CrearProductoConSeries(cn, "TEST SERIES RECEPCION");
        var compra = cn.ExecuteScalar<Guid>(
            "SELECT id FROM purchases WHERE tenant_id = @t LIMIT 1",
            new { t = TenantDatabaseFixture.TenantUno });

        var entrega = Guid.NewGuid();
        cn.Execute(@"
            INSERT INTO purchases_delivery
                (id, purchase_id, delivery_date, operation_uid, state,
                 created_by, created, modified_by, modified, tenant_id)
            VALUES (@id, @compra, now(), gen_random_uuid(), true,
                    1, now(), 1, now(), @t)",
            new { id = entrega, compra, t = TenantDatabaseFixture.TenantUno });

        var detalle = new Inventory.Domain.PurchaseDeliveryDetail
        {
            PurchaseDeliveryId = entrega,
            ProductId = producto,
            DeliveryQuantity = 3,
            OrderedQuantity = 3,
            UnitPrice = 250m,
            SerialNumbers = ["SN-A", "SN-B", "SN-C"],
            ExpiryDate = null,
            DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow),
            State = true,
            CreatedBy = 1,
            ModifiedBy = 1,
            Created = DateTime.Now,
            Modified = DateTime.Now,
        };

        using (var tx = cn.BeginTransaction())
        {
            await new Inventory.Infrastructure.PurchaseDetailRepository()
                .ReceiveOrdersDetail(compra, detalle, cn, tx);
            tx.Commit();
        }

        var series = cn.Query<(string Serie, decimal Cantidad)>(
            "SELECT serial_number, quantity FROM stock_items WHERE product_id = @p AND serial_number IS NOT NULL ORDER BY serial_number",
            new { p = producto }).ToList();

        Assert.Equal(3, series.Count);
        Assert.All(series, s => Assert.Equal(1m, s.Cantidad));
        Assert.Equal(["SN-A", "SN-B", "SN-C"], series.Select(s => s.Serie));

        // Un movimiento por unidad: el libro mayor identifica cada entrada.
        Assert.Equal(3, cn.ExecuteScalar<int>(
            "SELECT count(*) FROM stock_movements WHERE product_id = @p AND movement_type = 'COMPRA'",
            new { p = producto }));

        Assert.Equal(3, cn.ExecuteScalar<int>(
            "SELECT current_stock FROM products WHERE id = @p", new { p = producto }));
    }

    [Fact]
    public async Task La_recepcion_por_series_exige_tantos_numeros_como_unidades()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var producto = CrearProductoConSeries(cn, "TEST SERIES FALTANTES");
        var compra = cn.ExecuteScalar<Guid>(
            "SELECT id FROM purchases WHERE tenant_id = @t LIMIT 1",
            new { t = TenantDatabaseFixture.TenantUno });

        var entrega = Guid.NewGuid();
        cn.Execute(@"
            INSERT INTO purchases_delivery
                (id, purchase_id, delivery_date, operation_uid, state,
                 created_by, created, modified_by, modified, tenant_id)
            VALUES (@id, @compra, now(), gen_random_uuid(), true,
                    1, now(), 1, now(), @t)",
            new { id = entrega, compra, t = TenantDatabaseFixture.TenantUno });

        // Tres unidades pero solo dos números: dejarlo pasar dejaría una unidad
        // que nadie puede identificar después.
        var detalle = new Inventory.Domain.PurchaseDeliveryDetail
        {
            PurchaseDeliveryId = entrega,
            ProductId = producto,
            DeliveryQuantity = 3,
            OrderedQuantity = 3,
            UnitPrice = 250m,
            SerialNumbers = ["SN-X", "SN-Y"],
            DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow),
            State = true,
            CreatedBy = 1,
            ModifiedBy = 1,
            Created = DateTime.Now,
            Modified = DateTime.Now,
        };

        using var tx = cn.BeginTransaction();
        await Assert.ThrowsAsync<Common.Utilities.Exceptions.CustomException>(() =>
            new Inventory.Infrastructure.PurchaseDetailRepository()
                .ReceiveOrdersDetail(compra, detalle, cn, tx));
        tx.Rollback();
    }

    /// <summary>
    /// Al vender una unidad serializada manda el mostrador, no FEFO. La garantía
    /// queda atada al número que se entrega en mano, así que entregar «la que
    /// vencía antes» en lugar de la elegida deja el registro mintiendo.
    /// </summary>
    [Fact]
    public async Task La_serie_elegida_en_el_mostrador_gana_sobre_FEFO()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var producto = CrearProductoConSeries(cn, "TEST VENTA SERIE");
        // FEFO elegiría la primera; el mostrador va a entregar la segunda.
        cn.Execute("SELECT fn_recibir_serie(@p, 'SN-VENCE-ANTES',   '2027-01-01', 1)", new { p = producto });
        cn.Execute("SELECT fn_recibir_serie(@p, 'SN-VENCE-DESPUES', '2029-01-01', 1)", new { p = producto });

        var venta = Guid.NewGuid();
        cn.Execute(@"
            INSERT INTO sales
                (id, customer_id, sale_date, is_active, state, created_by, created,
                 modified_by, modified, header_discount_amount, tenant_id)
            SELECT @id, (SELECT id FROM customers WHERE tenant_id = @t LIMIT 1),
                   now(), true, true, 1, now(), 1, now(), 0, @t",
            new { id = venta, t = TenantDatabaseFixture.TenantUno });

        using (var tx = cn.BeginTransaction())
        {
            await new Inventory.Infrastructure.SalesDetailRepository().CreateSaleDetail(
                new Inventory.Domain.SaleDetail
                {
                    SaleId = venta,
                    ProductId = producto,
                    Quantity = 1,
                    UnitPrice = 100m,
                    LineSubtotal = 100m,
                    LineTotalDiscounts = 0m,
                    LineTotal = 100m,
                    SerialNumbers = ["sn-vence-despues"], // tal como se teclea
                    State = true,
                    CreatedBy = 1,
                    ModifiedBy = 1,
                    Created = DateTime.Now,
                    Modified = DateTime.Now,
                }, cn, tx);
            tx.Commit();
        }

        var vendida = cn.QueryFirst<string>(@"
            SELECT si.serial_number
              FROM sales_detail sd
                   INNER JOIN stock_items si ON si.id = sd.stock_item_id
             WHERE sd.sale_id = @v", new { v = venta });

        Assert.Equal("SN-VENCE-DESPUES", vendida);

        // Y la que FEFO habría elegido sigue en el estante.
        Assert.Equal(1m, cn.ExecuteScalar<decimal>(
            "SELECT quantity FROM stock_items WHERE product_id = @p AND serial_number = 'SN-VENCE-ANTES'",
            new { p = producto }));
    }

    [Fact]
    public async Task No_se_puede_vender_una_serie_que_ya_no_esta()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var producto = CrearProductoConSeries(cn, "TEST SERIE AGOTADA");
        cn.Execute("SELECT fn_recibir_serie(@p, 'SN-UNICA', '2027-01-01', 1)", new { p = producto });

        var venta = Guid.NewGuid();
        cn.Execute(@"
            INSERT INTO sales
                (id, customer_id, sale_date, is_active, state, created_by, created,
                 modified_by, modified, header_discount_amount, tenant_id)
            SELECT @id, (SELECT id FROM customers WHERE tenant_id = @t LIMIT 1),
                   now(), true, true, 1, now(), 1, now(), 0, @t",
            new { id = venta, t = TenantDatabaseFixture.TenantUno });

        Inventory.Domain.SaleDetail Linea(string serie) => new()
        {
            SaleId = venta,
            ProductId = producto,
            Quantity = 1,
            UnitPrice = 100m,
            LineSubtotal = 100m,
            LineTotalDiscounts = 0m,
            LineTotal = 100m,
            SerialNumbers = [serie],
            State = true,
            CreatedBy = 1,
            ModifiedBy = 1,
            Created = DateTime.Now,
            Modified = DateTime.Now,
        };

        // Una serie inventada no puede pasar como stock.
        using (var tx = cn.BeginTransaction())
        {
            await Assert.ThrowsAsync<Common.Utilities.Exceptions.CustomException>(() =>
                new Inventory.Infrastructure.SalesDetailRepository()
                    .CreateSaleDetail(Linea("SN-QUE-NO-EXISTE"), cn, tx));
            tx.Rollback();
        }

        // Y la misma unidad no se puede vender dos veces: es el caso de dos cajas
        // cobrando a la vez, donde la segunda tiene que enterarse.
        using (var tx = cn.BeginTransaction())
        {
            await new Inventory.Infrastructure.SalesDetailRepository()
                .CreateSaleDetail(Linea("SN-UNICA"), cn, tx);
            tx.Commit();
        }

        using (var tx = cn.BeginTransaction())
        {
            await Assert.ThrowsAsync<Common.Utilities.Exceptions.CustomException>(() =>
                new Inventory.Infrastructure.SalesDetailRepository()
                    .CreateSaleDetail(Linea("SN-UNICA"), cn, tx));
            tx.Rollback();
        }
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

    /// <summary>
    /// El kardex por lote (pantalla "Historial" desde Vencimientos): filtrar por
    /// stock_item_id no debe traer movimientos de otro lote del mismo producto,
    /// aunque ambos compartan product_id.
    /// </summary>
    [Fact]
    public async Task El_historial_por_lote_no_mezcla_movimientos_de_otros_lotes_del_mismo_producto()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");
        var producto = CrearProductoConLotes(cn, "TEST KARDEX POR LOTE");
        cn.Execute("SELECT fn_recibir_lote(@p, 10, 'KARDEX-A', '2027-01-01', 1)", new { p = producto });
        cn.Execute("SELECT fn_recibir_lote(@p, 10, 'KARDEX-B', '2027-02-01', 1)", new { p = producto });

        var itemA = cn.QueryFirst<Guid>(
            "SELECT id FROM stock_items WHERE product_id = @p AND lot_code = 'KARDEX-A'", new { p = producto });
        var itemB = cn.QueryFirst<Guid>(
            "SELECT id FROM stock_items WHERE product_id = @p AND lot_code = 'KARDEX-B'", new { p = producto });

        void RegistrarMovimiento(Guid stockItemId, string reason)
        {
            cn.Execute(@"
                INSERT INTO stock_movements
                    (id, product_id, stock_item_id, movement_type, quantity, stock_before, stock_after,
                     reason, state, created_by, created, modified_by, modified)
                VALUES (gen_random_uuid(), @p, @item, 'AJUSTE', -1, 10, 9,
                        @reason, true, 1, now(), 1, now())",
                new { p = producto, item = stockItemId, reason });
        }

        RegistrarMovimiento(itemA, "MOVIMIENTO DE A");
        RegistrarMovimiento(itemB, "MOVIMIENTO DE B");

        var repo = new Inventory.Infrastructure.StockMovementRepository(db.ContextoApp(TenantDatabaseFixture.TenantUno));

        var soloA = await repo.GetMovementsByProduct(producto, itemA);
        Assert.Single(soloA);
        Assert.Equal("MOVIMIENTO DE A", soloA[0].Reason);
        Assert.Equal("KARDEX-A", soloA[0].LotCode);

        var todos = await repo.GetMovementsByProduct(producto, null);
        Assert.Equal(2, todos.Count);
    }
}
