using Dapper;
using Inventory.Domain;
using Inventory.Infrastructure;
using Npgsql;

namespace MultiTenancy.Tests;

/// <summary>
/// Sucursales, paso 5: precio y mínimo de reposición por sucursal.
/// </summary>
[Collection("tenant-db")]
public class PreciosSucursalTests(TenantDatabaseFixture db)
{
    private const int Uno = TenantDatabaseFixture.TenantUno;

    private Guid Principal => db.SucursalDe(Uno);

    private Guid NuevaSucursal(string nombre)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>(
            "INSERT INTO branches (tenant_id, name) VALUES (@t, @n) RETURNING id", new { t = Uno, n = nombre });
    }

    private Guid NuevoProducto(string nombre, decimal precio, int minimo)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>(@"
            INSERT INTO products
                (id, product_name, description, sale_price, min_reorder_quantity, available_in_pos,
                 laboratory_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id, current_stock)
            SELECT gen_random_uuid(), @nombre, '', @precio, @minimo, true,
                   (SELECT id FROM laboratories        WHERE tenant_id = @t LIMIT 1),
                   (SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1),
                   true, true, 1, now(), 1, now(), @t, 0
            RETURNING id", new { nombre, precio, minimo, t = Uno });
    }

    private ProductBranchSettingsRepository Ajustes(Guid sucursal) => new(db.ContextoApp(Uno, sucursal));

    [Fact]
    public async Task Cada_sucursal_vende_a_su_precio_y_sin_excepcion_usa_el_base()
    {
        var aeropuerto = NuevaSucursal("PR Aeropuerto");
        var producto = NuevoProducto("PR PRODUCTO PRECIO", 10m, 5);

        await Ajustes(Principal).SaveSettings(producto,
            [new ProductBranchSettingRequest { BranchId = aeropuerto, SalePrice = 14.50m }], 1);

        var enAeropuerto = await new ProductRepository(db.ContextoApp(Uno, aeropuerto)).GetProductStockPrice(producto);
        var enPrincipal = await new ProductRepository(db.ContextoApp(Uno, Principal)).GetProductStockPrice(producto);

        Assert.Equal(14.50m, enAeropuerto.SalePrice);
        Assert.Equal(10m, enPrincipal.SalePrice);
    }

    [Fact]
    public async Task La_ficha_recibe_el_precio_base_ademas_del_de_la_sucursal()
    {
        // Si la ficha editara SalePrice, guardar copiaría la excepción al catálogo.
        var sucursal = NuevaSucursal("PR Ficha");
        var producto = NuevoProducto("PR PRODUCTO FICHA", 20m, 3);
        await Ajustes(sucursal).SaveSettings(producto,
            [new ProductBranchSettingRequest { BranchId = sucursal, SalePrice = 25m, MinReorderQuantity = 9 }], 1);

        var ficha = await new ProductRepository(db.ContextoApp(Uno, sucursal)).GetProduct(producto);

        Assert.Equal(25m, ficha.SalePrice);
        Assert.Equal(20m, ficha.BaseSalePrice);
        Assert.Equal(9, ficha.MinReorderQuantity);
        Assert.Equal(3, ficha.BaseMinReorderQuantity);
    }

    [Fact]
    public async Task Dejar_la_excepcion_vacia_devuelve_la_sucursal_al_valor_base()
    {
        var sucursal = NuevaSucursal("PR Vaciar");
        var producto = NuevoProducto("PR PRODUCTO VACIAR", 8m, 2);
        await Ajustes(sucursal).SaveSettings(producto,
            [new ProductBranchSettingRequest { BranchId = sucursal, SalePrice = 9m }], 1);
        await Ajustes(sucursal).SaveSettings(producto,
            [new ProductBranchSettingRequest { BranchId = sucursal }], 1);

        using var admin = db.AbrirComoAdmin();
        Assert.Equal(0, admin.ExecuteScalar<int>(
            "SELECT count(*) FROM product_branch_settings WHERE product_id = @producto", new { producto }));

        using var cn = db.AbrirComoApp(Uno, sucursal);
        Assert.Equal(8m, cn.ExecuteScalar<decimal>("SELECT fn_precio_venta(@producto)", new { producto }));
    }

    [Fact]
    public async Task La_lista_de_ajustes_muestra_todas_las_sucursales_con_su_stock()
    {
        var sucursal = NuevaSucursal("PR Lista");
        var producto = NuevoProducto("PR PRODUCTO LISTA", 5m, 1);
        using (var cn = db.AbrirComoApp(Uno, sucursal))
            cn.Execute("SELECT fn_mover_stock(@p, 3, 1)", new { p = producto });

        var ajustes = await Ajustes(Principal).GetSettings(producto);

        var fila = ajustes.Single(a => a.BranchId == sucursal);
        Assert.Null(fila.SalePrice);
        Assert.Equal(5m, fila.BaseSalePrice);
        Assert.Equal(3m, fila.Stock);
        Assert.Contains(ajustes, a => a.BranchId == Principal);
    }

    [Fact]
    public void No_se_puede_poner_precio_en_la_sucursal_de_otra_farmacia()
    {
        var producto = NuevoProducto("PR PRODUCTO AJENO", 5m, 1);

        using var cn = db.AbrirComoApp(Uno);
        var ex = Assert.Throws<PostgresException>(() => cn.Execute(@"
            INSERT INTO product_branch_settings (branch_id, product_id, sale_price) VALUES (@b, @p, 1)",
            new { b = db.SucursalDe(db.TenantDos), p = producto }));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ex.SqlState);
    }
}
