-- =============================================================================
-- Sucursales, paso 1: modelo y sucursal activa de la sesión
-- =============================================================================
-- Hasta acá una farmacia (tenant) era un solo local. Este script agrega el
-- nivel de sucursal debajo del tenant, sin cambiar todavía ningún
-- comportamiento:
--
--   * Cada tenant recibe una sucursal "Principal", y todas sus filas
--     operativas (existencias, movimientos, ventas, cajas, compras) quedan
--     asignadas a ella.
--   * Cada usuario queda habilitado en esa sucursal, marcada como su default.
--   * Las tablas operativas llevan branch_id con DEFAULT current_branch(), que
--     el backend fija en cada conexión igual que app.tenant_id. Por eso los
--     INSERT actuales siguen funcionando sin cambios.
--
-- Las existencias por sucursal, los traspasos y los precios por sucursal
-- vienen en los pasos siguientes. Con una sola sucursal por tenant, la
-- aplicación se comporta exactamente igual que antes.
--
-- ORDEN DE DESPLIEGUE — igual que con tenant_id:
--   1. Aplicar este script (como postgres).
--   2. Desplegar el backend que fija app.branch_id y emite el claim BranchId.
-- Con el backend viejo, cualquier INSERT en una tabla operativa falla por
-- branch_id NULL: current_branch() no tiene de dónde sacar el valor. Es a
-- propósito. Un DEFAULT fijo a la sucursal Principal sería la misma bomba que
-- el DEFAULT 1 de tenant_id: con dos sucursales, todo lo que olvide indicar la
-- suya terminaría en silencio en la Principal.
--
-- Sin RLS por sucursal. El límite de seguridad sigue siendo el tenant: los
-- reportes consolidados y los traspasos necesitan ver varias sucursales de la
-- misma farmacia a la vez, así que la sucursal se filtra en las consultas.
--
-- Idempotente: se puede correr más de una vez sin efectos secundarios.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Catálogo de sucursales
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.branches (
    id          uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   integer      NOT NULL DEFAULT public.current_tenant()
                             REFERENCES sec.tenants(id),
    name        varchar(150) NOT NULL,
    code        varchar(20),
    address     varchar(300) NOT NULL DEFAULT '',
    phone       varchar(50)  NOT NULL DEFAULT '',
    is_active   boolean      NOT NULL DEFAULT true,
    -- Código de sucursal ante el SIAT (0 = casa matriz). Queda nulo hasta que
    -- la facturación electrónica se conecte con las sucursales; hoy
    -- siat.sucursales es un módulo aparte y no se toca.
    siat_code   integer,
    state       boolean      NOT NULL DEFAULT true,
    created_by  integer      NOT NULL DEFAULT 0,
    created     timestamptz  NOT NULL DEFAULT now(),
    modified_by integer      NOT NULL DEFAULT 0,
    modified    timestamptz  NOT NULL DEFAULT now()
);

COMMENT ON TABLE public.branches IS
    'Sucursales de una farmacia (tenant). Las filas operativas —existencias, '
    'ventas, cajas, compras— pertenecen a una sucursal.';

-- Destino de las FK compuestas (tenant_id, branch_id): impiden que una fila
-- apunte a la sucursal de otra farmacia. Mismo patrón que
-- 2026-08-14_multitenant_06_fk_compuestas.sql.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                    WHERE conname = 'branches_tenant_id_uk'
                      AND conrelid = 'public.branches'::regclass) THEN
        ALTER TABLE public.branches
            ADD CONSTRAINT branches_tenant_id_uk UNIQUE (tenant_id, id);
    END IF;
END $$;

-- Dos sucursales con el mismo nombre en una farmacia serían indistinguibles en
-- el selector.
CREATE UNIQUE INDEX IF NOT EXISTS branches_nombre_uk
    ON public.branches (tenant_id, lower(name)) WHERE state;

-- -----------------------------------------------------------------------------
-- 2. Habilitación de usuarios por sucursal
-- -----------------------------------------------------------------------------
-- Un usuario puede trabajar en varias sucursales. La marcada is_default es a
-- la que entra al iniciar sesión; desde ahí puede cambiar a cualquier otra en
-- la que esté habilitado.
CREATE TABLE IF NOT EXISTS sec.users_branches (
    tenant_id   integer     NOT NULL DEFAULT public.current_tenant(),
    user_id     integer     NOT NULL,
    branch_id   uuid        NOT NULL,
    is_default  boolean     NOT NULL DEFAULT false,
    created_by  integer     NOT NULL DEFAULT 0,
    created     timestamptz NOT NULL DEFAULT now(),
    modified_by integer     NOT NULL DEFAULT 0,
    modified    timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, branch_id),
    -- Compuestas: un usuario solo puede habilitarse en sucursales de su propia
    -- farmacia.
    CONSTRAINT users_branches_user_fk
        FOREIGN KEY (tenant_id, user_id)   REFERENCES sec.users (tenant_id, id),
    CONSTRAINT users_branches_branch_fk
        FOREIGN KEY (tenant_id, branch_id) REFERENCES public.branches (tenant_id, id)
);

