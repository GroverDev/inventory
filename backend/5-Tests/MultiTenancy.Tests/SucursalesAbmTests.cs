using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;
using Inventory.Infrastructure;
using Seguridad.Infrastructure;

namespace MultiTenancy.Tests;

/// <summary>
/// Sucursales, paso 2: el ABM, la habilitación de usuarios y el cambio de
/// sucursal de una sesión.
/// </summary>
[Collection("tenant-db")]
public class SucursalesAbmTests(TenantDatabaseFixture db)
{
    private const int Uno = TenantDatabaseFixture.TenantUno;

    private BranchRepository Sucursales(int tenantId) => new(db.ContextoApp(tenantId));
    private UsersRepository Usuarios(int tenantId) => new(db.ContextoAppSeguridad(tenantId));

    private (int Id, Guid Uuid) SembrarUsuarioEn(int tenantId, string email)
    {
        using var admin = db.AbrirComoAdmin();
        int id = admin.ExecuteScalar<int>("SELECT set_sequences_key('sec.users')");
        var uuid = Guid.NewGuid();
        admin.Execute(@"
            INSERT INTO sec.users
                (id, user_name, password, email, full_name, last_access, change_password, is_active,
                 created_by, created, modified_by, modified, uuid, tenant_id)
            VALUES
                (@id, @email, 'x', @email, @email, now(), false, true, 1, now(), 1, now(), @uuid, @tenant)",
            new { id, email, uuid, tenant = tenantId });
        return (id, uuid);
    }

    private static Branch Nueva(string nombre, int usuario) => new()
    {
        Name = nombre,
        Address = "",
        Phone = "",
        IsActive = true,
        CreatedBy = usuario,
        ModifiedBy = usuario,
        Created = DateTime.UtcNow,
        Modified = DateTime.UtcNow
    };

    private static List<Guid> HabilitadasDe(Npgsql.NpgsqlConnection cn, int userId) =>
        cn.Query<Guid>("SELECT branch_id FROM sec.users_branches WHERE user_id = @userId", new { userId }).ToList();

    [Fact]
    public async Task Quien_crea_una_sucursal_queda_habilitado_en_ella_sin_cambiar_su_default()
    {
        var (creador, _) = SembrarUsuarioEn(Uno, "abm-creador@test.local");
        using (var admin = db.AbrirComoAdmin())
            admin.Execute("INSERT INTO sec.users_branches (tenant_id, user_id, branch_id, is_default) VALUES (@t, @u, @b, true)",
                          new { t = Uno, u = creador, b = db.SucursalDe(Uno) });

        Guid nueva = await Sucursales(Uno).CreateBranch(Nueva("ABM Creada", creador));

        using var cn = db.AbrirComoAdmin();
        Assert.Contains(nueva, HabilitadasDe(cn, creador));
        Assert.Equal(db.SucursalDe(Uno), cn.ExecuteScalar<Guid>(
            "SELECT branch_id FROM sec.users_branches WHERE user_id = @creador AND is_default", new { creador }));
    }

    [Fact]
    public async Task No_puede_haber_dos_sucursales_con_el_mismo_nombre()
    {
        var (creador, _) = SembrarUsuarioEn(Uno, "abm-duplicada@test.local");
        await Sucursales(Uno).CreateBranch(Nueva("ABM Duplicada", creador));

        var ex = await Assert.ThrowsAsync<CustomException>(() =>
            Sucursales(Uno).CreateBranch(Nueva("abm duplicada", creador)));
        Assert.Contains("Ya existe", ex.Message);
    }

    [Fact]
    public async Task No_se_puede_desactivar_la_sucursal_que_deja_a_un_usuario_sin_ninguna()
    {
        var (creador, _) = SembrarUsuarioEn(Uno, "abm-desactiva@test.local");
        Guid sucursal = await Sucursales(Uno).CreateBranch(Nueva("ABM Por Cerrar", creador));
        // El creador solo quedó habilitado en la sucursal nueva.

        var cerrar = Nueva("ABM Por Cerrar", creador);
        cerrar.Id = sucursal;
        cerrar.IsActive = false;

        var ex = await Assert.ThrowsAsync<CustomException>(() => Sucursales(Uno).UpdateBranch(cerrar));
        Assert.Contains("abm-desactiva@test.local", ex.Message);

        // Habilitado también en la Principal, ya puede cerrarse.
        using (var admin = db.AbrirComoAdmin())
            admin.Execute("INSERT INTO sec.users_branches (tenant_id, user_id, branch_id, is_default) VALUES (@t, @u, @b, true)",
                          new { t = Uno, u = creador, b = db.SucursalDe(Uno) });

        Assert.Equal(1, await Sucursales(Uno).UpdateBranch(cerrar));
    }

