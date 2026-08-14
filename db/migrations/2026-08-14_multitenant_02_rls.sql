-- =============================================================================
-- Multi-tenant, paso 2 de 2: rol de aplicación y Row-Level Security
-- =============================================================================
-- ORDEN DE DESPLIEGUE — esto NO se puede aplicar solo.
--
--   1. Aplicar este script (como postgres).
--   2. Desplegar el backend que usa sec.fn_auth_* (el código de esta misma rama).
--   3. Cambiar la cadena de conexión de la app al rol app_pos.
--
-- Aplicarlo sin el paso 3 no rompe nada, pero tampoco aísla: postgres es
-- superusuario y dueño de las tablas, y RLS no se aplica a ninguno de los dos.
-- Aplicarlo con backend viejo rompe el login, porque la consulta directa a
-- sec.users pasa a estar filtrada.
--
-- Reversible: ver el bloque de rollback al final.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Rol de aplicación
-- -----------------------------------------------------------------------------
-- Sin SUPERUSER y sin BYPASSRLS: son justamente los dos atributos que harían
-- que las políticas de abajo no se aplicaran.
--
-- Se crea SIN contraseña a propósito. Un rol con LOGIN y sin contraseña no puede
-- autenticarse, así que queda inutilizable hasta que alguien le ponga una:
--
--     ALTER ROLE app_pos PASSWORD '...';     -- openssl rand -base64 24
--
-- Es deliberado que falle ruidosamente si se omite ese paso. La alternativa
-- —dejar una contraseña por defecto en el script— corre el riesgo de terminar
-- en producción, y sería la credencial de acceso a los datos de las 40 farmacias.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_pos') THEN
        CREATE ROLE app_pos LOGIN;
    END IF;
END $$;

GRANT USAGE ON SCHEMA public, sec TO app_pos;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES    IN SCHEMA public, sec TO app_pos;
GRANT USAGE, SELECT                 ON ALL SEQUENCES IN SCHEMA public, sec TO app_pos;

-- Tablas y secuencias que se creen más adelante.
ALTER DEFAULT PRIVILEGES IN SCHEMA public, sec
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO app_pos;
ALTER DEFAULT PRIVILEGES IN SCHEMA public, sec
    GRANT USAGE, SELECT ON SEQUENCES TO app_pos;

-- El rol de aplicación NO debe poder cambiar el esquema. Las migraciones
-- siguen corriendo como postgres.
REVOKE CREATE ON SCHEMA public, sec FROM app_pos;

-- Única excepción: el schema de respaldos. El reinicio de datos de una farmacia
-- copia sus filas a una tabla con marca de tiempo antes de borrarlas, y eso
-- necesita CREATE TABLE. Acotado a este schema, que no contiene nada operativo.
CREATE SCHEMA IF NOT EXISTS backup;
GRANT USAGE, CREATE ON SCHEMA backup TO app_pos;

-- -----------------------------------------------------------------------------
-- 2. Tenant de la sesión
-- -----------------------------------------------------------------------------
-- La forma de dos argumentos de current_setting devuelve NULL en vez de lanzar
-- cuando la variable no está fijada. Con la de un argumento, toda conexión sin
-- tenant terminaría en un error de PostgreSQL en vez de simplemente no ver nada.
CREATE OR REPLACE FUNCTION public.current_tenant() RETURNS integer
    LANGUAGE sql STABLE
AS $$
    SELECT nullif(current_setting('app.tenant_id', true), '')::integer
$$;

COMMENT ON FUNCTION public.current_tenant() IS
    'Tenant de la conexión actual, o NULL si no se fijó. Lo usan las políticas RLS.';

GRANT EXECUTE ON FUNCTION public.current_tenant() TO app_pos;

