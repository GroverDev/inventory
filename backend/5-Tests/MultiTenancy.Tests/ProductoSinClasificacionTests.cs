using Dapper;
using Mapster;

namespace MultiTenancy.Tests;

/// <summary>
/// El laboratorio y la categoría son opcionales en el producto.
/// </summary>
/// <remarks>
/// El laboratorio era NOT NULL y las consultas hacían INNER JOIN contra
/// laboratories, así que el riesgo no era solo no poder crear el producto: uno
/// sin laboratorio habría DESAPARECIDO de las listas sin error ni aviso.
///
/// La categoría ya era opcional en la base, pero el mapeo convertía el vacío en
/// Guid.Empty y la FK lo rechazaba, con el error disfrazado de fallo de
/// conexión. Son dos formas distintas de romperse por lo mismo: tratar como
/// obligatorio algo que no lo es.
/// </remarks>
[Collection("tenant-db")]
public class ProductoSinClasificacionTests(TenantDatabaseFixture db)
{
    private Guid CrearProductoSinClasificacion(Npgsql.NpgsqlConnection cn, string nombre)
    {
        var id = cn.ExecuteScalar<Guid>(@"
            INSERT INTO products
                (id, product_name, description, sale_price, available_in_pos,
                 laboratory_id, category_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id, current_stock)
            SELECT gen_random_uuid(), @nombre, '', 10, true,
                   NULL, NULL,
                   (SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1),
                   true, true, 1, now(), 1, now(), @t, 0
            RETURNING id",
            new { nombre, t = TenantDatabaseFixture.TenantUno });

        // Su existencia implícita, igual que cualquier otro producto en modo
        // 'none'. Sin ella el producto queda fuera del invariante del modelo, y
        // las pruebas que eligen "un producto cualquiera" fallan al toparse con
        // él: el problema no sería del producto sin laboratorio, sino del dato
        // de prueba mal armado.
        cn.Execute("INSERT INTO stock_items (tenant_id, product_id, quantity) VALUES (@t, @p, 0)",
            new { t = TenantDatabaseFixture.TenantUno, p = id });

        return id;
    }

    /// <summary>
    /// El mapeo tiene que producir NULL y no <c>Guid.Empty</c>. Con Guid.Empty
    /// el INSERT choca contra la FK, y ese error llega al usuario disfrazado de
    /// "No se puede conectar a la Base de Datos".
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_producto_sin_laboratorio_ni_categoria_se_mapea_con_nulos(string vacio)
    {
        var config = new Mapster.TypeAdapterConfig();
        new Inventory.Application.Mappers.InventoryMappingConfig().Register(config);

        var producto = new Inventory.Domain.ProductRequest
        {
            ProductName = "X",
            Description = "X",
            LaboratoryId = vacio,
            CategoryId = vacio,
            UomId = Guid.NewGuid().ToString(),
        }.Adapt<Inventory.Domain.Product>(config);

        Assert.Null(producto.LaboratoryId);
        Assert.Null(producto.CategoryId);
    }

    [Fact]
    public void Un_Guid_vacio_seria_rechazado_por_la_base()
    {
        // Deja constancia de por qué el mapeo no puede caer en Guid.Empty: no es
        // una preferencia de estilo, la FK lo rechaza.
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var ex = Assert.Throws<Npgsql.PostgresException>(() => cn.Execute(@"
            INSERT INTO products
                (id, product_name, description, sale_price, available_in_pos,
                 laboratory_id, category_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id, current_stock)
            SELECT gen_random_uuid(), 'TEST GUID VACIO', '', 10, true,
                   NULL, '00000000-0000-0000-0000-000000000000',
                   (SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1),
                   true, true, 1, now(), 1, now(), @t, 0",
            new { t = TenantDatabaseFixture.TenantUno }));

        Assert.Equal("23503", ex.SqlState);   // foreign_key_violation
    }

    [Fact]
    public void La_columna_admite_nulos()
    {
        using var cn = db.AbrirComoAdmin();

        var admiteNulos = cn.ExecuteScalar<bool>(@"
            SELECT is_nullable = 'YES'
              FROM information_schema.columns
             WHERE table_schema = 'public'
               AND table_name = 'products'
               AND column_name = 'laboratory_id'");

        Assert.True(admiteNulos,
            "products.laboratory_id volvió a ser NOT NULL: la migración se perdió.");
    }

    [Fact]
    public async Task Un_producto_sin_laboratorio_no_desaparece_de_las_consultas()
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var nombre = "TEST SIN LABORATORIO";
        var id = CrearProductoSinClasificacion(cn, nombre);

        var repo = new Inventory.Infrastructure.ProductRepository(
            db.ContextoApp(TenantDatabaseFixture.TenantUno));

        // La ficha individual.
        var ficha = await repo.GetProduct(id);
        Assert.Equal(nombre, ficha.ProductName);
        Assert.Null(ficha.LaboratoryId);
        Assert.Null(ficha.CategoryId);

        // La búsqueda por nombre, que es la que alimenta las listas.
        var lista = await repo.GetProducts(nombre);
        Assert.Contains(lista, p => p.Id == id);

        // Y la consulta paginada de control de stock.
        var (items, _) = await repo.GetProductsStock(nombre, 1, 50);
        Assert.Contains(items, p => p.Id == id);
    }
}