COMMENT ON TABLE sec.users_branches IS
    'Sucursales en las que cada usuario está habilitado. is_default es la '
    'sucursal a la que entra al iniciar sesión.';

-- Una sola sucursal default por usuario.
CREATE UNIQUE INDEX IF NOT EXISTS users_branches_default_uk
    ON sec.users_branches (user_id) WHERE is_default;

CREATE INDEX IF NOT EXISTS idx_users_branches_branch
    ON sec.users_branches (tenant_id, branch_id);

GRANT SELECT, INSERT, UPDATE, DELETE ON public.branches, sec.users_branches TO app_pos;

-- -----------------------------------------------------------------------------
-- 3. RLS por tenant en las dos tablas nuevas
-- -----------------------------------------------------------------------------
-- La misma política que el resto (ver 2026-08-14_multitenant_02_rls.sql).
DO $$
DECLARE
    tabla text;
BEGIN
    FOREACH tabla IN ARRAY ARRAY['public.branches', 'sec.users_branches'] LOOP
        EXECUTE format('ALTER TABLE %s ENABLE ROW LEVEL SECURITY', tabla);
        EXECUTE format('ALTER TABLE %s FORCE  ROW LEVEL SECURITY', tabla);
        EXECUTE format('DROP POLICY IF EXISTS tenant_aislado ON %s', tabla);
        EXECUTE format($f$
            CREATE POLICY tenant_aislado ON %s
                USING      (tenant_id = public.current_tenant())
                WITH CHECK (tenant_id = public.current_tenant())
        $f$, tabla);
    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- 4. Sucursal de la sesión
-- -----------------------------------------------------------------------------
-- Igual que current_tenant(): NULL si la variable no está fijada, en vez de
-- lanzar. Como las columnas branch_id son NOT NULL, un INSERT sin sucursal en
-- la sesión falla de todos modos, con un mensaje que nombra la columna.
CREATE OR REPLACE FUNCTION public.current_branch() RETURNS uuid
    LANGUAGE sql STABLE
AS $$
    SELECT nullif(current_setting('app.branch_id', true), '')::uuid
$$;

COMMENT ON FUNCTION public.current_branch() IS
    'Sucursal activa de la conexión actual, o NULL si no se fijó. La fija el '
    'backend a partir del claim BranchId del JWT.';

GRANT EXECUTE ON FUNCTION public.current_branch() TO app_pos;

-- -----------------------------------------------------------------------------
-- 5. Sucursal Principal para cada farmacia existente
-- -----------------------------------------------------------------------------
-- tenant_id explícito: el script corre como postgres, sin tenant en la sesión.
INSERT INTO public.branches (tenant_id, name, code, siat_code)
SELECT t.id, 'Principal', 'PRINCIPAL', 0
  FROM sec.tenants t
 WHERE NOT EXISTS (SELECT 1 FROM public.branches b WHERE b.tenant_id = t.id);

-- Todos los usuarios quedan habilitados en la Principal de su farmacia, como
-- default. Los que ya tengan alguna sucursal (segunda corrida) no se tocan.
INSERT INTO sec.users_branches (tenant_id, user_id, branch_id, is_default)
SELECT u.tenant_id, u.id, b.id, true
  FROM sec.users u
  JOIN LATERAL (
        SELECT b.id FROM public.branches b
         WHERE b.tenant_id = u.tenant_id
         ORDER BY b.created, b.id
         LIMIT 1
       ) b ON true
 WHERE NOT EXISTS (SELECT 1 FROM sec.users_branches ub WHERE ub.user_id = u.id);