-- -----------------------------------------------------------------------------
-- 3. Políticas RLS
-- -----------------------------------------------------------------------------
-- Cuando current_tenant() es NULL, la comparación da NULL —no true— así que no
-- se ve ninguna fila. Falla cerrado: una conexión sin tenant no ve nada, en vez
-- de verlo todo.
--
-- FORCE hace que las políticas apliquen también al dueño de la tabla. Es la red
-- que evita que conectar la app como postgres desactive el aislamiento sin que
-- nadie se entere.
DO $$
DECLARE
    t          text;
    tabla      text;
    esquema    text;
    solo_tabla text;
    tablas     text[] := ARRAY[
        -- Datos de negocio
        'public.cash_movements',
        'public.cash_sessions',
        'public.categories',
        'public.customers',
        'public.discounts',
        'public.laboratories',
        'public.payment_methods',
        'public.products',
        'public.products_providers',
        'public.providers',
        'public.purchases',
        'public.purchases_delivery',
        'public.purchases_delivery_detail',
        'public.purchases_detail',
        'public.sale_detail_discounts',
        'public.sale_payments',
        'public.sale_return_detail',
        'public.sale_returns',
        'public.sales',
        'public.sales_detail',
        'public.stock_movements',
        'public.unit_of_measurement',
        -- Superficie de administración de usuarios: un admin de una farmacia no
        -- debe poder listar ni editar los usuarios de otra.
        'sec.users',
        'sec.roles',
        'sec.users_roles',
        'sec.roles_forms'
    ];
BEGIN
    FOREACH t IN ARRAY tablas LOOP
        esquema    := split_part(t, '.', 1);
        solo_tabla := split_part(t, '.', 2);
        tabla      := format('%I.%I', esquema, solo_tabla);

        EXECUTE format('ALTER TABLE %s ENABLE ROW LEVEL SECURITY', tabla);
        EXECUTE format('ALTER TABLE %s FORCE  ROW LEVEL SECURITY', tabla);

        EXECUTE format('DROP POLICY IF EXISTS tenant_aislado ON %s', tabla);
        EXECUTE format($f$
            CREATE POLICY tenant_aislado ON %s
                USING      (tenant_id = public.current_tenant())
                WITH CHECK (tenant_id = public.current_tenant())
        $f$, tabla);

        -- El DEFAULT 1 del paso 1 era una muleta para no romper los INSERT
        -- existentes mientras no había tenant en la sesión. Con varios clientes
        -- sería una bomba: cualquier INSERT que omita tenant_id escribiría en la
        -- farmacia 1. Ahora lo toma de la sesión, y por eso las 219 consultas
        -- del backend siguen sin necesitar cambios.
        EXECUTE format(
            'ALTER TABLE %s ALTER COLUMN tenant_id SET DEFAULT public.current_tenant()', tabla);
    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- 4. Tablas deliberadamente SIN RLS
-- -----------------------------------------------------------------------------
-- sec.forms, sec.modules, public.purchases_status  → catálogos globales, iguales
--   para todas las farmacias. No tienen tenant_id.
--
-- public.sequences_key
--   → generador de claves primarias para sec.users, sec.roles, sec.users_login,
--     sec.forms, sec.modules y sec.users_resetpass. Esas PK son GLOBALES: si el
--     contador fuera por tenant, dos farmacias generarían el mismo sec.users.id
--     y colisionarían contra pk_user. Debe ser global aunque tenga la columna
--     tenant_id heredada del paso 1, que ahí no significa nada.
--     Además, el login fallido de un correo inexistente necesita un id de sesión
--     sin tenant resuelto.
--
-- sec.users_login, sec.user_mfa, sec.user_mfa_recovery_codes,
-- sec.users_changepass, sec.users_resetpass, sec.refresh_tokens
--   → maquinaria de autenticación. Se acceden siempre por un user_id que ya
--     salió de una búsqueda validada, y varias corren antes de que exista tenant
--     (registrar un intento fallido de un correo que no existe en ninguna
--     farmacia). Llevan tenant_id para reportes y limpieza, pero filtrarlas con
--     RLS rompería el login sin agregar aislamiento real.

-- -----------------------------------------------------------------------------
-- 5. Funciones de autenticación
-- -----------------------------------------------------------------------------
-- El login es la única operación que legítimamente corre sin tenant: busca al
-- usuario por correo justamente para averiguar a qué farmacia pertenece.
--
-- SECURITY DEFINER: corren con los permisos del dueño (postgres), así que ven
-- sec.users sin el filtro de RLS. Es la excepción, declarada explícitamente en
-- un solo lugar y acotada a las columnas que el login necesita, en vez de una
-- política permisiva que cualquier consulta podría aprovechar por descuido.
--
-- SET search_path es obligatorio en SECURITY DEFINER: sin eso, quien controle el
-- search_path de la sesión puede hacer que la función ejecute objetos suyos.

