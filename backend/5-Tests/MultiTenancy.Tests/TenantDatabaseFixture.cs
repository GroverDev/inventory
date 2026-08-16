using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace MultiTenancy.Tests;

/// <summary>
/// Base desechable con dos farmacias, para verificar el aislamiento.
/// </summary>
/// <remarks>
/// Se copia de la base de desarrollo con <c>CREATE DATABASE ... TEMPLATE</c>, así
/// que las pruebas corren contra el esquema <b>tal como está hoy</b>. Eso es
/// deliberado: lo que se quiere detectar es que alguien agregue una tabla sin
/// política, o que el rol de la aplicación recupere privilegios que anulen RLS.
/// <para>
/// Crea su propio rol en vez de reutilizar <c>app_pos</c>, por dos razones: no
/// necesita conocer la contraseña de desarrollo, y no puede romperla. Las
/// políticas no nombran ningún rol, así que aplican a cualquiera que no sea
/// superusuario ni dueño de las tablas.
/// </para>
/// </remarks>
public sealed class TenantDatabaseFixture : IAsyncLifetime
{
    /// <summary>Farmacia que ya existía: la del cliente actual.</summary>
    public const int TenantUno = 1;

    /// <summary>Farmacia creada por el fixture mediante la provisión real.</summary>
    public int TenantDos { get; private set; }

    private const string RolPrueba = "app_pos_test";

    private readonly string _admin   = Env("TEST_PG_ADMIN",
        "Host=localhost;Port=5432;Username=postgres;Database=postgres");
    private readonly string _plantilla = Env("TEST_PG_TEMPLATE", "punto_venta");

    private readonly string _baseDatos = "punto_venta_test_" + Guid.NewGuid().ToString("N")[..8];
    private readonly string _password  = Guid.NewGuid().ToString("N");

    private string _appConn = "";

    private static string Env(string clave, string porDefecto) =>
        Environment.GetEnvironmentVariable(clave) is { Length: > 0 } v ? v : porDefecto;

    /// <summary>Conexión con los privilegios de la aplicación, sin tenant fijado.</summary>
    public NpgsqlConnection AbrirComoApp()
    {
        var cn = new NpgsqlConnection(_appConn);
        cn.Open();
        return cn;
    }

    /// <summary>Conexión con los privilegios de la aplicación, actuando como un tenant.</summary>
    public NpgsqlConnection AbrirComoApp(int tenantId)
    {
        var cn = AbrirComoApp();
        cn.Execute("SELECT set_config('app.tenant_id', @t, false)", new { t = tenantId.ToString() });
        return cn;
    }

