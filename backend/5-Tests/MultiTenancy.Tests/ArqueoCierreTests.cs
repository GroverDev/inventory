using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Application;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace MultiTenancy.Tests;

/// <summary>Reglas del arqueo por medio de pago, sin base de datos.</summary>
public class ReglasDeArqueoTests
{
    private static readonly PaymentMethod Efectivo = new() { Id = Guid.NewGuid(), Name = "Efectivo", AffectsCash = true, RequiresCount = true };
    private static readonly PaymentMethod Qr = new() { Id = Guid.NewGuid(), Name = "QR", RequiresCount = true };
    private static readonly PaymentMethod Tarjeta = new() { Id = Guid.NewGuid(), Name = "Tarjeta", RequiresCount = false };
    private static readonly PaymentMethod[] Medios = [Efectivo, Qr, Tarjeta];
    private static readonly PaymentMethodTotal[] Cobrado =
        [new() { PaymentMethodId = Qr.Id, Net = 480 }, new() { PaymentMethodId = Tarjeta.Id, Net = 300 }];

    private static CashCountRequest Declara(PaymentMethod m, decimal monto) => new() { PaymentMethodId = m.Id, Declared = monto };

    [Fact]
    public void Cada_medio_arqueado_se_compara_con_lo_que_entro_por_el()
    {
        var r = CashCountRules.Compute(Medios, Cobrado, cashExpected: 1250,
            [Declara(Efectivo, 1270), Declara(Qr, 460)], legacyCashDeclared: 0);

        Assert.Equal(2, r.Count);   // la tarjeta no se arquea
        Assert.Equal((1250m, 20m), (r.Single(x => x.Name == "Efectivo").Expected, r.Single(x => x.Name == "Efectivo").Difference));
        Assert.Equal((480m, -20m), (r.Single(x => x.Name == "QR").Expected, r.Single(x => x.Name == "QR").Difference));
    }

    [Fact]
    public void No_se_puede_omitir_un_medio_que_se_arquea()
    {
        var ex = Assert.Throws<CustomException>(() =>
            CashCountRules.Compute(Medios, Cobrado, 1250, [Declara(Efectivo, 1250)], 0));
        Assert.Contains("QR", ex.Message);
    }

    [Fact]
    public void Un_cliente_que_solo_manda_el_efectivo_sigue_pudiendo_cerrar()
    {
        // El móvil, hasta que se actualice: declara solo el efectivo.
        var r = CashCountRules.Compute(Medios, Cobrado, 1250, [], legacyCashDeclared: 1240);

        Assert.Equal(-10m, r.Single().Difference);
    }

    [Fact]
    public void El_umbral_se_mide_en_valor_absoluto_por_medio()
    {
        var r = CashCountRules.Compute(Medios, Cobrado, 1250, [Declara(Efectivo, 1245), Declara(Qr, 460)], 0);

        Assert.Equal(["QR"], CashCountRules.OverThreshold(r, threshold: 10));
    }

    private static CashCountResponse Diferencia(decimal d) => new() { Name = "Efectivo", Difference = d };

    [Theory]
    [InlineData(5, 0, false, false)]      // dentro del umbral: nada
    [InlineData(-20, 0, true, false)]     // sobre la observación
    [InlineData(-60, 0, true, true)]      // sobre el tope del supervisor
    [InlineData(0, 3, false, true)]       // intentos agotados, aunque ahora cuadre
    public void Que_exige_el_cierre(decimal diferencia, int intentos, bool nota, bool supervisor)
    {
        var reglas = new CashCloseSettings { NoteThreshold = 10, SupervisorThreshold = 50, MaxAttempts = 3 };
        var r = CashCountRules.Requirement([Diferencia(diferencia)], reglas, intentos);
        Assert.Equal((nota, supervisor), (r.NeedsNote, r.NeedsSupervisor));
    }

    [Fact]
    public void Sin_topes_configurados_nunca_pide_supervisor()
    {
        var r = CashCountRules.Requirement([Diferencia(-9999)], new CashCloseSettings { NoteThreshold = 10 }, priorAttempts: 50);
        Assert.True(r.NeedsNote);
        Assert.False(r.NeedsSupervisor);
    }

