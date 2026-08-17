using Dapper;
using Npgsql;

namespace MultiTenancy.Tests;

/// <summary>
/// Rubro farmacia: composición del producto y equivalentes.
/// </summary>
/// <remarks>
/// La composición se modeló como relación y no como texto justamente para poder
/// deducir equivalentes. Estas pruebas verifican esa promesa: si alguien
/// "simplifica" la composición a un campo de texto, dejan de pasar.
/// </remarks>
[Collection("tenant-db")]
public class RubroFarmaciaTests(TenantDatabaseFixture db)
{
    private Guid CrearProducto(NpgsqlConnection cn, string nombre, decimal precio)
    {
        var id = cn.ExecuteScalar<Guid>(@"
            INSERT INTO products
                (id, product_name, description, sale_price, available_in_pos,
                 laboratory_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id, current_stock)
            SELECT gen_random_uuid(), @nombre, '', @precio, true, NULL,
                   (SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1),
                   true, true, 1, now(), 1, now(), @t, 10
            RETURNING id",
            new { nombre, precio, t = TenantDatabaseFixture.TenantUno });

        cn.Execute("INSERT INTO stock_items (tenant_id, product_id, quantity) VALUES (@t, @p, 10)",
            new { t = TenantDatabaseFixture.TenantUno, p = id });
        return id;
    }

    private static List<(Guid?, string, decimal?, string?, bool)> Componentes(
        params (string Nombre, decimal Valor, string Unidad)[] items) =>
        [.. items.Select(i => ((Guid?)null, i.Nombre, (decimal?)i.Valor, (string?)i.Unidad, true))];

    [Fact]
    public async Task La_sustancia_se_da_de_alta_al_vuelo_y_no_se_duplica()
    {
        // Sin esto, cargar un producto obligaría a salir a otra pantalla a crear
        // el principio activo. Con 1.200 productos por cargar, esa fricción es
        // la diferencia entre que el catálogo se llene y que quede vacío.
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var repo = new Inventory.Infrastructure.PharmaRepository(
            db.ContextoApp(TenantDatabaseFixture.TenantUno));

        var uno = CrearProducto(cn, "TEST IBU MARCA A", 12m);
        var dos = CrearProducto(cn, "TEST IBU MARCA B", 9m);

        await repo.Save(uno, new Inventory.Domain.ProductPharma { ProductId = uno },
            Componentes(("TEST-IBUPROFENO", 400, "mg")), 1);

        // El segundo producto usa la MISMA sustancia, escrita igual: tiene que
        // reutilizarla, no crear otra. Si se duplicara, la búsqueda de
        // equivalentes se partiría en dos mitades que no se ven entre sí.
        await repo.Save(dos, new Inventory.Domain.ProductPharma { ProductId = dos },
            Componentes(("test-ibuprofeno", 400, "mg")), 1);

        Assert.Equal(1, cn.ExecuteScalar<int>(
            "SELECT count(*) FROM pharma_substances WHERE upper(substance_name) = 'TEST-IBUPROFENO'"));
    }

    [Fact]
    public async Task Los_equivalentes_se_deducen_de_la_composicion()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var repo = new Inventory.Infrastructure.PharmaRepository(
            db.ContextoApp(TenantDatabaseFixture.TenantUno));

        var caro   = CrearProducto(cn, "TEST EQUIV CARO", 20m);
        var barato = CrearProducto(cn, "TEST EQUIV BARATO", 8m);
        var otro   = CrearProducto(cn, "TEST EQUIV DISTINTO", 15m);

        await repo.Save(caro,   new Inventory.Domain.ProductPharma { ProductId = caro },
            Componentes(("TEST-EQ-SUSTANCIA", 500, "mg")), 1);
        await repo.Save(barato, new Inventory.Domain.ProductPharma { ProductId = barato },
            Componentes(("TEST-EQ-SUSTANCIA", 500, "mg")), 1);
        // Misma sustancia, OTRA concentración: no es equivalente.
        await repo.Save(otro,   new Inventory.Domain.ProductPharma { ProductId = otro },
            Componentes(("TEST-EQ-SUSTANCIA", 250, "mg")), 1);

