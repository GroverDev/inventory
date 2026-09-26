using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Application;
using Inventory.Domain;
using Inventory.Infrastructure;
using Mapster;
using Microsoft.Extensions.Options;

namespace MultiTenancy.Tests;

/// <summary>Cálculo y topes del descuento manual, sin base de datos.</summary>
public class ReglasDeDescuentoTests
{
    private static readonly EffectiveDiscountLimits Topes = new()
    {
        CashierMaxPct = 15, CashierMaxAmount = 50, GeneralMaxPct = 40, GeneralMaxAmount = 200
    };

    [Theory]
    [InlineData("Percentage", 10, 200, 20)]
    [InlineData("Percentage", 100, 200, 200)]
    [InlineData("FixedAmount", 30, 200, 30)]
    [InlineData("FixedAmount", 500, 200, 200)]   // nunca más que la base
    [InlineData("", 0, 200, 0)]                   // sin descuento
    public void El_importe_sale_del_tipo_y_el_valor(string tipo, decimal valor, decimal baseImporte, decimal esperado) =>
        Assert.Equal(esperado, SaleDiscountRules.ManualAmount(tipo, valor, baseImporte));

    [Theory]
    [InlineData("Percentage", 101)]
    [InlineData("Percentage", -5)]
    [InlineData("Regalo", 10)]
    [InlineData("", 10)]
    public void Tipos_desconocidos_y_valores_fuera_de_rango_se_rechazan(string tipo, decimal valor) =>
        Assert.Throws<CustomException>(() => SaleDiscountRules.ManualAmount(tipo, valor, 100));

    [Fact]
    public void El_cajero_sin_autorizacion_no_pasa_su_tope()
    {
        SaleDiscountRules.CheckLimits("Percentage", 15, Topes, cajeroSinAutorizacion: true, "por línea");
        Assert.Throws<CustomException>(() =>
            SaleDiscountRules.CheckLimits("Percentage", 16, Topes, cajeroSinAutorizacion: true, "por línea"));
        Assert.Throws<CustomException>(() =>
            SaleDiscountRules.CheckLimits("FixedAmount", 51, Topes, cajeroSinAutorizacion: true, "global"));
    }

    [Fact]
    public void Nadie_pasa_el_tope_maximo_ni_con_autorizacion()
    {
        SaleDiscountRules.CheckLimits("Percentage", 40, Topes, cajeroSinAutorizacion: false, "por línea");
        var ex = Assert.Throws<CustomException>(() =>
            SaleDiscountRules.CheckLimits("Percentage", 41, Topes, cajeroSinAutorizacion: false, "por línea"));
        Assert.Contains("tope máximo", ex.Message);
    }

    [Fact]
    public void Cada_tope_se_hereda_por_separado_de_la_sucursal_a_la_empresa_y_al_default()
    {
        var empresa = new DiscountLimitLevel { CashierMaxPct = 10, GeneralMaxAmount = 300 };
        var sucursal = new DiscountLimitLevel { BranchId = Guid.NewGuid(), CashierMaxPct = 5 };
        var defaults = new PosSettings { MaxCashierDiscountPct = 15, MaxCashierDiscountAmount = 50 };

        var r = SaleDiscountRules.Resolve(empresa, sucursal, defaults);

        Assert.Equal(5, r.CashierMaxPct);          // de la sucursal
        Assert.Equal(50, r.CashierMaxAmount);      // del default
        Assert.Null(r.GeneralMaxPct);              // sin tope
        Assert.Equal(300, r.GeneralMaxAmount);     // de la empresa
    }
}

/// <summary>
/// Ventas reales por SalesApplication: el servidor recalcula precios y
/// descuentos y aplica los topes de la sucursal.
/// </summary>
[Collection("tenant-db")]
public class VentasConDescuentoTests(TenantDatabaseFixture db)
{
    private const int Uno = TenantDatabaseFixture.TenantUno;
    private const string Cajero = "Cajero";
    private const string Admin = "Administrador";

    static VentasConDescuentoTests() =>
        TypeAdapterConfig.GlobalSettings.Scan(typeof(SalesApplication).Assembly);

    private SalesApplication Ventas(Guid sucursal)
    {
        var ctx = db.ContextoApp(Uno, sucursal);
        return new SalesApplication(
            new SalesRepository(ctx, new SalesDetailRepository(), new SalePaymentRepository(), new SaleReturnRepository(ctx)),
            new ProductRepository(ctx),
            new CustomersRepository(ctx),
            new DiscountRepository(ctx),
            new DiscountLimitsApplication(new DiscountLimitsRepository(ctx),
                Options.Create(new PosSettings { MaxCashierDiscountPct = 15, MaxCashierDiscountAmount = 50 })),
            new PaymentMethodRepository(ctx));
    }