-- -----------------------------------------------------------------------------
-- 6. branch_id en las tablas operativas
-- -----------------------------------------------------------------------------
-- Las filas existentes se asignan a la Principal de su farmacia. Después, el
-- DEFAULT pasa a salir de la sesión.
--
-- Quedan fuera a propósito:
--   * Los detalles (sales_detail, purchases_detail, sale_return_detail, ...):
--     heredan la sucursal de su cabecera.
--   * Los maestros (products, customers, providers, ...): son de la farmacia,
--     compartidos por todas sus sucursales. El precio por sucursal va aparte,
--     en un paso posterior.
DO $$
DECLARE
    t          text;
    esquema    text;
    solo_tabla text;
    tabla      text;
    tablas     text[] := ARRAY[
        'public.stock_items',
        'public.stock_movements',
        'public.sales',
        'public.sale_returns',
        'public.cash_sessions',
        'public.cash_movements',
        'public.purchases',            -- sucursal que pide y recibe
        'public.purchases_delivery'
    ];
BEGIN
    FOREACH t IN ARRAY tablas LOOP
        esquema    := split_part(t, '.', 1);
        solo_tabla := split_part(t, '.', 2);
        tabla      := format('%I.%I', esquema, solo_tabla);

        EXECUTE format('ALTER TABLE %s ADD COLUMN IF NOT EXISTS branch_id uuid', tabla);

        -- Postgres corre este script como superusuario, así que RLS no filtra
        -- este UPDATE: alcanza a las filas de todas las farmacias, que es lo
        -- que se quiere.
        EXECUTE format($f$
            UPDATE %s x
               SET branch_id = (SELECT b.id FROM public.branches b
                                 WHERE b.tenant_id = x.tenant_id
                                 ORDER BY b.created, b.id
                                 LIMIT 1)
             WHERE x.branch_id IS NULL
        $f$, tabla);

        EXECUTE format(
            'ALTER TABLE %s ALTER COLUMN branch_id SET DEFAULT public.current_branch()', tabla);
        EXECUTE format('ALTER TABLE %s ALTER COLUMN branch_id SET NOT NULL', tabla);

        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
             WHERE conname = format('fk_%s_branch', solo_tabla)
               AND conrelid = tabla::regclass
        ) THEN
            EXECUTE format(
                'ALTER TABLE %s ADD CONSTRAINT %I FOREIGN KEY (tenant_id, branch_id) '
                'REFERENCES public.branches (tenant_id, id)',
                tabla, format('fk_%s_branch', solo_tabla));
        END IF;

        EXECUTE format('CREATE INDEX IF NOT EXISTS %I ON %s (tenant_id, branch_id)',
                       format('idx_%s_branch', solo_tabla), tabla);
    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- 7. Sucursal activa de cada sesión con refresh token
-- -----------------------------------------------------------------------------
-- El refresh emite un JWT nuevo sin que el usuario elija nada: tiene que
-- recordar en qué sucursal estaba esa sesión. Se guarda por sesión y no por
-- usuario, porque el mismo usuario puede estar en una sucursal desde la web y
-- en otra desde el móvil. Nulo en los tokens emitidos antes de este paso: el
-- refresh cae entonces a la sucursal default del usuario.
ALTER TABLE sec.refresh_tokens ADD COLUMN IF NOT EXISTS branch_id uuid;

-- -----------------------------------------------------------------------------
-- 8. Sucursales habilitadas, para el login
-- -----------------------------------------------------------------------------
-- El login y el refresh corren sin tenant en la sesión (ver
-- sec.fn_auth_lookup), así que no pueden leer sec.users_branches, que está bajo
-- RLS. Esta función es la excepción, acotada a un usuario y a sus sucursales
-- activas.
CREATE OR REPLACE FUNCTION sec.fn_auth_branches(p_user_id integer)
RETURNS TABLE (branch_id uuid, name varchar, is_default boolean)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = sec, public, pg_temp
AS $$
    SELECT b.id, b.name, ub.is_default
      FROM sec.users_branches ub
      JOIN sec.users       u ON u.id = ub.user_id AND u.tenant_id = ub.tenant_id AND u.is_active
      JOIN public.branches b ON b.id = ub.branch_id AND b.tenant_id = ub.tenant_id
                            AND b.is_active AND b.state
     WHERE ub.user_id = p_user_id
     ORDER BY ub.is_default DESC, b.name;
$$;

COMMENT ON FUNCTION sec.fn_auth_branches(integer) IS
    'Sucursales activas en las que está habilitado un usuario, la default primero. '
    'SECURITY DEFINER porque el login la llama antes de tener tenant en la sesión.';

REVOKE ALL ON FUNCTION sec.fn_auth_branches(integer) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION sec.fn_auth_branches(integer) TO app_pos;

