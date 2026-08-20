using Dapper;

namespace MultiTenancy.Tests;

/// <summary>
/// Dar de baja una existencia vencida (MovementType="MERMA") y el reporte
/// agregado (v_mermas) que muestra cuánta plata se perdió.
/// </summary>
[Collection("tenant-db")]
public class MermasTests(TenantDatabaseFixture db)
{
    private Guid CrearProductoConLotes(Npgsql.NpgsqlConnection cn, string nombre, decimal salePrice = 10m)
    {
        var id = cn.ExecuteScalar<Guid>(@"
            INSERT INTO products
                (id, product_name, description, sale_price, available_in_pos,
                 laboratory_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id, current_stock)
            SELECT gen_random_uuid(), @nombre, '', @salePrice, true,
                   (SELECT id FROM laboratories        WHERE tenant_id = @t LIMIT 1),
                   (SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1),
                   true, true, 1, now(), 1, now(), @t, 0
            RETURNING id",
            new { nombre, salePrice, t = TenantDatabaseFixture.TenantUno });

        cn.Execute("INSERT INTO stock_items (tenant_id, product_id, quantity) VALUES (@t, @p, 0)",
            new { t = TenantDatabaseFixture.TenantUno, p = id });
        cn.Execute("SELECT fn_activar_lotes(@p)", new { p = id });
        return id;
    }

    [Fact]
    public async Task Dar_de_baja_reduce_la_cantidad_y_aparece_en_v_mermas()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");
        var producto = CrearProductoConLotes(cn, "TEST MERMA VENCIDO", salePrice: 20m);
        cn.Execute("SELECT fn_recibir_lote(@p, 10, 'MERMA-A', '2020-01-01', 1)", new { p = producto });

        var stockItemId = cn.QueryFirst<Guid>(
            "SELECT id FROM stock_items WHERE product_id = @p AND lot_code = 'MERMA-A'", new { p = producto });

        var repo = new Inventory.Infrastructure.StockMovementRepository(db.ContextoApp(TenantDatabaseFixture.TenantUno));
        var movement = new Inventory.Domain.StockMovement
        {
            ProductId = producto,
            Quantity = 4,
            Reason = "Vencimiento",
        };

        await repo.CreateWriteOff(movement, stockItemId, userId: 1);

        var cantidadRestante = cn.ExecuteScalar<decimal>(
            "SELECT quantity FROM stock_items WHERE id = @id", new { id = stockItemId });
        Assert.Equal(6m, cantidadRestante);

        var merma = cn.QueryFirst<(decimal Cantidad, decimal ValorPerdido, string? LotCode)>(
            "SELECT cantidad, valor_perdido, lot_code FROM v_mermas WHERE product_id = @p", new { p = producto });
        Assert.Equal(4m, merma.Cantidad);
        Assert.Equal(80m, merma.ValorPerdido); // 4 unidades * 20 de precio de venta
        Assert.Equal("MERMA-A", merma.LotCode);
    }

    [Fact]
    public async Task No_se_puede_dar_de_baja_mas_de_lo_que_hay_en_esa_existencia()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");
        var producto = CrearProductoConLotes(cn, "TEST MERMA EXCESO");
        cn.Execute("SELECT fn_recibir_lote(@p, 5, 'MERMA-B', '2020-01-01', 1)", new { p = producto });

        var stockItemId = cn.QueryFirst<Guid>(
            "SELECT id FROM stock_items WHERE product_id = @p AND lot_code = 'MERMA-B'", new { p = producto });

        var repo = new Inventory.Infrastructure.StockMovementRepository(db.ContextoApp(TenantDatabaseFixture.TenantUno));
        var movement = new Inventory.Domain.StockMovement { ProductId = producto, Quantity = 6, Reason = "Vencimiento" };

        await Assert.ThrowsAsync<Common.Utilities.Exceptions.CustomException>(
            () => repo.CreateWriteOff(movement, stockItemId, userId: 1));

        // Nada se movió: la existencia sigue en 5.
        var cantidad = cn.ExecuteScalar<decimal>(
            "SELECT quantity FROM stock_items WHERE id = @id", new { id = stockItemId });
        Assert.Equal(5m, cantidad);
    }

    [Fact]
    public async Task No_se_puede_dar_de_baja_una_existencia_de_otro_producto()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");
        var productoA = CrearProductoConLotes(cn, "TEST MERMA PRODUCTO A");
        var productoB = CrearProductoConLotes(cn, "TEST MERMA PRODUCTO B");
        cn.Execute("SELECT fn_recibir_lote(@p, 5, 'MERMA-C', '2020-01-01', 1)", new { p = productoB });

        var stockItemDeB = cn.QueryFirst<Guid>(
            "SELECT id FROM stock_items WHERE product_id = @p AND lot_code = 'MERMA-C'", new { p = productoB });

        var repo = new Inventory.Infrastructure.StockMovementRepository(db.ContextoApp(TenantDatabaseFixture.TenantUno));
        // Se indica el producto A, pero la existencia es de B.
        var movement = new Inventory.Domain.StockMovement { ProductId = productoA, Quantity = 1, Reason = "Vencimiento" };

        await Assert.ThrowsAsync<Common.Utilities.Exceptions.CustomException>(
            () => repo.CreateWriteOff(movement, stockItemDeB, userId: 1));
    }

    [Fact]
    public void V_mermas_respeta_el_aislamiento_por_tenant()
    {
        using var admin = db.AbrirComoAdmin();
        admin.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");
        var producto = CrearProductoConLotes(admin, "TEST MERMA AISLADA");
        admin.Execute("SELECT fn_recibir_lote(@p, 5, 'MERMA-D', '2020-01-01', 1)", new { p = producto });
        var stockItemId = admin.QueryFirst<Guid>(
            "SELECT id FROM stock_items WHERE product_id = @p AND lot_code = 'MERMA-D'", new { p = producto });

        admin.Execute(@"
            INSERT INTO stock_movements
                (id, tenant_id, product_id, stock_item_id, movement_type, quantity, stock_before, stock_after,
                 reason, state, created_by, created, modified_by, modified)
            VALUES (gen_random_uuid(), @t, @p, @item, 'MERMA', -1, 5, 4,
                    'Vencimiento', true, 1, now(), 1, now())",
            new { t = TenantDatabaseFixture.TenantUno, p = producto, item = stockItemId });

        using var otra = db.AbrirComoApp(db.TenantDos);
        Assert.Equal(0, otra.ExecuteScalar<int>(
            "SELECT count(*) FROM v_mermas WHERE lot_code = 'MERMA-D'"));
    }
}