    private Guid NuevaSucursal(string nombre)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>(
            "INSERT INTO branches (tenant_id, name) VALUES (@t, @n) RETURNING id", new { t = Uno, n = nombre });
    }

    private Guid NuevoProducto(string nombre, decimal precio)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>(@"
            INSERT INTO products
                (id, product_name, description, sale_price, available_in_pos,
                 laboratory_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id, current_stock)
            SELECT gen_random_uuid(), @nombre, '', @precio, true,
                   (SELECT id FROM laboratories        WHERE tenant_id = @t LIMIT 1),
                   (SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1),
                   true, true, 1, now(), 1, now(), @t, 0
            RETURNING id", new { nombre, precio, t = Uno });
    }

    /// <summary>
    /// Existencia en una sucursal. Un cajero no vende sin stock sin la firma de un
    /// supervisor, y acá lo que se prueba son los descuentos, no el stock.
    /// </summary>
    private void ConStock(Guid producto, Guid sucursal, int cantidad = 1000)
    {
        using var admin = db.AbrirComoAdmin();
        admin.Execute("SELECT fn_mover_stock(@producto, @cantidad, 1, NULL, @sucursal)", new { producto, cantidad, sucursal });
    }

    private SaleRequest Venta(Guid producto, decimal precio, int cantidad, string tipo = "", decimal valor = 0,
                              decimal importeQueMandaElCliente = 0)
    {
        using var admin = db.AbrirComoAdmin();
        var cliente = admin.ExecuteScalar<Guid>("SELECT id FROM customers WHERE tenant_id = @t AND is_generic LIMIT 1", new { t = Uno });
        var efectivo = admin.ExecuteScalar<Guid>("SELECT id FROM payment_methods WHERE tenant_id = @t AND name = 'Efectivo' LIMIT 1", new { t = Uno });

        return new SaleRequest
        {
            CustomerId = cliente.ToString(),
            IsActive = true,
            Detail =
            [
                new SaleDetailRequest
                {
                    ProductId = producto.ToString(), Quantity = cantidad, UnitPrice = precio,
                    LineSubtotal = precio * cantidad, LineTotalDiscounts = importeQueMandaElCliente,
                    DiscountType = tipo, DiscountValue = valor
                }
            ],
            Payments = [new SalePaymentRequest { PaymentMethodId = efectivo.ToString(), AmountGiven = 100000 }]
        };
    }

    private decimal DescuentoGrabado(string ventaId)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<decimal>("SELECT total_discounts FROM sales WHERE id = @id::uuid", new { id = ventaId });
    }

    [Fact]
    public async Task El_importe_del_descuento_lo_calcula_el_servidor_no_el_cliente()
    {
        var producto = NuevoProducto("DS PRODUCTO IMPORTE", 100);
        ConStock(producto, db.SucursalDe(Uno));

        // Declara 10 % (dentro del tope) pero pide descontar 90 de 100.
        var resp = await Ventas(db.SucursalDe(Uno)).CreateSale(
            Venta(producto, 100, 1, "Percentage", 10, importeQueMandaElCliente: 90), 1, Cajero);

        Assert.True(resp.ok, resp.Message?.Description);
        Assert.Equal(10, DescuentoGrabado(resp.Data!));
    }

    [Fact]
    public async Task Un_precio_distinto_al_de_la_sucursal_se_rechaza()
    {
        var producto = NuevoProducto("DS PRODUCTO PRECIO", 100);

        var resp = await Ventas(db.SucursalDe(Uno)).CreateSale(Venta(producto, 1, 1), 1, Admin);

        Assert.False(resp.ok);
        Assert.Contains("Actualice el punto de venta", resp.Message.Description);
    }

    [Fact]
    public async Task Un_descuento_sin_tipo_valido_se_rechaza()
    {
        var producto = NuevoProducto("DS PRODUCTO TIPO", 100);

        var resp = await Ventas(db.SucursalDe(Uno)).CreateSale(Venta(producto, 100, 1, "Regalo", 50, 50), 1, Admin);

        Assert.False(resp.ok);
    }

    [Fact]
    public async Task Los_topes_de_la_sucursal_rigen_para_cajero_y_administrador()
    {
        var sucursal = NuevaSucursal("DS Topes");
        var producto = NuevoProducto("DS PRODUCTO TOPES", 100);
        ConStock(producto, sucursal);
        ConStock(producto, db.SucursalDe(Uno));   // el último caso vende en la Principal
        await new DiscountLimitsRepository(db.ContextoApp(Uno, sucursal)).SaveLevels(
            [new DiscountLimitLevel { BranchId = sucursal, CashierMaxPct = 5, GeneralMaxPct = 20 }], 1);

        var ventas = Ventas(sucursal);

        // Cajero: 5 % pasa, 6 % no (sin autorización), 6 % sí con autorización.
        Assert.True((await ventas.CreateSale(Venta(producto, 100, 1, "Percentage", 5), 1, Cajero)).ok);
        Assert.False((await ventas.CreateSale(Venta(producto, 100, 1, "Percentage", 6), 1, Cajero)).ok);
        Assert.True((await ventas.CreateSale(Venta(producto, 100, 1, "Percentage", 6), 1, Cajero, supervisorApproved: true)).ok);

        // Nadie pasa el tope máximo, ni el administrador ni con autorización.
        Assert.True((await ventas.CreateSale(Venta(producto, 100, 1, "Percentage", 20), 1, Admin)).ok);
        Assert.False((await ventas.CreateSale(Venta(producto, 100, 1, "Percentage", 21), 1, Admin)).ok);
        Assert.False((await ventas.CreateSale(Venta(producto, 100, 1, "Percentage", 21), 1, Cajero, supervisorApproved: true)).ok);

        // En la Principal no hay topes cargados: rige el default de 15 % para el cajero.
        Assert.True((await Ventas(db.SucursalDe(Uno)).CreateSale(Venta(producto, 100, 1, "Percentage", 15), 1, Cajero)).ok);
    }
}
