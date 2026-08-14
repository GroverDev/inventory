using System.Data;
using Npgsql;

namespace Common.Utilities.MultiTenancy;

/// <summary>
/// Crea conexiones que anuncian su tenant a PostgreSQL.
/// </summary>
/// <remarks>
/// El aislamiento entre tenants NO depende de que cada consulta recuerde filtrar:
/// lo aplica PostgreSQL con Row-Level Security, leyendo la variable de sesión
/// <c>app.tenant_id</c> que se fija acá.
/// </remarks>
public static class TenantConnectionFactory
{
    /// <summary>Variable de sesión que leen las políticas RLS.</summary>
    public const string TenantSetting = "app.tenant_id";

    /// <summary>
    /// Devuelve una conexión <b>cerrada</b> que fija <c>app.tenant_id</c> en cuanto
    /// se abra.
    /// </summary>
    /// <param name="requiereTenant">
    /// <c>true</c> para los datos de negocio: abrir sin tenant resuelto es un bug y
    /// lanza. <c>false</c> solo para el camino de autenticación, que busca al usuario
    /// por correo antes de saber a qué tenant pertenece.
    /// </param>
    /// <remarks>
    /// Se engancha a <see cref="System.Data.Common.DbConnection.StateChange"/> en vez de
    /// devolver la conexión ya abierta, por dos razones:
    /// <list type="bullet">
    /// <item>Los repositorios llaman <c>Open()</c> ellos mismos en 117 sitios, y Npgsql
    /// lanza si se abre una conexión ya abierta.</item>
    /// <item>Dapper también abre de forma implícita cuando nadie llamó a <c>Open()</c>.
    /// El evento cubre ambos caminos sin tocar ningún repositorio.</item>
    /// </list>
    /// La variable se fija en cada apertura, no una sola vez: el pool reutiliza
    /// conexiones físicas entre requests de distintos tenants, así que el valor
    /// anterior siempre queda sobrescrito.
    /// </remarks>
    public static IDbConnection Create(string connectionString, ITenantContext tenant, bool requiereTenant = true)
    {
        // Sin esta guarda, una consulta de negocio sin tenant no falla: con RLS
        // activo simplemente devuelve cero filas, que es mucho más difícil de
        // diagnosticar que una excepción.
        if (requiereTenant && !tenant.HasTenant)
            throw new InvalidOperationException(
                "Se intentó abrir una conexión a datos de negocio sin tenant resuelto. " +
                "El middleware de tenant debe correr antes, y el endpoint debe estar autenticado.");

        var connection = new NpgsqlConnection(connectionString);

        connection.StateChange += (_, args) =>
        {
            if (args.CurrentState != ConnectionState.Open) return;

            // Endpoints anónimos (login, health check) abren conexión sin tenant
            // resuelto. No se fija la variable; RLS se encarga de que esas
            // conexiones no vean datos de negocio.
            if (!tenant.HasTenant) return;

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT set_config('{TenantSetting}', @tenant, false)";

            var parametro = cmd.CreateParameter();
            parametro.ParameterName = "tenant";
            parametro.Value = tenant.TenantId!.Value.ToString();
            cmd.Parameters.Add(parametro);

            cmd.ExecuteNonQuery();
        };

        return connection;
    }
}