    /// <summary>
    /// Contexto de datos apuntando a la base desechable, para ejercitar los
    /// repositorios que abren su propia conexión en vez de recibirla.
    /// </summary>
    public Inventory.Infrastructure.InventoryDbContext ContextoApp(int tenantId)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{Common.Utilities.MultiTenancy.ConnectionStringResolver.Key}"] = _appConn,
            })
            .Build();

        var tenant = new Common.Utilities.MultiTenancy.TenantContext();
        tenant.SetTenant(tenantId);

        return new Inventory.Infrastructure.InventoryDbContext(config, tenant);
    }

    /// <summary>Conexión de superusuario. Sirve para preparar datos saltando RLS.</summary>
    public NpgsqlConnection AbrirComoAdmin()
    {
        var cn = new NpgsqlConnection(AdminSobre(_baseDatos));
        cn.Open();
        return cn;
    }

    private string AdminSobre(string db)
    {
        var b = new NpgsqlConnectionStringBuilder(_admin) { Database = db };
        return b.ToString();
    }

    public async Task InitializeAsync()
    {

        await using (var maestro = new NpgsqlConnection(_admin))
        {
            try
            {
                await maestro.OpenAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "No se pudo conectar a PostgreSQL para preparar las pruebas. Definí la variable " +
                    "TEST_PG_ADMIN con una cadena de superusuario, por ejemplo:\n" +
                    "  TEST_PG_ADMIN='Host=localhost;Port=5432;Username=postgres;Password=...;Database=postgres'",
                    ex);
            }

            await CopiarPlantilla(maestro);

            // El rol es del cluster, no de la base: puede existir de una corrida previa.
            await maestro.ExecuteAsync($@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = '{RolPrueba}') THEN
                        CREATE ROLE {RolPrueba} LOGIN;
                    END IF;
                END $$;
                ALTER ROLE {RolPrueba} PASSWORD '{_password}';");
        }

        await using (var db = new NpgsqlConnection(AdminSobre(_baseDatos)))
        {
            await db.OpenAsync();
            await db.ExecuteAsync($@"
                GRANT USAGE ON SCHEMA public, sec TO {RolPrueba};
                GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES    IN SCHEMA public, sec TO {RolPrueba};
                GRANT USAGE, SELECT                 ON ALL SEQUENCES IN SCHEMA public, sec TO {RolPrueba};
                GRANT EXECUTE ON FUNCTION public.current_tenant() TO {RolPrueba};
                GRANT EXECUTE ON FUNCTION sec.fn_auth_lookup(varchar, integer) TO {RolPrueba};
                GRANT EXECUTE ON FUNCTION sec.fn_provision_tenant(varchar, varchar, varchar, varchar, varchar) TO {RolPrueba};
                GRANT EXECUTE ON FUNCTION sec.fn_seed_tenant_master_data(integer) TO {RolPrueba};
                GRANT EXECUTE ON FUNCTION public.set_sequences_key(varchar) TO {RolPrueba};");

            // La segunda farmacia se crea con la provisión real, no a mano: así las
            // pruebas de aislamiento también cubren que el alta deje datos usables.
            TenantDos = await db.ExecuteScalarAsync<int>(
                "SELECT sec.fn_provision_tenant(@n, @s, @e, @f, @p)",
                new
                {
                    n = "Farmacia de Prueba",
                    s = "farmacia-prueba",
                    e = "admin@farmacia-prueba.test",
                    f = "Administrador de Prueba",
                    p = "$pbkdf2-sha512$60000$hash-de-prueba-sin-uso"
                });
        }

        _appConn = new NpgsqlConnectionStringBuilder(_admin)
        {
            Database = _baseDatos,
            Username = RolPrueba,
            Password = _password
        }.ToString();
    }

    /// <summary>
    /// Copia la base de desarrollo. Si hay conexiones ociosas estorbando —un cliente
    /// gráfico abierto es lo habitual— las cierra y reintenta.
    /// </summary>
    /// <remarks>
    /// Solo cierra conexiones en estado <c>idle</c>: no tienen transacción abierta,
    /// así que no se pierde trabajo y el cliente reconecta solo. Las que están
    /// ejecutando algo, o con una transacción a medias, no se tocan: ahí sí podría
    /// perderse algo, y es preferible avisar.
    /// </remarks>
    private async Task CopiarPlantilla(NpgsqlConnection maestro)
    {
        try
        {
            await maestro.ExecuteAsync($"CREATE DATABASE \"{_baseDatos}\" TEMPLATE \"{_plantilla}\"");
            return;
        }
        catch (PostgresException ex) when (ex.SqlState == "55006")
        {
            var ocupadas = await maestro.QueryAsync<string>(
                @"SELECT DISTINCT coalesce(application_name, 'desconocido')
                    FROM pg_stat_activity
                   WHERE datname = @db AND pid <> pg_backend_pid() AND state <> 'idle'",
                new { db = _plantilla });

            if (ocupadas.Any())
                throw new InvalidOperationException(
                    $"No se pudo copiar «{_plantilla}»: hay conexiones con trabajo en curso " +
                    $"({string.Join(", ", ocupadas)}). Cerralas y volvé a correr las pruebas.", ex);

            await maestro.ExecuteAsync(
                @"SELECT pg_terminate_backend(pid) FROM pg_stat_activity
                   WHERE datname = @db AND pid <> pg_backend_pid() AND state = 'idle'",
                new { db = _plantilla });

            await maestro.ExecuteAsync($"CREATE DATABASE \"{_baseDatos}\" TEMPLATE \"{_plantilla}\"");
        }
    }

    public async Task DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();

        await using var maestro = new NpgsqlConnection(_admin);
        await maestro.OpenAsync();
        await maestro.ExecuteAsync($@"
            SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_baseDatos}';");
        await maestro.ExecuteAsync($"DROP DATABASE IF EXISTS \"{_baseDatos}\"");
    }
}

[CollectionDefinition("tenant-db")]
public class TenantDatabaseCollection : ICollectionFixture<TenantDatabaseFixture>;