-- -----------------------------------------------------------------------------
-- 9. Alta y reinicio de farmacias
-- -----------------------------------------------------------------------------
-- fn_seed_tenant_master_data es la siembra común del alta y del reinicio de
-- datos. Ahora también garantiza la sucursal Principal: sin ninguna sucursal,
-- nadie de la farmacia podría iniciar sesión.
CREATE OR REPLACE FUNCTION sec.fn_seed_tenant_master_data(p_tenant integer)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = sec, public, pg_temp
AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sec.tenants WHERE id = p_tenant) THEN
        RAISE EXCEPTION 'No existe la farmacia %.', p_tenant;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.branches WHERE tenant_id = p_tenant) THEN
        INSERT INTO public.branches (tenant_id, name, code, siat_code)
        VALUES (p_tenant, 'Principal', 'PRINCIPAL', 0);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.unit_of_measurement WHERE tenant_id = p_tenant) THEN
        INSERT INTO public.unit_of_measurement
            (id, unit_name, proportion, precision_rounding, is_large_than_default,
             is_default, is_active, state, created_by, created, modified_by, modified, tenant_id)
        VALUES (gen_random_uuid(), 'UNIDAD', 100, 1, false, true, true, true,
                1, now(), 1, now(), p_tenant);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.laboratories WHERE tenant_id = p_tenant) THEN
        INSERT INTO public.laboratories
            (id, laboratory_name, description, direction, celular,
             is_active, state, created_by, created, modified_by, modified, tenant_id)
        VALUES (gen_random_uuid(), 'SIN LABORATORIO', 'Valor por defecto, editable', '', '',
                true, true, 1, now(), 1, now(), p_tenant);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.categories WHERE tenant_id = p_tenant) THEN
        INSERT INTO public.categories
            (id, category_name, description, is_active, state,
             created_by, created, modified_by, modified, tenant_id)
        VALUES (gen_random_uuid(), 'GENERAL', 'Categoría por defecto, editable',
                true, true, 1, now(), 1, now(), p_tenant);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.payment_methods WHERE tenant_id = p_tenant) THEN
        INSERT INTO public.payment_methods
            (id, name, requires_changes, affects_cash, state, created_by, created, modified_by, modified, icon_css, tenant_id)
        VALUES
            (gen_random_uuid(), 'Efectivo', true,  true,  true, 1, now(), 1, now(), 'fal fa-money-bill-wave', p_tenant),
            (gen_random_uuid(), 'Tarjeta',  false, false, true, 1, now(), 1, now(), 'fal fa-credit-card',     p_tenant),
            (gen_random_uuid(), 'QR',       false, false, true, 1, now(), 1, now(), 'fal fa-qrcode',          p_tenant);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.customers WHERE tenant_id = p_tenant AND is_generic) THEN
        INSERT INTO public.customers
            (id, full_name, document_number, email, cellphone, is_active, is_generic,
             state, created_by, created, modified_by, modified, tenant_id)
        VALUES (gen_random_uuid(), 'Cliente Genérico', '0', '', '', true, true,
                true, 1, now(), 1, now(), p_tenant);
    END IF;
END $$;

REVOKE ALL  ON FUNCTION sec.fn_seed_tenant_master_data(integer) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION sec.fn_seed_tenant_master_data(integer) TO app_pos;

-- El alta crea el administrador y ahora además lo habilita en la Principal.
-- Es copia de la versión vigente con un solo agregado, el punto 5.
CREATE OR REPLACE FUNCTION sec.fn_provision_tenant(
    p_name             varchar,
    p_slug             varchar,
    p_admin_email      varchar,
    p_admin_full_name  varchar,
    p_admin_password   varchar
) RETURNS integer
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = sec, public, pg_temp
AS $$
DECLARE
    v_tenant integer;
    v_rol    integer;
    v_user   integer;
