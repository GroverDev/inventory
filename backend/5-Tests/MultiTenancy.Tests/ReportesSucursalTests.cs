using Dapper;
using Inventory.Infrastructure;

namespace MultiTenancy.Tests;

/// <summary>
/// Sucursales, paso 6: los reportes leen la sucursal activa, otra, o el
/// consolidado, según el alcance que reciben.
/// </summary>
/// <remarks>
/// Acá se prueba el alcance en los repositorios. Que el usuario solo pueda pedir
/// sucursales en las que está habilitado lo decide BranchScope en la API, antes
/// de llegar acá.
/// </remarks>
[Collection("tenant-db")]
public class ReportesSucursalTests(TenantDatabaseFixture db)
{
    private const int Uno = TenantDatabaseFixture.TenantUno;

    private Guid Principal => db.SucursalDe(Uno);

    private Guid NuevaSucursal(string nombre)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>(
            "INSERT INTO branches (tenant_id, name) VALUES (@t, @n) RETURNING id", new { t = Uno, n = nombre });
    }

    private Guid NuevoProducto(string nombre)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>(@"
            INSERT INTO products
                (id, product_name, description, sale_price, available_in_pos,
                 laboratory_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id, current_stock)
            SELECT gen_random_uuid(), @nombre, '', 10, true,
                   (SELECT id FROM laboratories        WHERE tenant_id = @t LIMIT 1),
                   (SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1),
                   true, true, 1, now(), 1, now(), @t, 0
            RETURNING id", new { nombre, t = Uno });
    }

    private Guid VenderEn(Guid sucursal, decimal total)
    {
        using var cn = db.AbrirComoApp(Uno, sucursal);
        return cn.ExecuteScalar<Guid>(@"
            INSERT INTO sales (id, customer_id, sale_date, is_active, state, created_by, modified_by, subtotal, total_discounts, total)
            SELECT gen_random_uuid(), c.id, now(), true, true, 1, 1, @total, 0, @total
              FROM customers c WHERE c.is_generic LIMIT 1
            RETURNING id", new { total });
    }

    private SalesRepository Ventas(Guid sucursal)
    {
        var ctx = db.ContextoApp(Uno, sucursal);
        return new SalesRepository(ctx, new SalesDetailRepository(), new SalePaymentRepository(), new SaleReturnRepository(ctx));
    }

    [Fact]
    public async Task Las_ventas_se_leen_de_la_activa_de_otra_o_consolidadas()
    {
        var sur = NuevaSucursal("RP Ventas Sur");
        var enPrincipal = VenderEn(Principal, 100);
        var enSur = VenderEn(sur, 40);
        var desde = DateTime.UtcNow.AddHours(-1);
        var hasta = DateTime.UtcNow.AddHours(1);

        async Task<List<Guid>> Ids(Guid[]? alcance) =>
            (await Ventas(Principal).GetSales(desde, hasta, null, 1, 10000, null, alcance))
                .Items.Select(v => v.Id).ToList();

        var activa = await Ids(null);
        Assert.Contains(enPrincipal, activa);
        Assert.DoesNotContain(enSur, activa);

        var soloSur = await Ids([sur]);
        Assert.Equal([enSur], soloSur);

        var consolidado = await Ventas(Principal).GetSales(desde, hasta, null, 1, 10000, null, [Principal, sur]);
        Assert.Contains(consolidado.Items, v => v.Id == enPrincipal && v.BranchName != "");
        Assert.Contains(consolidado.Items, v => v.Id == enSur && v.BranchName == "RP Ventas Sur");
    }

    [Fact]
    public async Task El_stock_del_reporte_suma_las_sucursales_pedidas()
    {
        var sur = NuevaSucursal("RP Stock Sur");
        var producto = NuevoProducto("RP STOCK CONSOLIDADO");
        using (var cn = db.AbrirComoApp(Uno, Principal)) cn.Execute("SELECT fn_mover_stock(@p, 7, 1)", new { p = producto });
        using (var cn = db.AbrirComoApp(Uno, sur)) cn.Execute("SELECT fn_mover_stock(@p, 3, 1)", new { p = producto });

        var repo = new ProductRepository(db.ContextoApp(Uno, Principal));
        async Task<decimal> Stock(Guid[]? alcance) =>
            (await repo.GetProducts("RP STOCK CONSOLIDADO", alcance)).Single().CurrentStock;

        Assert.Equal(7, await Stock(null));
        Assert.Equal(3, await Stock([sur]));
        Assert.Equal(10, await Stock([Principal, sur]));
    }

    [Fact]
    public async Task Las_mermas_se_leen_por_sucursal_o_consolidadas()
    {
        var sur = NuevaSucursal("RP Mermas Sur");
        var producto = NuevoProducto("RP MERMA CONSOLIDADA");

        foreach (var (sucursal, cantidad) in new[] { (Principal, 2), (sur, 5) })
        {
            using var cn = db.AbrirComoApp(Uno, sucursal);
            var item = cn.QueryFirst<Guid>("SELECT stock_item_id FROM fn_mover_stock(@p, 10, 1)", new { p = producto });
            cn.Execute(@"
                INSERT INTO stock_movements (product_id, stock_item_id, movement_type, quantity, stock_before, stock_after, reason)
                VALUES (@p, @item, 'MERMA', -@cantidad, 10, 10, 'VENCIMIENTO')",
                new { p = producto, item, cantidad });
        }

        var repo = new StockMovementRepository(db.ContextoApp(Uno, Principal));
        var desde = DateTime.UtcNow.AddDays(-1);
        var hasta = DateTime.UtcNow.AddDays(1);

        async Task<decimal> Unidades(Guid[]? alcance) =>
            (await repo.GetWriteOffs(desde, hasta, producto, alcance)).Detalle.Sum(d => d.Cantidad);

        Assert.Equal(2, await Unidades(null));
        Assert.Equal(5, await Unidades([sur]));
        Assert.Equal(7, await Unidades([Principal, sur]));

        var consolidado = await repo.GetWriteOffs(desde, hasta, producto, [Principal, sur]);
        Assert.Contains(consolidado.Detalle, d => d.BranchName == "RP Mermas Sur");
    }
}
