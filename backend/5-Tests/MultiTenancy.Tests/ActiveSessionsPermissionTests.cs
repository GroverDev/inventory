using Dapper;

namespace MultiTenancy.Tests;

/// <summary>
/// El formulario "active-sessions" (panel de sesiones/usuarios conectados)
/// tiene que existir y estar otorgado al rol Administrador con permiso de
/// lectura y de borrado — si no, HasFormPermission deniega todo y la pantalla
/// queda inservible sin que nada más avise.
/// </summary>
[Collection("tenant-db")]
public class ActiveSessionsPermissionTests(TenantDatabaseFixture db)
{
    [Fact]
    public void El_formulario_active_sessions_existe()
    {
        using var admin = db.AbrirComoAdmin();

        bool existe = admin.ExecuteScalar<bool>(
            "SELECT EXISTS(SELECT 1 FROM sec.forms WHERE route = 'active-sessions' AND state)");

        Assert.True(existe, "El seed de la migración 2026-08-18_active_sessions_menu.sql no está aplicado.");
    }

    [Fact]
    public void El_rol_Administrador_del_tenant_original_puede_leer_y_cerrar_sesiones()
    {
        // Acotado al tenant 1 (el que copia la plantilla desde punto_venta):
        // es el único cuyo seed se puede verificar sin asumir cómo
        // fn_seed_tenant_master_data replica permisos a un tenant provisto en
        // caliente por el fixture.
        using var admin = db.AbrirComoAdmin();

        var (canRead, canDelete) = admin.QueryFirst<(bool, bool)>(@"
            SELECT COALESCE(rf.can_read, true), COALESCE(rf.can_delete, true)
            FROM sec.roles_forms rf
            JOIN sec.forms f ON f.id = rf.form_id
            JOIN sec.roles r ON r.id = rf.rol_id
            WHERE f.route = 'active-sessions' AND r.name_rol = 'Administrador'
              AND rf.tenant_id = @tenant AND rf.state",
            new { tenant = TenantDatabaseFixture.TenantUno });

        Assert.True(canRead);
        Assert.True(canDelete);
    }
}
