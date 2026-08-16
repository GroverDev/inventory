using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Npgsql;

namespace MultiTenancy.Tests;

/// <summary>
/// Los errores de PostgreSQL tienen que llegar al usuario diciendo qué pasó.
/// </summary>
/// <remarks>
/// Antes, cualquier <c>PostgresException</c> se convertía en "No se puede
/// conectar a la Base de Datos", que es lo contrario de lo que significa: si
/// PostgreSQL respondió con un error, la conexión existía. Eso escondió tres
/// bugs distintos en una sola jornada —una función con tipos que no resolvían,
/// una FK violada por un Guid vacío— y obligó a leer el log del servidor para
/// enterarse. Estas pruebas usan errores reales de la base, no simulados.
/// </remarks>
[Collection("tenant-db")]
public class ErroresDeBaseTests(TenantDatabaseFixture db)
{
    /// <summary>Provoca el error en la base y devuelve lo que vería el usuario.</summary>
    private Exception Traducir(string sql, object? parametros = null)
    {
        using var cn = db.AbrirComoAdmin();
        cn.Execute($"SET app.tenant_id = '{TenantDatabaseFixture.TenantUno}'");

        var original = Assert.ThrowsAny<PostgresException>(() => cn.Execute(sql, parametros));
        return ExceptionHandler.HandleException<bool>(original);
    }

    [Fact]
    public void Un_RAISE_EXCEPTION_llega_con_su_texto_intacto()
    {
        // Los mensajes de las funciones están escritos para quien recibe la
        // mercadería; traducirlos sería perder información útil.
        var traducida = Traducir(
            "SELECT fn_recibir_lote(gen_random_uuid(), 5, 'X', NULL, 1)");

        var custom = Assert.IsType<CustomException>(traducida);
        Assert.Contains("No existe el producto", custom.Message);
        Assert.Equal(MessageTypes.Warning, custom.messageType);
    }

    [Fact]
    public void Una_violacion_de_clave_foranea_se_explica_sin_jerga()
    {
        var traducida = Traducir(@"
            INSERT INTO products
                (id, product_name, description, sale_price, available_in_pos,
                 laboratory_id, category_id, uom_id, is_active, state,
                 created_by, created, modified_by, modified, tenant_id, current_stock)
            SELECT gen_random_uuid(), 'TEST FK', '', 10, true,
                   NULL, '00000000-0000-0000-0000-000000000000',
                   (SELECT id FROM unit_of_measurement WHERE tenant_id = @t LIMIT 1),
                   true, true, 1, now(), 1, now(), @t, 0",
            new { t = TenantDatabaseFixture.TenantUno });

        var custom = Assert.IsType<CustomException>(traducida);
        Assert.Contains("dato relacionado", custom.Message);
        // El nombre del índice no le sirve a nadie que no sea programador.
        Assert.DoesNotContain("products_category_id_fkey", custom.Message);
        // Pero la causa real se conserva para el log.
        Assert.IsType<PostgresException>(custom.InnerException);
        Assert.True(custom.SaveLog);
    }

    [Fact]
    public void Un_error_de_base_ya_no_se_hace_pasar_por_un_fallo_de_conexion()
    {
        // Esta es la regresión que importa: si PostgreSQL respondió, hubo
        // conexión, y decir lo contrario manda a depurar al lugar equivocado.
        var traducida = Traducir("SELECT funcion_que_no_existe(1)");

        Assert.DoesNotContain("conectar", traducida.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<CustomException>(traducida);
    }
}
