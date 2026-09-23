using Dapper;
using Inventory.Infrastructure;

namespace MultiTenancy.Tests;

/// <summary>
/// Neto por medio de pago: cobrado (entregado − vuelto) menos lo reintegrado
/// por devoluciones con ese medio.
/// </summary>
[Collection("tenant-db")]
public class DesgloseMediosDePagoTests(TenantDatabaseFixture db)
{
    private const int Uno = TenantDatabaseFixture.TenantUno;

    private (Guid Sucursal, Guid Caja, Guid Efectivo, Guid Qr) Escenario(string nombre)
    {
        using var admin = db.AbrirComoAdmin();
        var sucursal = admin.ExecuteScalar<Guid>(
            "INSERT INTO branches (tenant_id, name) VALUES (@t, @n) RETURNING id", new { t = Uno, n = nombre });
        var efectivo = admin.ExecuteScalar<Guid>("SELECT id FROM payment_methods WHERE tenant_id = @t AND name = 'Efectivo'", new { t = Uno });
        var qr = admin.ExecuteScalar<Guid>("SELECT id FROM payment_methods WHERE tenant_id = @t AND name = 'QR'", new { t = Uno });

        using var cn = db.AbrirComoApp(Uno, sucursal);
        var caja = cn.ExecuteScalar<Guid>(@"
            INSERT INTO cash_sessions (user_id, created_by, modified_by, closed_at)
            SELECT id, id, id, now() FROM sec.users WHERE tenant_id = @t AND is_active LIMIT 1
            RETURNING id", new { t = Uno });
        return (sucursal, caja, efectivo, qr);
    }

    private Guid Vender(Guid sucursal, Guid caja, decimal total, Guid metodo, decimal entregado, decimal vuelto)
    {
        using var cn = db.AbrirComoApp(Uno, sucursal);
        var venta = cn.ExecuteScalar<Guid>(@"
            INSERT INTO sales (id, customer_id, sale_date, is_active, state, created_by, modified_by,
                               subtotal, total_discounts, total, cash_session_id)
            SELECT gen_random_uuid(), c.id, now(), true, true, 1, 1, @total, 0, @total, @caja
              FROM customers c WHERE c.is_generic LIMIT 1
            RETURNING id", new { total, caja });
        cn.Execute(@"
            INSERT INTO sale_payments (id, sale_id, payment_method_id, amount_given, ""amount returned"")
            VALUES (gen_random_uuid(), @venta, @metodo, @entregado, @vuelto)",
            new { venta, metodo, entregado, vuelto });
        return venta;
    }

    [Fact]
    public async Task El_periodo_y_el_turno_dan_el_neto_de_cada_medio()
    {
        var (sucursal, caja, efectivo, qr) = Escenario("DMP Desglose");

        Vender(sucursal, caja, 137.50m, efectivo, entregado: 200, vuelto: 62.50m);
        var ventaQr = Vender(sucursal, caja, 50, qr, entregado: 50, vuelto: 0);

        // Devolución de 20 reintegrada por QR: no pasa por la caja, así que no
        // queda asociada al turno.
        using (var cn = db.AbrirComoApp(Uno, sucursal))
            cn.Execute(@"
                INSERT INTO sale_returns (sale_id, total_returned, created_by, modified_by, payment_method_id)
                VALUES (@ventaQr, 20, 1, 1, @qr)", new { ventaQr, qr });

        // Período
        var ctx = db.ContextoApp(Uno, sucursal);
        var ventas = new SalesRepository(ctx, new SalesDetailRepository(), new SalePaymentRepository(), new SaleReturnRepository(ctx));
        var periodo = await ventas.GetSales(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1), branches: [sucursal]);

        var efe = periodo.PeriodByPaymentMethod.Single(m => m.PaymentMethodId == efectivo);
        var q = periodo.PeriodByPaymentMethod.Single(m => m.PaymentMethodId == qr);
        Assert.Equal(137.50m, efe.Net);               // cobrado, no lo entregado
        Assert.Equal((50m, 20m, 30m), (q.Collected, q.Refunded, q.Net));
        Assert.Equal(periodo.PeriodNet, periodo.PeriodByPaymentMethod.Sum(m => m.Net));

        // Cada venta dice con qué se pagó.
        Assert.Contains(periodo.Items, v => v.Id == ventaQr && v.PaymentMethodsLabel == "QR");

        // Turno: la devolución por QR no es del turno.
        var turno = await new CashSessionRepository(ctx, new CashMovementRepository(ctx)).GetSessionById(caja);
        Assert.Equal(137.50m, turno!.ByPaymentMethod.Single(m => m.PaymentMethodId == efectivo).Net);
        Assert.Equal(50m, turno.ByPaymentMethod.Single(m => m.PaymentMethodId == qr).Net);
    }
}