    [Fact]
    public void Los_billetes_tienen_que_sumar_el_efectivo_declarado()
    {
        DenominationCount[] conteo = [new() { Value = 100, Quantity = 2 }, new() { Value = 0.5m, Quantity = 3 }, new() { Value = 20, Quantity = 0 }];

        var filas = CashCountRules.ValidateDenominations(conteo, 201.5m, required: true);
        Assert.Equal([100m, 0.5m], filas.Select(f => f.Value));   // las de cantidad cero no se guardan

        var ex = Assert.Throws<CustomException>(() => CashCountRules.ValidateDenominations(conteo, 210m, required: false));
        Assert.Contains("201.50", ex.Message.Replace(",", "."));
    }

    [Fact]
    public void El_conteo_por_billetes_obligatorio_no_se_puede_omitir()
    {
        Assert.Throws<CustomException>(() => CashCountRules.ValidateDenominations([], 100m, required: true));
        Assert.Empty(CashCountRules.ValidateDenominations([], 100m, required: false));
        Assert.Throws<CustomException>(() => CashCountRules.ValidateDenominations(
            [new() { Value = 10, Quantity = 1 }, new() { Value = 10, Quantity = 2 }], 30m, required: false));
    }
}

/// <summary>Cierre real con arqueo por medio.</summary>
[Collection("tenant-db")]
public class CierreConArqueoTests(TenantDatabaseFixture db)
{
    private const int Uno = TenantDatabaseFixture.TenantUno;