CREATE OR REPLACE FUNCTION sec.fn_auth_lookup(p_email varchar DEFAULT NULL,
                                              p_user_id integer DEFAULT NULL)
RETURNS TABLE (
    user_id         integer,
    tenant_id       integer,
    user_name       varchar,
    change_password boolean,
    is_active       boolean,
    email           varchar,
    full_name       varchar,
    uuid            uuid,
    password        varchar,
    mfa_enabled     boolean,
    mfa_required    boolean,
    rol_id          integer,
    rol_name        varchar
)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = sec, public, pg_temp
AS $$
DECLARE
    v_tenant integer;
BEGIN
    RETURN QUERY
    SELECT u.id, u.tenant_id, u.user_name, u.change_password, u.is_active,
           u.email, u.full_name, u.uuid, u.password,
           COALESCE(m.is_enabled,  false),
           COALESCE(m.is_required, false),
           COALESCE(r.id,       0),
           COALESCE(r.name_rol, '')::varchar
    FROM sec.users u
    -- JOIN, no LEFT JOIN: si la farmacia está desactivada, sus usuarios no entran.
    -- Es el corte de servicio para un cliente dado de baja.
    JOIN sec.tenants          t  ON t.id = u.tenant_id AND t.is_active
    LEFT JOIN sec.user_mfa    m  ON m.user_id = u.id AND m.mfa_type = 'totp'
    LEFT JOIN sec.users_roles ur ON ur.user_id = u.id AND ur.state
    LEFT JOIN sec.roles       r  ON r.id = ur.rol_id  AND r.state
    WHERE u.is_active
      AND ((p_email   IS NOT NULL AND u.email = p_email)
        OR (p_user_id IS NOT NULL AND u.id    = p_user_id));

    -- Encontrado el usuario, ya se sabe el tenant. Fijarlo acá hace que todo lo
    -- que siga en ESTA conexión —actualizar last_access, registrar la sesión—
    -- corra con el tenant correcto y bajo RLS, sin necesitar más excepciones.
    SELECT u.tenant_id INTO v_tenant
    FROM sec.users u
    JOIN sec.tenants t ON t.id = u.tenant_id AND t.is_active
    WHERE u.is_active
      AND ((p_email   IS NOT NULL AND u.email = p_email)
        OR (p_user_id IS NOT NULL AND u.id    = p_user_id))
    LIMIT 1;

    IF v_tenant IS NOT NULL THEN
        PERFORM set_config('app.tenant_id', v_tenant::text, false);
    END IF;
END $$;

COMMENT ON FUNCTION sec.fn_auth_lookup(varchar, integer) IS
    'Búsqueda de usuario para autenticación, sin filtro de tenant. Única excepción '
    'a RLS del sistema: el login necesita resolver a qué tenant pertenece quien entra. '
    'Como efecto, fija app.tenant_id en la conexión.';

REVOKE ALL ON FUNCTION sec.fn_auth_lookup(varchar, integer) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION sec.fn_auth_lookup(varchar, integer) TO app_pos;

COMMIT;

-- =============================================================================
-- ROLLBACK
-- =============================================================================
-- Volver la cadena de conexión de la app a postgres ya desactiva el efecto sin
-- tocar el esquema. Para revertir de verdad:
--
--   DO $$
--   DECLARE t record;
--   BEGIN
--       FOR t IN SELECT schemaname, tablename FROM pg_policies
--                 WHERE policyname = 'tenant_aislado'
--       LOOP
--           EXECUTE format('ALTER TABLE %I.%I DISABLE ROW LEVEL SECURITY',
--                          t.schemaname, t.tablename);
--           EXECUTE format('ALTER TABLE %I.%I ALTER COLUMN tenant_id SET DEFAULT 1',
--                          t.schemaname, t.tablename);
--       END LOOP;
--   END $$;
-- =============================================================================
