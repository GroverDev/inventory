using Common.Utilities.Exceptions;
using Dapper;
using Npgsql;
using Seguridad.Domain;
using Seguridad.Infrastructure;

namespace MultiTenancy.Tests;

/// <summary>
/// Sucursales, paso 1: el modelo, la sucursal de la sesión y la que elige el login.
/// </summary>
[Collection("tenant-db")]
public class SucursalesTests(TenantDatabaseFixture db)
{
    private AuthenticationRepository Auth() => new(db.ContextoAppSeguridad(TenantDatabaseFixture.TenantUno));

    private int SembrarUsuarioEn(int tenantId, string email)
    {
        using var admin = db.AbrirComoAdmin();
        int id = admin.ExecuteScalar<int>("SELECT set_sequences_key('sec.users')");
        admin.Execute(@"
            INSERT INTO sec.users
                (id, user_name, password, email, full_name, last_access, change_password, is_active,
                 created_by, created, modified_by, modified, uuid, tenant_id)
            VALUES
                (@id, @email, 'x', @email, 'Test User', now(), false, true, 1, now(), 1, now(), gen_random_uuid(), @tenant)",
            new { id, email, tenant = tenantId });
        return id;
    }

    private Guid SembrarSucursalEn(int tenantId, string nombre, bool activa = true)
    {
        using var admin = db.AbrirComoAdmin();
        return admin.ExecuteScalar<Guid>(
            "INSERT INTO public.branches (tenant_id, name, is_active) VALUES (@t, @n, @a) RETURNING id",
            new { t = tenantId, n = nombre, a = activa });
    }

    private void Habilitar(int tenantId, int userId, Guid sucursal, bool esDefault)
    {
        using var admin = db.AbrirComoAdmin();
        admin.Execute(@"
            INSERT INTO sec.users_branches (tenant_id, user_id, branch_id, is_default)
            VALUES (@t, @u, @b, @d)",
            new { t = tenantId, u = userId, b = sucursal, d = esDefault });
    }

    [Fact]
    public async Task Un_usuario_nuevo_queda_habilitado_en_la_sucursal_de_quien_lo_crea()
    {
        // Sin sucursal no podría iniciar sesión: el alta de usuarios tiene que
        // dejarlo habilitado en algún lado.
        var repo = new UsersRepository(db.ContextoAppSeguridad(TenantDatabaseFixture.TenantUno));
        var uuid = await repo.CreateUser(new Users
        {
            UserName = "suc-alta@test.local",
            Email = "suc-alta@test.local",
            FullName = "Alta con sucursal",
            Password = "Clave-de-prueba-1",
            IsActive = true,
            LastAccess = DateTime.UtcNow,
            Uuid = Guid.NewGuid(),
            CreatedBy = 1,
            ModifiedBy = 1
        }, 1);

        using var admin = db.AbrirComoAdmin();
        var habilitada = admin.QueryFirstOrDefault<Guid?>(@"
            SELECT ub.branch_id FROM sec.users_branches ub
              JOIN sec.users u ON u.id = ub.user_id
             WHERE u.uuid = @uuid::uuid AND ub.is_default",
            new { uuid });

        Assert.Equal(db.SucursalDe(TenantDatabaseFixture.TenantUno), habilitada);
    }

    [Fact]
    public void La_provision_crea_la_sucursal_principal_y_habilita_al_administrador()
    {
        using var admin = db.AbrirComoAdmin();
        var habilitada = admin.QueryFirstOrDefault<Guid?>(@"
            SELECT ub.branch_id FROM sec.users_branches ub
              JOIN sec.users u ON u.id = ub.user_id
             WHERE u.tenant_id = @t AND u.email = 'admin@farmacia-prueba.test' AND ub.is_default",
            new { t = db.TenantDos });

        Assert.Equal(db.SucursalDe(db.TenantDos), habilitada);
    }

