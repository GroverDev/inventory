using Dapper;

namespace MultiTenancy.Tests;

/// <summary>
/// Guardarraíles sobre la configuración del aislamiento.
/// </summary>
/// <remarks>
/// A diferencia de las pruebas de comportamiento, estas no ejercitan un caso: se
/// aseguran de que nadie desarme la estructura sin darse cuenta. Una tabla nueva
/// sin política, una clave foránea sin <c>tenant_id</c> o un privilegio de más en
/// el rol de la aplicación no rompen nada visible —simplemente dejan de aislar—,
/// y sin una prueba así nadie se entera hasta que un cliente ve datos de otro.
/// </remarks>
[Collection("tenant-db")]
public class PoliticasTests(TenantDatabaseFixture db)
{
    /// <summary>
    /// Tablas con <c>tenant_id</c> que a propósito NO llevan RLS, con su motivo.
    /// Agregar algo acá tiene que ser una decisión consciente.
    /// </summary>
    private static readonly Dictionary<string, string> SinRlsAdrede = new()
    {
        ["public.sequences_key"] = "genera claves primarias globales; por tenant colisionarían",
        ["public.zlogs_app"]     = "auditoría; tenant_id es para diagnóstico, no para aislar",
        ["sec.users_login"]      = "registra intentos de correos que no existen en ninguna farmacia",
        ["sec.user_mfa"]         = "maquinaria de autenticación, previa a conocer el tenant",
        ["sec.user_mfa_recovery_codes"] = "ídem",
        ["sec.users_changepass"] = "ídem",
        ["sec.users_resetpass"]  = "ídem",
        ["sec.refresh_tokens"]   = "ídem",
    };

