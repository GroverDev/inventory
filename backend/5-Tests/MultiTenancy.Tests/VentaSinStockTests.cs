using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Application;
using Inventory.Domain;
using Inventory.Infrastructure;
using Mapster;
using Microsoft.Extensions.Options;

namespace MultiTenancy.Tests;

/// <summary>
/// Vender más de lo que hay de un producto sin seguimiento: el cajero necesita a
/// un supervisor, y lo decide el servidor dentro de la transacción de la venta.
/// </summary>
[Collection("tenant-db")]
public class VentaSinStockTests(TenantDatabaseFixture db)
{
    private const int Uno = TenantDatabaseFixture.TenantUno;
    private const string Cajero = "Cajero";
    private const string Admin = "Administrador";

    static VentaSinStockTests() =>
        TypeAdapterConfig.GlobalSettings.Scan(typeof(SalesApplication).Assembly);

    private SalesApplication Ventas()
    {
        var ctx = db.ContextoApp(Uno, db.SucursalDe(Uno));
        return new SalesApplication(
            new SalesRepository(ctx, new SalesDetailRepository(), new SalePaymentRepository(), new SaleReturnRepository(ctx)),
            new ProductRepository(ctx),
            new CustomersRepository(ctx),
            new DiscountRepository(ctx),
            new DiscountLimitsApplication(new DiscountLimitsRepository(ctx),
                Options.Create(new PosSettings { MaxCashierDiscountPct = 15, MaxCashierDiscountAmount = 50 })),
            new PaymentMethodRepository(ctx));
    }

    private Guid NuevoProducto(string nombre, int stock, string seguimiento = "none")
    {
        using var admin = db.AbrirComoAdmin();
        var id = admin.ExecuteScalar<Guid>(@"
            INSERT INTO products
                (id, product_name, description, sale_price, available_in_pos,
                 laboratory_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id, current_stock)
            SELECT gen_random_uuid(), @nombre, '', 100, true,
                   (SELECT id FROM laboratories        WHERE tenant_id = @t LIMIT 1),
                   (SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1),
                   true, true, 1, now(), 1, now(), @t, 0
            RETURNING id", new { nombre, t = Uno });

        if (stock > 0)
            admin.Execute("SELECT fn_mover_stock(@id, @stock, 1, NULL, @sucursal)",
                new { id, stock, sucursal = db.SucursalDe(Uno) });
        if (seguimiento != "none")
            admin.Execute("UPDATE products SET tracking_mode = @seguimiento WHERE id = @id", new { id, seguimiento });
        return id;
    }

    private decimal Existencia(Guid producto)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<decimal>(
            "SELECT COALESCE(sum(quantity), 0) FROM stock_items WHERE product_id = @producto AND branch_id = @sucursal",
            new { producto, sucursal = db.SucursalDe(Uno) });
    }

    private int VentasDe(Guid producto)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<int>(
            "SELECT count(DISTINCT sale_id) FROM sales_detail WHERE product_id = @producto", new { producto });
    }

    private SaleRequest Venta(Guid producto, int cantidad, string supervisor = "")
    {
        using var admin = db.AbrirComoAdmin();
        var cliente = admin.ExecuteScalar<Guid>("SELECT id FROM customers WHERE tenant_id = @t AND is_generic LIMIT 1", new { t = Uno });
        var efectivo = admin.ExecuteScalar<Guid>("SELECT id FROM payment_methods WHERE tenant_id = @t AND name = 'Efectivo' LIMIT 1", new { t = Uno });

        return new SaleRequest
        {
            CustomerId = cliente.ToString(),
            IsActive = true,
            Detail = [new SaleDetailRequest { ProductId = producto.ToString(), Quantity = cantidad, UnitPrice = 100, LineSubtotal = 100 * cantidad }],
            Payments = [new SalePaymentRequest { PaymentMethodId = efectivo.ToString(), AmountGiven = 100000 }]
        };
    }

    [Fact]
    public async Task El_cajero_no_vende_sin_stock_y_se_le_pide_un_supervisor()
    {
        var producto = NuevoProducto("VSS SIN STOCK", stock: 0);

        var resp = await Ventas().CreateSale(Venta(producto, 1), 1, Cajero);

        Assert.False(resp.ok);
        Assert.Equal(StockSupervisorRequiredException.MessageId, resp.Message.Id);
        Assert.Contains("VSS SIN STOCK", resp.Message.Description);
        Assert.Equal(0, VentasDe(producto));      // nada quedó grabado
        Assert.Equal(0, Existencia(producto));    // ni el saldo se movió
    }

    [Fact]
    public async Task Pedir_mas_de_lo_que_hay_tambien_se_rechaza_y_no_descuenta_nada()
    {
        var producto = NuevoProducto("VSS PARCIAL", stock: 2);

        var resp = await Ventas().CreateSale(Venta(producto, 3), 1, Cajero);

        Assert.False(resp.ok);
        Assert.Equal(StockSupervisorRequiredException.MessageId, resp.Message.Id);
        Assert.Equal(2, Existencia(producto));    // la transacción se deshizo entera
    }

    [Fact]
    public async Task Con_la_firma_del_supervisor_el_faltante_se_vende_y_el_saldo_queda_negativo()
    {
        var producto = NuevoProducto("VSS AUTORIZADA", stock: 0);

        var resp = await Ventas().CreateSale(Venta(producto, 2), 1, Cajero, supervisorApproved: true);

        Assert.True(resp.ok, resp.Message.Description);
        Assert.Equal(-2, Existencia(producto));
    }

    [Fact]
    public async Task Con_stock_suficiente_el_cajero_vende_sin_supervisor()
    {
        var producto = NuevoProducto("VSS CON STOCK", stock: 5);

        var resp = await Ventas().CreateSale(Venta(producto, 5), 1, Cajero);

        Assert.True(resp.ok, resp.Message.Description);
        Assert.Equal(0, Existencia(producto));
    }

    [Fact]
    public async Task Un_rol_administrativo_no_necesita_supervisor()
    {
        var producto = NuevoProducto("VSS ADMIN", stock: 0);

        var resp = await Ventas().CreateSale(Venta(producto, 1), 1, Admin);

        Assert.True(resp.ok, resp.Message.Description);
    }

    [Fact]
    public async Task Con_lotes_ni_un_supervisor_autoriza_vender_sin_stock()
    {
        var producto = NuevoProducto("VSS LOTES", stock: 0, seguimiento: "lot");

        var resp = await Ventas().CreateSale(Venta(producto, 1), 1, Cajero, supervisorApproved: true);

        Assert.False(resp.ok);
        Assert.NotEqual(StockSupervisorRequiredException.MessageId, resp.Message.Id);   // no es "falta firma": es un rechazo
        Assert.Contains("Stock insuficiente", resp.Message.Description);
    }

    [Fact]
    public async Task Dos_cajas_que_venden_la_ultima_unidad_a_la_vez_no_pasan_las_dos()
    {
        // Sin el candado por producto las dos leerían "hay 1" y las dos venderían.
        var producto = NuevoProducto("VSS CARRERA", stock: 1);

        var resultados = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(_ => Task.Run(() => Ventas().CreateSale(Venta(producto, 1), 1, Cajero))));

        Assert.Equal(1, resultados.Count(r => r.ok));
        var rechazada = resultados.Single(r => !r.ok);
        Assert.Equal(StockSupervisorRequiredException.MessageId, rechazada.Message.Id);
        Assert.Equal(0, Existencia(producto));    // nunca negativo
    }
}
