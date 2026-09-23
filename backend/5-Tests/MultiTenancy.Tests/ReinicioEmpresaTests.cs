using System.Reflection;
using Dapper;

namespace MultiTenancy.Tests;

/// <summary>
/// El reinicio de datos de una empresa (AdminRepository.ResetCompany) borra
/// tabla por tabla, de hijas a padres. Cada tabla nueva que cuelgue de un
/// producto, una venta o una compra tiene que estar en su lista, en orden.
/// </summary>
/// <remarks>
/// Se descubrió que faltaba stock_items desde que existen las existencias: el
/// DELETE de products chocaba contra su FK y el reinicio entero fallaba, sin que
/// ninguna prueba lo notara. Esta ejecuta la lista real, así que falla apenas
/// alguien agregue una tabla y se olvide de ella.
/// </remarks>
[Collection("tenant-db")]
public class ReinicioEmpresaTests(TenantDatabaseFixture db)
{
    private static (string Schema, string Table)[] ListaDelReinicio() =>
        ((string Schema, string Table)[])typeof(Seguridad.Infrastructure.AdminRepository)
            .GetField("TenantDataTables", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

    [Fact]
    public void La_lista_del_reinicio_se_puede_borrar_en_orden_y_conserva_las_sucursales()
    {
        using var cn = db.AbrirComoApp(TenantDatabaseFixture.TenantUno);
        using var tx = cn.BeginTransaction();

        foreach (var (schema, table) in ListaDelReinicio())
            cn.Execute($"DELETE FROM \"{schema}\".\"{table}\"", transaction: tx);

        // La siembra que corre después del borrado deja la empresa operativa.
        cn.Execute("SELECT sec.fn_seed_tenant_master_data(1)", transaction: tx);

        Assert.Equal(0, cn.ExecuteScalar<int>("SELECT count(*) FROM products", transaction: tx));
        Assert.Equal(0, cn.ExecuteScalar<int>("SELECT count(*) FROM stock_items", transaction: tx));
        Assert.Equal(0, cn.ExecuteScalar<int>("SELECT count(*) FROM stock_transfers", transaction: tx));

        // La organización queda: sucursales y quién trabaja en cada una.
        Assert.True(cn.ExecuteScalar<int>("SELECT count(*) FROM branches", transaction: tx) > 0);
        Assert.True(cn.ExecuteScalar<int>("SELECT count(*) FROM sec.users_branches", transaction: tx) > 0);

        tx.Rollback();
    }

    [Fact]
    public void Toda_tabla_que_referencia_a_una_que_se_borra_tambien_esta_en_la_lista()
    {
        // Complementa a la anterior: esa solo prueba las tablas que hoy tienen
        // filas. Esta mira la estructura: si una tabla apunta a otra que el
        // reinicio borra, el DELETE de esa otra fallaría en cuanto haya filas.
        // Así se detectaron cash_session_counts (apunta a cash_sessions) y
        // held_sales (apunta a customers).
        var enLista = ListaDelReinicio().Select(t => t.Table).ToHashSet();

        using var cn = db.AbrirComoAdmin();
        var faltantes = cn.Query<string>(@"
            SELECT DISTINCT c.relname
              FROM pg_constraint k
              JOIN pg_class c      ON c.oid = k.conrelid
              JOIN pg_class p      ON p.oid = k.confrelid
              JOIN pg_namespace n  ON n.oid = c.relnamespace
             WHERE k.contype = 'f' AND n.nspname = 'public'
               AND p.relname = ANY(@borradas)",
            new { borradas = enLista.ToArray() })
            .Where(t => !enLista.Contains(t))
            .ToList();

        Assert.Empty(faltantes);
    }
}