    [Fact]
    public void Toda_tabla_con_tenant_id_tiene_RLS_o_una_excepcion_documentada()
    {
        using var cn = db.AbrirComoAdmin();

        var tablas = cn.Query<(string Tabla, bool RlsActivo, bool Forzado, int Politicas)>(@"
            SELECT n.nspname || '.' || c.relname,
                   c.relrowsecurity,
                   c.relforcerowsecurity,
                   (SELECT count(*)::int FROM pg_policies p
                     WHERE p.schemaname = n.nspname AND p.tablename = c.relname)
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relkind = 'r'
              AND n.nspname IN ('public','sec')
              AND EXISTS (SELECT 1 FROM pg_attribute a
                           WHERE a.attrelid = c.oid AND a.attname = 'tenant_id' AND a.attnum > 0)
            ORDER BY 1").ToList();

        Assert.NotEmpty(tablas);

        var desprotegidas = tablas
            .Where(t => !SinRlsAdrede.ContainsKey(t.Tabla))
            .Where(t => !t.RlsActivo || !t.Forzado || t.Politicas == 0)
            .Select(t => $"{t.Tabla} (rls={t.RlsActivo}, force={t.Forzado}, políticas={t.Politicas})")
            .ToList();

        Assert.True(desprotegidas.Count == 0,
            "Estas tablas tienen tenant_id pero no están aisladas. Si es a propósito, " +
            "agregalas a SinRlsAdrede con su motivo:\n  " + string.Join("\n  ", desprotegidas));
    }

    /// <summary>
    /// Claves foráneas que a propósito NO incluyen tenant_id, con su motivo.
    /// </summary>
    private static readonly Dictionary<string, string> FkSimplesAdrede = new()
    {
        ["users_login_user_fk"] =
            "sec.users_login.tenant_id admite NULL —registra intentos de correos que no existen " +
            "en ninguna farmacia— y una FK compuesta con MATCH SIMPLE no se evalúa con NULL, " +
            "así que nunca dispararía",
    };

    [Fact]
    public void Toda_clave_foranea_entre_tablas_por_tenant_incluye_tenant_id()
    {
        using var cn = db.AbrirComoAdmin();

        // Una FK simple entre dos tablas por tenant deja pasar referencias cruzadas:
        // la integridad referencial no pasa por RLS.
        var simples = cn.Query<(string Nombre, string Descripcion)>(@"
            SELECT con.conname,
                   con.conname || ': ' || nh.nspname || '.' || h.relname
                                || ' -> ' || np.nspname || '.' || p.relname
            FROM pg_constraint con
            JOIN pg_class h      ON h.oid  = con.conrelid
            JOIN pg_namespace nh ON nh.oid = h.relnamespace
            JOIN pg_class p      ON p.oid  = con.confrelid
            JOIN pg_namespace np ON np.oid = p.relnamespace
            WHERE con.contype = 'f'
              AND array_length(con.conkey, 1) = 1
              AND EXISTS (SELECT 1 FROM pg_attribute a WHERE a.attrelid = h.oid AND a.attname = 'tenant_id')
              AND EXISTS (SELECT 1 FROM pg_attribute a WHERE a.attrelid = p.oid AND a.attname = 'tenant_id')
            ORDER BY 1")
            .Where(f => !FkSimplesAdrede.ContainsKey(f.Nombre))
            .Select(f => f.Descripcion)
            .ToList();

        Assert.True(simples.Count == 0,
            "Estas claves foráneas unen dos tablas por tenant sin incluir tenant_id, así que " +
            "admiten referencias cruzadas entre farmacias. Si alguna es a propósito, agregala " +
            "a FkSimplesAdrede con su motivo:\n  " + string.Join("\n  ", simples));
    }

    [Fact]
    public void Toda_vista_propia_corre_con_los_permisos_de_quien_consulta()
    {
        // Una vista corre con los privilegios de su DUEÑO salvo que se declare
        // security_invoker. Como las vistas las crea postgres —superusuario—, sin
        // esa opción RLS no se aplica adentro y la vista expone las filas de todas
        // las farmacias, aunque sus tablas base estén perfectamente aisladas.
        //
        // Es exactamente el agujero que tenía v_stock_por_vencer hasta que una
        // prueba de aislamiento lo detectó.
        using var cn = db.AbrirComoAdmin();

        var sinInvoker = cn.Query<string>(@"
            SELECT n.nspname || '.' || c.relname
              FROM pg_class c
              JOIN pg_namespace n ON n.oid = c.relnamespace
             WHERE c.relkind = 'v'
               AND n.nspname IN ('public','sec')
               AND NOT COALESCE(c.reloptions::text[] @> ARRAY['security_invoker=true'], false)
             ORDER BY 1").ToList();

        Assert.True(sinInvoker.Count == 0,
            "Estas vistas no declaran security_invoker, así que se saltan RLS y exponen " +
            "datos de todas las farmacias:\n  " + string.Join("\n  ", sinInvoker));
    }

    [Fact]
    public void El_rol_de_la_aplicacion_no_puede_saltarse_RLS()
    {
        // RLS no se aplica a superusuarios ni a roles con BYPASSRLS. Si el rol de la
        // aplicación gana cualquiera de los dos, las políticas quedan de adorno y
        // todo sigue "funcionando" sin aislar nada.
        using var cn = db.AbrirComoApp();

        var (superusuario, bypass) = cn.QueryFirst<(bool, bool)>(
            "SELECT rolsuper, rolbypassrls FROM pg_roles WHERE rolname = current_user");

        Assert.False(superusuario, "El rol de la aplicación es superusuario: RLS no se le aplica.");
        Assert.False(bypass, "El rol de la aplicación tiene BYPASSRLS: RLS no se le aplica.");
    }

    [Fact]
    public void El_rol_de_la_aplicacion_no_puede_alterar_el_esquema()
    {
        using var cn = db.AbrirComoApp();

        Assert.False(cn.ExecuteScalar<bool>(
            "SELECT has_schema_privilege(current_user, 'public', 'CREATE')"));
        Assert.False(cn.ExecuteScalar<bool>(
            "SELECT has_schema_privilege(current_user, 'sec', 'CREATE')"));
    }

    [Fact]
    public void La_busqueda_de_autenticacion_funciona_sin_tenant_y_lo_deja_fijado()
    {
        // Es la única excepción declarada del sistema: el login busca por correo
        // justamente para averiguar a qué farmacia pertenece quien entra.
        using var cn = db.AbrirComoApp();

        var fila = cn.QueryFirstOrDefault(
            "SELECT user_id, tenant_id FROM sec.fn_auth_lookup(@e, NULL)",
            new { e = "admin@farmacia-prueba.test" });

        Assert.NotNull(fila);
        Assert.Equal(db.TenantDos, (int)fila!.tenant_id);

        // Y como efecto deja la conexión operando en esa farmacia.
        Assert.Equal(db.TenantDos, cn.ExecuteScalar<int>("SELECT public.current_tenant()"));
    }

    [Fact]
    public void Una_farmacia_desactivada_no_puede_entrar()
    {
        using var admin = db.AbrirComoAdmin();
        admin.Execute("UPDATE sec.tenants SET is_active = false WHERE id = @t", new { t = db.TenantDos });

        try
        {
            using var cn = db.AbrirComoApp();
            var fila = cn.QueryFirstOrDefault(
                "SELECT user_id FROM sec.fn_auth_lookup(@e, NULL)",
                new { e = "admin@farmacia-prueba.test" });

            Assert.Null(fila);   // es el corte de servicio de un cliente dado de baja
        }
        finally
        {
            admin.Execute("UPDATE sec.tenants SET is_active = true WHERE id = @t", new { t = db.TenantDos });
        }
    }
}