    [Fact]
    public void El_insert_toma_la_sucursal_de_la_sesion()
    {
        // Es lo que permite que los INSERT actuales no cambien: ninguno manda
        // branch_id, lo pone el DEFAULT.
        using var cn = db.AbrirComoApp(db.TenantDos);

        var sucursal = cn.ExecuteScalar<Guid>(@"
            INSERT INTO cash_sessions (user_id, created_by, modified_by, closed_at)
            SELECT id, id, id, now() FROM sec.users LIMIT 1
            RETURNING branch_id");

        Assert.Equal(db.SucursalDe(db.TenantDos), sucursal);
    }

    [Fact]
    public void Sin_sucursal_en_la_sesion_no_se_puede_registrar_nada()
    {
        // Falla cerrado. Un DEFAULT fijo a la Principal escribiría en silencio
        // en la sucursal equivocada apenas haya dos.
        using var cn = db.AbrirComoApp();
        cn.Execute("SELECT set_config('app.tenant_id', @t, false)", new { t = db.TenantDos.ToString() });

        var ex = Assert.Throws<PostgresException>(() => cn.Execute(@"
            INSERT INTO cash_sessions (user_id, created_by, modified_by, closed_at)
            SELECT id, id, id, now() FROM sec.users LIMIT 1"));

        Assert.Equal(PostgresErrorCodes.NotNullViolation, ex.SqlState);
        Assert.Equal("branch_id", ex.ColumnName);
    }

    [Fact]
    public void Una_farmacia_no_ve_las_sucursales_de_otra()
    {
        Guid ajena = SembrarSucursalEn(db.TenantDos, "Solo de la farmacia dos");

        using var cn = db.AbrirComoApp(TenantDatabaseFixture.TenantUno);

        Assert.Null(cn.QueryFirstOrDefault<Guid?>(
            "SELECT id FROM branches WHERE id = @id", new { id = ajena }));
    }

    [Fact]
    public void No_se_puede_registrar_en_la_sucursal_de_otra_farmacia()
    {
        using var cn = db.AbrirComoApp(TenantDatabaseFixture.TenantUno);

        var ex = Assert.Throws<PostgresException>(() => cn.Execute(@"
            INSERT INTO cash_sessions (user_id, created_by, modified_by, closed_at, branch_id)
            SELECT id, id, id, now(), @ajena FROM sec.users LIMIT 1",
            new { ajena = db.SucursalDe(db.TenantDos) }));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ex.SqlState);
    }

    [Fact]
    public void Un_usuario_no_puede_habilitarse_en_la_sucursal_de_otra_farmacia()
    {
        int userId = SembrarUsuarioEn(TenantDatabaseFixture.TenantUno, "suc-cruzada@test.local");

        var ex = Assert.Throws<PostgresException>(() =>
            Habilitar(TenantDatabaseFixture.TenantUno, userId, db.SucursalDe(db.TenantDos), esDefault: true));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ex.SqlState);
    }

    [Fact]
    public void Un_usuario_tiene_una_sola_sucursal_default()
    {
        int userId = SembrarUsuarioEn(TenantDatabaseFixture.TenantUno, "suc-dos-default@test.local");
        Habilitar(TenantDatabaseFixture.TenantUno, userId, db.SucursalDe(TenantDatabaseFixture.TenantUno), esDefault: true);
        Guid otra = SembrarSucursalEn(TenantDatabaseFixture.TenantUno, "Segunda default");

        var ex = Assert.Throws<PostgresException>(() =>
            Habilitar(TenantDatabaseFixture.TenantUno, userId, otra, esDefault: true));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
    }

    [Fact]
    public void Las_sucursales_del_login_se_leen_sin_tenant_y_omiten_las_inactivas()
    {
        int userId = SembrarUsuarioEn(TenantDatabaseFixture.TenantUno, "suc-login@test.local");
        Guid principal = db.SucursalDe(TenantDatabaseFixture.TenantUno);
        Guid cerrada = SembrarSucursalEn(TenantDatabaseFixture.TenantUno, "Cerrada", activa: false);
        Habilitar(TenantDatabaseFixture.TenantUno, userId, principal, esDefault: true);
        Habilitar(TenantDatabaseFixture.TenantUno, userId, cerrada, esDefault: false);

        // Como el login: conexión de la aplicación sin tenant fijado.
        using var cn = db.AbrirComoApp();
        var sucursales = cn.Query<Guid>(
            "SELECT branch_id FROM sec.fn_auth_branches(@u)", new { u = userId }).ToList();

        Assert.Equal([principal], sucursales);
    }

    [Fact]
    public async Task El_login_entra_a_la_sucursal_default()
    {
        int userId = SembrarUsuarioEn(TenantDatabaseFixture.TenantUno, "suc-default@test.local");
        Guid otra = SembrarSucursalEn(TenantDatabaseFixture.TenantUno, "Norte");
        Habilitar(TenantDatabaseFixture.TenantUno, userId, db.SucursalDe(TenantDatabaseFixture.TenantUno), esDefault: false);
        Habilitar(TenantDatabaseFixture.TenantUno, userId, otra, esDefault: true);

        var login = new LoginResponse { UserId = userId };
        await Auth().AssignBranch(login, null);

        Assert.Equal(otra, login.BranchId);
        Assert.Equal("Norte", login.BranchName);
        Assert.Equal(2, login.Branches.Count);
        Assert.True(login.Branches[0].IsDefault);
    }

    [Fact]
    public async Task El_refresh_conserva_la_sucursal_de_la_sesion_mientras_siga_habilitada()
    {
        int userId = SembrarUsuarioEn(TenantDatabaseFixture.TenantUno, "suc-refresh@test.local");
        Guid principal = db.SucursalDe(TenantDatabaseFixture.TenantUno);
        Guid sur = SembrarSucursalEn(TenantDatabaseFixture.TenantUno, "Sur");
        Habilitar(TenantDatabaseFixture.TenantUno, userId, principal, esDefault: true);
        Habilitar(TenantDatabaseFixture.TenantUno, userId, sur, esDefault: false);

        var conservada = new LoginResponse { UserId = userId };
        await Auth().AssignBranch(conservada, sur);
        Assert.Equal(sur, conservada.BranchId);

        // Le quitan la sucursal: el siguiente refresh la devuelve a la default
        // en vez de dejarla seguir operando donde ya no está habilitada.
        using (var admin = db.AbrirComoAdmin())
            admin.Execute("DELETE FROM sec.users_branches WHERE user_id = @u AND branch_id = @b",
                          new { u = userId, b = sur });

        var revocada = new LoginResponse { UserId = userId };
        await Auth().AssignBranch(revocada, sur);
        Assert.Equal(principal, revocada.BranchId);
    }

    [Fact]
    public async Task Sin_sucursales_habilitadas_no_se_puede_entrar()
    {
        int userId = SembrarUsuarioEn(TenantDatabaseFixture.TenantUno, "suc-ninguna@test.local");

        await Assert.ThrowsAsync<CustomException>(() =>
            Auth().AssignBranch(new LoginResponse { UserId = userId }, null));
    }
}