        var equivalentes = await repo.GetEquivalents(caro);

        Assert.Single(equivalentes);
        Assert.Equal("TEST EQUIV BARATO", equivalentes[0].ProductName);
        // Ordenados por precio: lo primero que se ofrece es lo más económico.
        Assert.Equal(8m, equivalentes[0].SalePrice);
    }

    [Fact]
    public async Task Una_combinacion_solo_equivale_a_la_misma_combinacion()
    {
        // Los antigripales son casi siempre combinaciones, y es donde un campo
        // de texto fallaría: "PARACETAMOL/CLORFENAMINA" no se puede comparar.
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var repo = new Inventory.Infrastructure.PharmaRepository(
            db.ContextoApp(TenantDatabaseFixture.TenantUno));

        var combo1 = CrearProducto(cn, "TEST COMBO A", 18m);
        var combo2 = CrearProducto(cn, "TEST COMBO B", 16m);
        var simple = CrearProducto(cn, "TEST COMBO SIMPLE", 10m);

        await repo.Save(combo1, new Inventory.Domain.ProductPharma { ProductId = combo1 },
            Componentes(("TEST-PARACETAMOL", 500, "mg"), ("TEST-CLORFENAMINA", 4, "mg")), 1);
        await repo.Save(combo2, new Inventory.Domain.ProductPharma { ProductId = combo2 },
            Componentes(("TEST-CLORFENAMINA", 4, "mg"), ("TEST-PARACETAMOL", 500, "mg")), 1);
        // Solo uno de los dos principios: no equivale a la combinación.
        await repo.Save(simple, new Inventory.Domain.ProductPharma { ProductId = simple },
            Componentes(("TEST-PARACETAMOL", 500, "mg")), 1);

        var equivalentes = await repo.GetEquivalents(combo1);

        // El orden de carga no importa: lo que compara es el conjunto.
        Assert.Single(equivalentes);
        Assert.Equal("TEST COMBO B", equivalentes[0].ProductName);
    }

    [Fact]
    public async Task Los_excipientes_no_cuentan_para_la_equivalencia()
    {
        // Dos marcas del mismo medicamento pueden traer excipientes distintos
        // (una con lactosa, otra sin). Siguen siendo equivalentes.
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var repo = new Inventory.Infrastructure.PharmaRepository(
            db.ContextoApp(TenantDatabaseFixture.TenantUno));

        var conLactosa = CrearProducto(cn, "TEST EXC CON LACTOSA", 14m);
        var sinLactosa = CrearProducto(cn, "TEST EXC SIN LACTOSA", 13m);

        await repo.Save(conLactosa, new Inventory.Domain.ProductPharma { ProductId = conLactosa },
        [
            ((Guid?)null, "TEST-EXC-ACTIVO", (decimal?)100, (string?)"mg", true),
            ((Guid?)null, "TEST-EXC-LACTOSA", null, null, false),
        ], 1);

        await repo.Save(sinLactosa, new Inventory.Domain.ProductPharma { ProductId = sinLactosa },
            Componentes(("TEST-EXC-ACTIVO", 100, "mg")), 1);

        var equivalentes = await repo.GetEquivalents(conLactosa);

        Assert.Single(equivalentes);
        Assert.Equal("TEST EXC SIN LACTOSA", equivalentes[0].ProductName);
    }

    [Fact]
    public async Task Guardar_dos_veces_reemplaza_la_composicion_y_no_la_duplica()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var repo = new Inventory.Infrastructure.PharmaRepository(
            db.ContextoApp(TenantDatabaseFixture.TenantUno));

        var producto = CrearProducto(cn, "TEST REEMPLAZO", 10m);

        await repo.Save(producto, new Inventory.Domain.ProductPharma { ProductId = producto },
            Componentes(("TEST-REEMP-UNO", 100, "mg"), ("TEST-REEMP-DOS", 50, "mg")), 1);

        // Se corrige la carga: queda uno solo.
        await repo.Save(producto, new Inventory.Domain.ProductPharma
        {
            ProductId = producto,
            Presentation = "caja x 20 comprimidos",
        }, Componentes(("TEST-REEMP-UNO", 200, "mg")), 1);

        var ficha = await repo.GetByProduct(producto);

        Assert.NotNull(ficha);
        Assert.Single(ficha!.Components);
        Assert.Equal(200m, ficha.Components[0].ConcentrationValue);
        Assert.Equal("caja x 20 comprimidos", ficha.Presentation);
    }

    /// <summary>
    /// Cuando un producto está en las dos listas —deducido por composición y
    /// cargado a mano— gana la MANUAL, y aparece una sola vez.
    /// </summary>
    /// <remarks>
    /// La regla estuvo al revés y volvía imposible destacar un equivalente:
    /// cargarlo a mano se guardaba en la base pero no se veía en ninguna
    /// pantalla, así que el botón "Sugerir" de la ficha era decorativo. El modo
    /// de falla es silencioso —nada se rompe, la sugerencia simplemente no
    /// aparece—, y por eso se fija acá.
    /// </remarks>
    [Fact]
    public async Task Una_alternativa_cargada_a_mano_gana_sobre_el_equivalente_deducido()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var repo = new Inventory.Infrastructure.PharmaRepository(
            db.ContextoApp(TenantDatabaseFixture.TenantUno));
        var app = new Inventory.Application.PharmaApplication(repo);

        var producto    = CrearProducto(cn, "TEST COLISION BASE", 20m);
        var equivalente = CrearProducto(cn, "TEST COLISION MISMO", 8m);

        await repo.Save(producto, new Inventory.Domain.ProductPharma { ProductId = producto },
            Componentes(("TEST-COL-SUSTANCIA", 500, "mg")), 1);
        await repo.Save(equivalente, new Inventory.Domain.ProductPharma { ProductId = equivalente },
            Componentes(("TEST-COL-SUSTANCIA", 500, "mg")), 1);

        // Antes de tocar nada ya se deduce solo, sin motivo que explicarlo.
        var deducido = Assert.Single((await app.GetEquivalents(producto.ToString())).Data);
        Assert.False(deducido.IsManual);

        await repo.AddAlternative(producto, equivalente, "Más económico", 1);

        // Sigue siendo UNO —no se duplica— pero ahora dice por qué se sugiere.
        var destacado = Assert.Single((await app.GetEquivalents(producto.ToString())).Data);
        Assert.True(destacado.IsManual);
        Assert.Equal("Más económico", destacado.Reason);

        // Y se puede deshacer: sin la manual vuelve a ser un equivalente deducido.
        await repo.RemoveAlternative(producto, equivalente);
        var otraVez = Assert.Single((await app.GetEquivalents(producto.ToString())).Data);
        Assert.False(otraVez.IsManual);
    }

    /// <summary>
    /// Una alternativa que NO comparte composición no se toca: es una sugerencia
    /// comercial y convive con los equivalentes deducidos, sin desplazarlos.
    /// </summary>
    [Fact]
    public async Task La_sugerencia_comercial_convive_con_el_equivalente_deducido()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var repo = new Inventory.Infrastructure.PharmaRepository(
            db.ContextoApp(TenantDatabaseFixture.TenantUno));
        var app = new Inventory.Application.PharmaApplication(repo);

        var producto    = CrearProducto(cn, "TEST CONVIVE BASE", 20m);
        var equivalente = CrearProducto(cn, "TEST CONVIVE MISMO", 9m);
        var comercial   = CrearProducto(cn, "TEST CONVIVE OTRO", 5m);

        await repo.Save(producto, new Inventory.Domain.ProductPharma { ProductId = producto },
            Componentes(("TEST-CONV-SUSTANCIA", 300, "mg")), 1);
        await repo.Save(equivalente, new Inventory.Domain.ProductPharma { ProductId = equivalente },
            Componentes(("TEST-CONV-SUSTANCIA", 300, "mg")), 1);
        // Otro principio activo: no es intercambiable, es una sugerencia.
        await repo.Save(comercial, new Inventory.Domain.ProductPharma { ProductId = comercial },
            Componentes(("TEST-CONV-DISTINTA", 300, "mg")), 1);

        await repo.AddAlternative(producto, comercial, "El cliente lo prefiere", 1);

        var todas = (await app.GetEquivalents(producto.ToString())).Data;

        Assert.Equal(2, todas.Count);
        Assert.Single(todas, e => e.IsManual && e.ProductName == "TEST CONVIVE OTRO");
        Assert.Single(todas, e => !e.IsManual && e.ProductName == "TEST CONVIVE MISMO");
    }

    /// <summary>
    /// En el mostrador manda lo que hay en el estante: primero lo disponible y
    /// recién después lo más barato. Sugerir algo agotado hace perder la venta.
    /// </summary>
    [Fact]
    public async Task Lo_disponible_se_sugiere_antes_que_lo_barato_pero_agotado()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var repo = new Inventory.Infrastructure.PharmaRepository(
            db.ContextoApp(TenantDatabaseFixture.TenantUno));

        var producto = CrearProducto(cn, "TEST ORDEN BASE", 20m);
        var agotado  = CrearProducto(cn, "TEST ORDEN AGOTADO", 3m);
        var hayStock = CrearProducto(cn, "TEST ORDEN CON STOCK", 12m);

        // Por fn_mover_stock y no con UPDATE: escribir el stock a mano deja la
        // caché desalineada del libro mayor, que es justo lo que vigila
        // ExistenciasTests.La_cache_de_stock_coincide_con_el_libro_mayor.
        cn.Execute("SELECT fn_mover_stock(@p, -10, 1)", new { p = agotado });

        await repo.AddAlternative(producto, agotado,  "Más económico", 1);
        await repo.AddAlternative(producto, hayStock, "Hay stock", 1);

        var lista = await repo.GetManualAlternatives(producto);

        // El agotado es más barato y aun así va último.
        Assert.Equal(2, lista.Count);
        Assert.Equal("TEST ORDEN CON STOCK", lista[0].ProductName);
        Assert.Equal("TEST ORDEN AGOTADO", lista[1].ProductName);
    }

    /// <summary>
    /// La relación se guarda en un solo sentido, así que hay que poder verla
    /// desde el otro lado: si no, un producto se ofrece en varias fichas sin que
    /// desde la suya se note.
    /// </summary>
    [Fact]
    public async Task Se_puede_ver_en_que_fichas_se_ofrece_un_producto()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var repo = new Inventory.Infrastructure.PharmaRepository(
            db.ContextoApp(TenantDatabaseFixture.TenantUno));

        var ofrecido = CrearProducto(cn, "TEST INVERSA OFRECIDO", 5m);
        var ficha1   = CrearProducto(cn, "TEST INVERSA FICHA UNO", 20m);
        var ficha2   = CrearProducto(cn, "TEST INVERSA FICHA DOS", 25m);

        await repo.AddAlternative(ficha1, ofrecido, "Más económico", 1);
        await repo.AddAlternative(ficha2, ofrecido, "Cuando no hay stock", 1);

        var donde = await repo.GetSuggestedIn(ofrecido);

        Assert.Equal(2, donde.Count);
        Assert.Contains(donde, d => d.ProductName == "TEST INVERSA FICHA UNO" && d.Reason == "Más económico");
        Assert.Contains(donde, d => d.ProductName == "TEST INVERSA FICHA DOS" && d.Reason == "Cuando no hay stock");

        // Y NO es simétrica: desde la ficha uno, el ofrecido no la sugiere a ella.
        Assert.Empty(await repo.GetSuggestedIn(ficha1));
    }
}