    [Fact]
    public async Task No_se_puede_eliminar_la_unica_sucursal_activa()
    {
        // La farmacia dos solo tiene su Principal.
        using var admin = db.AbrirComoAdmin();
        int adminDos = admin.ExecuteScalar<int>("SELECT id FROM sec.users WHERE tenant_id = @t LIMIT 1", new { t = db.TenantDos });

        await Assert.ThrowsAsync<CustomException>(() =>
            Sucursales(db.TenantDos).DeleteBranch(db.SucursalDe(db.TenantDos), adminDos));
    }

    [Fact]
    public async Task Asignar_sucursales_reemplaza_las_anteriores_y_fija_la_default()
    {
        var (creador, _) = SembrarUsuarioEn(Uno, "abm-asignador@test.local");
        Guid norte = await Sucursales(Uno).CreateBranch(Nueva("ABM Norte", creador));
        var (userId, uuid) = SembrarUsuarioEn(Uno, "abm-asignado@test.local");

        await Usuarios(Uno).AssignBranchesToUser(uuid, [db.SucursalDe(Uno), norte], norte, creador);
        await Usuarios(Uno).AssignBranchesToUser(uuid, [norte], norte, creador);

        var vistas = await Usuarios(Uno).GetBranchesByUserUuid(uuid);
        Assert.True(vistas.Single(b => b.BranchId == norte).IsDefault);
        Assert.False(vistas.Single(b => b.BranchId == db.SucursalDe(Uno)).Enabled);

        using var cn = db.AbrirComoAdmin();
        Assert.Equal([norte], HabilitadasDe(cn, userId));
    }

    [Fact]
    public async Task La_sucursal_default_tiene_que_estar_entre_las_habilitadas()
    {
        var (_, uuid) = SembrarUsuarioEn(Uno, "abm-default-fuera@test.local");

        await Assert.ThrowsAsync<CustomException>(() =>
            Usuarios(Uno).AssignBranchesToUser(uuid, [db.SucursalDe(Uno)], Guid.NewGuid(), 1));
        await Assert.ThrowsAsync<CustomException>(() =>
            Usuarios(Uno).AssignBranchesToUser(uuid, [], Guid.Empty, 1));
    }

    [Fact]
    public async Task No_se_puede_habilitar_a_un_usuario_en_una_sucursal_de_otra_farmacia()
    {
        var (_, uuid) = SembrarUsuarioEn(Uno, "abm-ajena@test.local");

        // RLS la oculta, así que ni siquiera cuenta como sucursal válida.
        await Assert.ThrowsAsync<CustomException>(() =>
            Usuarios(Uno).AssignBranchesToUser(uuid, [db.SucursalDe(db.TenantDos)], db.SucursalDe(db.TenantDos), 1));
    }

    [Fact]
    public async Task Cambiar_de_sucursal_queda_registrado_en_la_sesion_para_el_refresh()
    {
        var (userId, _) = SembrarUsuarioEn(Uno, "abm-cambio@test.local");
        var tokens = new RefreshTokenRepository(db.ContextoAppSeguridad(Uno));
        await tokens.Create(userId, Uno, db.SucursalDe(Uno), 9001, "hash-cambio", "d", "Web", DateTime.UtcNow.AddDays(1));
        var otra = Guid.NewGuid();

        Assert.Equal(1, await tokens.SetBranchForSession(userId, 9001, otra));
        Assert.Equal(otra, (await tokens.GetByHash("hash-cambio"))!.BranchId);

        // Otra sesión del mismo usuario no se toca.
        Assert.Equal(0, await tokens.SetBranchForSession(userId, 9002, otra));
    }
}
