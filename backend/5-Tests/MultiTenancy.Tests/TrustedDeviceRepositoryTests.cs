using Dapper;
using Seguridad.Domain;
using Seguridad.Infrastructure;

namespace MultiTenancy.Tests;

/// <summary>
/// "Recordar este dispositivo" en el TOTP: emitir, listar los propios,
/// revocar uno puntual y revocar todos.
/// </summary>
[Collection("tenant-db")]
public class TrustedDeviceRepositoryTests(TenantDatabaseFixture db)
{
    private TrustedDeviceRepository Repo(int tenantId) => new(db.ContextoAppSeguridad(tenantId));

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
    public async Task Create_y_GetByHash_recuperan_el_mismo_dispositivo()
    {
        int userId = SembrarUsuarioEn(db.TenantDos, "trusted-1@test.local");
        var repo = Repo(db.TenantDos);

        await repo.Create(userId, db.TenantDos, "hash-abc", "Chrome en Windows", DateTime.UtcNow.AddDays(30));
        var fila = await repo.GetByHash("hash-abc");

        Assert.NotNull(fila);
        Assert.Equal(userId, fila!.UserId);
        Assert.Equal(db.TenantDos, fila.TenantId);
        Assert.Equal("Chrome en Windows", fila.DeviceLabel);
        Assert.True(fila.IsActive);
    }

    [Fact]
    public async Task GetActiveForUser_no_devuelve_revocados_ni_vencidos()
    {
        int userId = SembrarUsuarioEn(db.TenantDos, "trusted-2@test.local");
        var repo = Repo(db.TenantDos);

        long activoId = await repo.Create(userId, db.TenantDos, "hash-activo", "activo", DateTime.UtcNow.AddDays(30));
        long revocadoId = await repo.Create(userId, db.TenantDos, "hash-revocado", "revocado", DateTime.UtcNow.AddDays(30));
        await repo.Create(userId, db.TenantDos, "hash-vencido", "vencido", DateTime.UtcNow.AddMinutes(-1));

        await repo.Revoke(revocadoId);

        var activos = await repo.GetActiveForUser(userId);

        Assert.Single(activos);
        Assert.Equal(activoId, activos[0].Id);
    }

    [Fact]
    public async Task GetByIdForUser_no_devuelve_el_dispositivo_de_otro_usuario()
    {
        int userA = SembrarUsuarioEn(db.TenantDos, "trusted-owner@test.local");
        int userB = SembrarUsuarioEn(db.TenantDos, "trusted-intruso@test.local");
        var repo = Repo(db.TenantDos);

        long id = await repo.Create(userA, db.TenantDos, "hash-ajeno", "de A", DateTime.UtcNow.AddDays(30));

        Assert.Null(await repo.GetByIdForUser(id, userB));
        Assert.NotNull(await repo.GetByIdForUser(id, userA));
    }

    [Fact]
    public async Task RevokeAllForUser_deja_sin_dispositivos_activos()
    {
        int userId = SembrarUsuarioEn(db.TenantDos, "trusted-3@test.local");
        var repo = Repo(db.TenantDos);

        await repo.Create(userId, db.TenantDos, "hash-uno", "uno", DateTime.UtcNow.AddDays(30));
        await repo.Create(userId, db.TenantDos, "hash-dos", "dos", DateTime.UtcNow.AddDays(30));

        await repo.RevokeAllForUser(userId);

        Assert.Empty(await repo.GetActiveForUser(userId));
    }
}
