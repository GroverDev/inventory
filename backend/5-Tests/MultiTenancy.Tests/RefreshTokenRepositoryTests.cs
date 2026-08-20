using Dapper;
using Seguridad.Infrastructure;

namespace MultiTenancy.Tests;

/// <summary>
/// Sesiones activas / "usuarios conectados": sec.refresh_tokens nunca tuvo
/// RLS (se consulta pre-tenant, ver PoliticasTests), así que el aislamiento
/// entre farmacias acá lo tiene que garantizar el código, no la base. Esto
/// habría atrapado el bug real de esta sesión: tenant_id nunca se escribía y
/// todo cascaba en el default de la farmacia 1.
/// </summary>
[Collection("tenant-db")]
public class RefreshTokenRepositoryTests(TenantDatabaseFixture db)
{
    private RefreshTokenRepository Repo(int tenantId) => new(db.ContextoAppSeguridad(tenantId));

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

    [Fact]
    public async Task Create_guarda_el_tenant_id_real_no_el_default()
    {
        int userId = SembrarUsuarioEn(db.TenantDos, "refresh-1@test.local");
        var repo = Repo(db.TenantDos);

        long id = await repo.Create(userId, db.TenantDos, sessionId: 111, "hash-1", "device-1", "Web", DateTime.UtcNow.AddDays(30));

        using var admin = db.AbrirComoAdmin();
        var (tenantGuardado, sessionGuardado) = admin.QueryFirst<(int, int?)>(
            "SELECT tenant_id, session_id FROM sec.refresh_tokens WHERE id = @id", new { id });

        Assert.Equal(db.TenantDos, tenantGuardado);
        Assert.Equal(111, sessionGuardado);
    }

    [Fact]
    public async Task GetActiveForTenant_no_muestra_sesiones_de_otra_farmacia()
    {
        int userUno = SembrarUsuarioEn(TenantDatabaseFixture.TenantUno, "refresh-tenant1@test.local");
        int userDos = SembrarUsuarioEn(db.TenantDos, "refresh-tenant2@test.local");

        await Repo(TenantDatabaseFixture.TenantUno).Create(
            userUno, TenantDatabaseFixture.TenantUno, 201, "hash-tenant1", "d1", "Web", DateTime.UtcNow.AddDays(30));
        await Repo(db.TenantDos).Create(
            userDos, db.TenantDos, 202, "hash-tenant2", "d2", "Web", DateTime.UtcNow.AddDays(30));

        var conectadosEnDos = await Repo(db.TenantDos).GetActiveForTenant(db.TenantDos);

        Assert.Contains(conectadosEnDos, s => s.Uuid != "" && s.Device == "d2");
        Assert.DoesNotContain(conectadosEnDos, s => s.Device == "d1");
    }

    [Fact]
    public async Task GetByIdForTenant_no_encuentra_la_sesion_de_otra_farmacia()
    {
        int userId = SembrarUsuarioEn(db.TenantDos, "refresh-2@test.local");
        var repo = Repo(db.TenantDos);

        long id = await repo.Create(userId, db.TenantDos, 301, "hash-2", "d", "Web", DateTime.UtcNow.AddDays(30));

        Assert.Null(await repo.GetByIdForTenant(id, TenantDatabaseFixture.TenantUno));
        Assert.NotNull(await repo.GetByIdForTenant(id, db.TenantDos));
    }

    [Fact]
    public async Task RevokeAllForUserInTenant_no_toca_sesiones_de_otra_farmacia_y_devuelve_los_session_id()
    {
        int userUno = SembrarUsuarioEn(TenantDatabaseFixture.TenantUno, "refresh-cierre1@test.local");
        int userDos = SembrarUsuarioEn(db.TenantDos, "refresh-cierre2@test.local");

        await Repo(TenantDatabaseFixture.TenantUno).Create(
            userUno, TenantDatabaseFixture.TenantUno, 401, "hash-c1", "d", "Web", DateTime.UtcNow.AddDays(30));
        await Repo(db.TenantDos).Create(
            userDos, db.TenantDos, 402, "hash-c2", "d", "Web", DateTime.UtcNow.AddDays(30));
        await Repo(db.TenantDos).Create(
            userDos, db.TenantDos, 403, "hash-c3", "d2", "Web", DateTime.UtcNow.AddDays(30));

        var revocados = await Repo(db.TenantDos).RevokeAllForUserInTenant(userDos, db.TenantDos);

        Assert.Equal(new[] { 402, 403 }, revocados.OrderBy(x => x));

        using var admin = db.AbrirComoAdmin();
        bool sesionAjenaSigueActiva = admin.ExecuteScalar<bool>(
            "SELECT revoked_at IS NULL FROM sec.refresh_tokens WHERE token_hash = 'hash-c1'");
        Assert.True(sesionAjenaSigueActiva);
    }
}