BEGIN
    -- -------------------------------------------------------------------------
    -- Validaciones
    -- -------------------------------------------------------------------------
    IF coalesce(trim(p_name), '') = '' THEN
        RAISE EXCEPTION 'El nombre de la farmacia es obligatorio.';
    END IF;

    IF coalesce(trim(p_slug), '') = '' THEN
        RAISE EXCEPTION 'El slug es obligatorio.';
    END IF;

    IF EXISTS (SELECT 1 FROM sec.tenants t WHERE lower(t.slug) = lower(p_slug)) THEN
        RAISE EXCEPTION 'Ya existe una farmacia con el identificador "%".', p_slug;
    END IF;

    -- email y user_name son únicos GLOBALMENTE, no por tenant: el login no lleva
    -- selector de farmacia, así que el correo es lo que resuelve a cuál pertenece
    -- quien entra. Un correo repetido entre farmacias haría el login ambiguo.
    IF EXISTS (SELECT 1 FROM sec.users u WHERE lower(u.email) = lower(p_admin_email)) THEN
        RAISE EXCEPTION 'El correo "%" ya está registrado en otra farmacia.', p_admin_email;
    END IF;

    -- -------------------------------------------------------------------------
    -- 1. La farmacia
    -- -------------------------------------------------------------------------
    INSERT INTO sec.tenants (name, slug, is_active)
    VALUES (trim(p_name), lower(trim(p_slug)), true)
    RETURNING id INTO v_tenant;

    -- -------------------------------------------------------------------------
    -- 2. Datos maestros mínimos (incluye la sucursal Principal)
    -- -------------------------------------------------------------------------
    PERFORM sec.fn_seed_tenant_master_data(v_tenant);

    -- -------------------------------------------------------------------------
    -- 3. Rol administrador de la farmacia
    -- -------------------------------------------------------------------------
    -- Los roles son por tenant; los formularios son globales. Por eso se crea un
    -- rol nuevo y se le asignan los formularios existentes, sin duplicarlos.
    v_rol := set_sequences_key('sec.roles');

    INSERT INTO sec.roles
        (id, name_rol, description, state, created_by, created, modified_by, modified, tenant_id)
    VALUES (v_rol, 'SuperAdmin', 'Administrador de la farmacia',
            true, 1, now(), 1, now(), v_tenant);

    INSERT INTO sec.roles_forms
        (rol_id, form_id, can_create, can_read, can_update, can_delete,
         state, created_by, created, modified_by, modified, tenant_id)
    SELECT v_rol, f.id, true, true, true, true, true, 1, now(), 1, now(), v_tenant
      FROM sec.forms f
     WHERE f.state;

    -- -------------------------------------------------------------------------
    -- 4. Usuario administrador
    -- -------------------------------------------------------------------------
    v_user := set_sequences_key('sec.users');

    INSERT INTO sec.users
        (id, user_name, password, email, full_name, last_access, change_password,
         is_active, created_by, created, modified_by, modified, uuid, tenant_id)
    VALUES (v_user, lower(trim(p_admin_email)), p_admin_password, lower(trim(p_admin_email)),
            p_admin_full_name, now(),
            true,   -- obliga a cambiar la contraseña en el primer ingreso
            true, v_user, now(), v_user, now(), gen_random_uuid(), v_tenant);

    INSERT INTO sec.users_roles
        (user_id, rol_id, state, created_by, created, modified_by, modified, tenant_id)
    VALUES (v_user, v_rol, true, v_user, now(), v_user, now(), v_tenant);

    -- -------------------------------------------------------------------------
    -- 5. El administrador entra a la sucursal Principal
    -- -------------------------------------------------------------------------
    INSERT INTO sec.users_branches (tenant_id, user_id, branch_id, is_default, created_by, modified_by)
    SELECT v_tenant, v_user, b.id, true, v_user, v_user
      FROM public.branches b
     WHERE b.tenant_id = v_tenant
     ORDER BY b.created, b.id
     LIMIT 1;

    RETURN v_tenant;
END $$;

COMMENT ON FUNCTION sec.fn_provision_tenant(varchar, varchar, varchar, varchar, varchar) IS
    'Da de alta una farmacia con sus datos maestros mínimos, su sucursal Principal, su '
    'rol SuperAdmin y su usuario administrador. SECURITY DEFINER porque provisionar '
    'cruza tenants. Devuelve el id del tenant creado.';

REVOKE ALL ON FUNCTION sec.fn_provision_tenant(varchar, varchar, varchar, varchar, varchar) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION sec.fn_provision_tenant(varchar, varchar, varchar, varchar, varchar) TO app_pos;

COMMIT;

-- =============================================================================
-- Verificación
-- =============================================================================
--   -- Una sucursal por farmacia:
--   SELECT tenant_id, count(*) FROM public.branches GROUP BY tenant_id;
--
--   -- Ningún usuario sin sucursal default (debe devolver cero filas):
--   SELECT u.id, u.email FROM sec.users u
--    WHERE NOT EXISTS (SELECT 1 FROM sec.users_branches ub
--                       WHERE ub.user_id = u.id AND ub.is_default);
--
--   -- Ninguna fila operativa sin sucursal (NOT NULL ya lo garantiza):
--   SELECT count(*) FROM public.sales WHERE branch_id IS NULL;
-- =============================================================================