    [Fact]
    public async Task Una_diferencia_grande_sin_observacion_no_cierra_y_queda_contada()
    {
        Guid sucursal, caja, qr;
        int cajero;
        using (var admin = db.AbrirComoAdmin())
        {
            sucursal = admin.ExecuteScalar<Guid>(
                "INSERT INTO branches (tenant_id, name) VALUES (@t, 'AQ Cierre') RETURNING id", new { t = Uno });
            qr = admin.ExecuteScalar<Guid>("SELECT id FROM payment_methods WHERE tenant_id = @t AND name = 'QR'", new { t = Uno });
            cajero = admin.ExecuteScalar<int>("SELECT set_sequences_key('sec.users')");
            admin.Execute(@"
                INSERT INTO sec.users (id, user_name, password, email, full_name, change_password, is_active,
                                       created_by, modified_by, uuid, tenant_id)
                VALUES (@cajero, 'aq-cajero@test.local', 'x', 'aq-cajero@test.local', 'Cajero AQ', false, true, 1, 1, gen_random_uuid(), @t)",
                new { cajero, t = Uno });
            admin.Execute("UPDATE payment_methods SET requires_count = true WHERE id = @qr", new { qr });
        }

        try
        {
            using (var cn = db.AbrirComoApp(Uno, sucursal))
            {
                caja = cn.ExecuteScalar<Guid>(
                    "INSERT INTO cash_sessions (user_id, opening_amount, created_by, modified_by) VALUES (@cajero, 100, @cajero, @cajero) RETURNING id",
                    new { cajero });
                // Una venta de 80 por QR en el turno.
                var venta = cn.ExecuteScalar<Guid>(@"
                    INSERT INTO sales (id, customer_id, sale_date, is_active, state, created_by, modified_by, subtotal, total_discounts, total, cash_session_id)
                    SELECT gen_random_uuid(), c.id, now(), true, true, @cajero, @cajero, 80, 0, 80, @caja
                      FROM customers c WHERE c.is_generic LIMIT 1 RETURNING id", new { cajero, caja });
                cn.Execute(@"INSERT INTO sale_payments (id, sale_id, payment_method_id, amount_given, ""amount returned"")
                             VALUES (gen_random_uuid(), @venta, @qr, 80, 0)", new { venta, qr });
            }

            var ctx = db.ContextoApp(Uno, sucursal);
            var app = new CashSessionApplication(
                new CashSessionRepository(ctx, new CashMovementRepository(ctx)),
                new CashMovementRepository(ctx),
                new PaymentMethodRepository(ctx));

            var medios = await new PaymentMethodRepository(ctx).GetPaymentMethods();
            var efectivo = medios.Single(m => m.AffectsCash).Id;

            // Declara 100 en efectivo (cuadra) y 50 de QR cuando entraron 80: −30.
            var pedido = new CloseCashSessionRequest
            {
                Counts = [new() { PaymentMethodId = efectivo, Declared = 100 }, new() { PaymentMethodId = qr, Declared = 50 }]
            };

            var rechazo = await app.CloseSession(caja.ToString(), pedido, cajero);
            Assert.False(rechazo.ok);
            Assert.Contains("QR", rechazo.Message.Description);

            pedido.Notes = "Un cliente mostró un comprobante que no llegó a la cuenta";
            var cierre = await app.CloseSession(caja.ToString(), pedido, cajero);
            Assert.True(cierre.ok, cierre.Message?.Description);

            var turno = cierre.Data!;
            Assert.Equal(1, turno.CloseAttempts);
            Assert.Equal(-30m, turno.Counts.Single(c => c.PaymentMethodId == qr).Difference);
            Assert.Equal(0m, turno.Counts.Single(c => c.PaymentMethodId == efectivo).Difference);
            Assert.Equal(100m, turno.DeclaredAmount);   // las columnas del turno siguen siendo el efectivo
        }
        finally
        {
            using var admin = db.AbrirComoAdmin();
            admin.Execute("UPDATE payment_methods SET requires_count = false WHERE id = @qr", new { qr });
        }
    }

    /// <summary>
    /// Arma un cajero, una sucursal y un turno con 100 de fondo, fija la
    /// configuración del cierre, corre la prueba y deja la configuración como estaba.
    /// </summary>
    private async Task ConCierreConfigurado(string sucursalNombre, string configuracion,
                                            Func<CashSessionApplication, Guid, Guid, int, Task> prueba)
    {
        Guid sucursal;
        int cajero;
        CashCloseSettings? antes;
        using (var admin = db.AbrirComoAdmin())
        {
            sucursal = admin.ExecuteScalar<Guid>(
                "INSERT INTO branches (tenant_id, name) VALUES (@t, @n) RETURNING id", new { t = Uno, n = sucursalNombre });
            cajero = admin.ExecuteScalar<int>("SELECT set_sequences_key('sec.users')");
            admin.Execute(@"
                INSERT INTO sec.users (id, user_name, password, email, full_name, change_password, is_active,
                                       created_by, modified_by, uuid, tenant_id)
                VALUES (@cajero, @u, 'x', @u, 'Cajero', false, true, 1, 1, gen_random_uuid(), @t)",
                new { cajero, u = $"cajero-{cajero}@test.local", t = Uno });
            antes = admin.QueryFirstOrDefault<CashCloseSettings>(@"
                SELECT note_threshold, supervisor_threshold, max_attempts, require_denominations
                  FROM cash_close_settings WHERE tenant_id = @t", new { t = Uno });
            admin.Execute($@"
                INSERT INTO cash_close_settings (tenant_id, note_threshold, supervisor_threshold, max_attempts, require_denominations)
                VALUES (@t, {configuracion})
                ON CONFLICT (tenant_id) DO UPDATE SET
                       note_threshold = EXCLUDED.note_threshold, supervisor_threshold = EXCLUDED.supervisor_threshold,
                       max_attempts = EXCLUDED.max_attempts, require_denominations = EXCLUDED.require_denominations",
                new { t = Uno });
        }

        try
        {
            Guid caja;
            using (var cn = db.AbrirComoApp(Uno, sucursal))
                caja = cn.ExecuteScalar<Guid>(
                    "INSERT INTO cash_sessions (user_id, opening_amount, created_by, modified_by) VALUES (@cajero, 100, @cajero, @cajero) RETURNING id",
                    new { cajero });

            var ctx = db.ContextoApp(Uno, sucursal);
            var app = new CashSessionApplication(
                new CashSessionRepository(ctx, new CashMovementRepository(ctx)),
                new CashMovementRepository(ctx),
                new PaymentMethodRepository(ctx));
            var efectivo = (await new PaymentMethodRepository(ctx).GetPaymentMethods()).Single(m => m.AffectsCash).Id;

            await prueba(app, caja, efectivo, cajero);
        }
        finally
        {
            using var admin = db.AbrirComoAdmin();
            if (antes is null)
                admin.Execute("DELETE FROM cash_close_settings WHERE tenant_id = @t", new { t = Uno });
            else
                admin.Execute(@"
                    UPDATE cash_close_settings
                       SET note_threshold = @NoteThreshold, supervisor_threshold = @SupervisorThreshold,
                           max_attempts = @MaxAttempts, require_denominations = @RequireDenominations
                     WHERE tenant_id = @t",
                    new { t = Uno, antes.NoteThreshold, antes.SupervisorThreshold, antes.MaxAttempts, antes.RequireDenominations });
        }
    }

    [Fact]
    public Task Una_diferencia_sobre_el_tope_necesita_supervisor_y_el_conteo_por_billetes() =>
        ConCierreConfigurado("AQ Supervisor", "10, 50, NULL, true", async (app, caja, efectivo, cajero) =>
        {
            // Esperado 100 (el fondo). Declara 30: faltan 70, sobre el tope de 50.
            var pedido = new CloseCashSessionRequest { Counts = [new() { PaymentMethodId = efectivo, Declared = 30 }] };

            var sinBilletes = await app.CloseSession(caja.ToString(), pedido, cajero);
            Assert.False(sinBilletes.ok);
            Assert.Contains("billetes", sinBilletes.Message.Description);

            pedido.Denominations = [new() { Value = 20, Quantity = 1 }, new() { Value = 10, Quantity = 1 }];
            var sinNota = await app.CloseSession(caja.ToString(), pedido, cajero);
            Assert.False(sinNota.ok);
            Assert.Equal("supervisor", sinNota.Data!.CloseRequires);

            pedido.Notes = "Se pagó a un proveedor sin registrar el gasto";
            var sinFirma = await app.CloseSession(caja.ToString(), pedido, cajero);
            Assert.False(sinFirma.ok);
            Assert.Equal("supervisor", sinFirma.Data!.CloseRequires);

            var cierre = await app.CloseSession(caja.ToString(), pedido, cajero, supervisorId: 1);
            Assert.True(cierre.ok, cierre.Message?.Description);
            Assert.Equal(1, cierre.Data!.CloseAuthorizedBy);
            Assert.Equal(1, cierre.Data.CloseAttempts);   // solo el rechazo sin observación cuenta
            Assert.Equal([20m, 10m], cierre.Data.Denominations.Select(d => d.Value));
        });

    [Fact]
    public Task Agotados_los_intentos_hace_falta_supervisor_aunque_la_diferencia_sea_chica() =>
        ConCierreConfigurado("AQ Intentos", "10, NULL, 2, false", async (app, caja, efectivo, cajero) =>
        {
            // Faltan 20: pide observación, no supervisor.
            var pedido = new CloseCashSessionRequest { Counts = [new() { PaymentMethodId = efectivo, Declared = 80 }] };

            var primero = await app.CloseSession(caja.ToString(), pedido, cajero);
            Assert.Equal("note", primero.Data!.CloseRequires);

            // El segundo rechazo agota los intentos: desde acá, supervisor.
            var segundo = await app.CloseSession(caja.ToString(), pedido, cajero);
            Assert.Equal("supervisor", segundo.Data!.CloseRequires);

            // Aunque ahora cuadre, ya no alcanza con volver a contar.
            pedido.Counts[0].Declared = 100;
            var cuadra = await app.CloseSession(caja.ToString(), pedido, cajero);
            Assert.False(cuadra.ok);
            Assert.Equal("supervisor", cuadra.Data!.CloseRequires);

            // Quien no es solo cajero se autoriza a sí mismo.
            var cierre = await app.CloseSession(caja.ToString(), pedido, cajero, closerIsSupervisor: true);
            Assert.True(cierre.ok, cierre.Message?.Description);
            Assert.Equal(cajero, cierre.Data!.CloseAuthorizedBy);
            Assert.Equal(2, cierre.Data.CloseAttempts);
        });
}
