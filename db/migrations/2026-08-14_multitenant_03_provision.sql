-- =============================================================================
-- Multi-tenant: alta de una farmacia nueva
-- =============================================================================
-- Provisionar cruza tenants por definición: quien crea la farmacia 2 está
-- operando como farmacia 1 (o como nadie). Con RLS activo, cada INSERT para el
-- tenant nuevo rebotaría contra WITH CHECK.
--
-- Por eso es SECURITY DEFINER, igual que sec.fn_auth_lookup. Y por eso mismo
-- CADA INSERT de acá abajo fija tenant_id EXPLÍCITAMENTE: dentro de la función
-- el DEFAULT public.current_tenant() devolvería el tenant de QUIEN LLAMA, que es
-- justamente el equivocado.
--
-- La contraseña llega ya hasheada. El hash lo produce la aplicación
-- (Common.Utilities.Cryptography.Hash.HashPassword, PBKDF2-SHA512), y duplicar
-- ese algoritmo en SQL sería una fuente de divergencia silenciosa.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- Datos maestros mínimos de una farmacia
-- -----------------------------------------------------------------------------
-- La usan el alta de una farmacia nueva y el reinicio de datos, para que las dos
-- dejen exactamente el mismo estado inicial.
--
-- Sin esto la farmacia queda inutilizable: products exige laboratory_id y uom_id
-- NOT NULL, y el POS necesita al menos un método de pago. Se descubrió probando
-- el aislamiento: un tenant sin maestros propios no puede registrar ni un producto.
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
            (id, name, requires_changes, state, created_by, created, modified_by, modified, icon_css, tenant_id)
        VALUES
            (gen_random_uuid(), 'Efectivo', true,  true, 1, now(), 1, now(), 'fal fa-money-bill-wave', p_tenant),
            (gen_random_uuid(), 'Tarjeta',  false, true, 1, now(), 1, now(), 'fal fa-credit-card',     p_tenant),
            (gen_random_uuid(), 'QR',       false, true, 1, now(), 1, now(), 'fal fa-qrcode',          p_tenant);
    END IF;
END $$;

REVOKE ALL  ON FUNCTION sec.fn_seed_tenant_master_data(integer) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION sec.fn_seed_tenant_master_data(integer) TO app_pos;

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
    -- 2. Datos maestros mínimos
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

    RETURN v_tenant;
END $$;

COMMENT ON FUNCTION sec.fn_provision_tenant(varchar, varchar, varchar, varchar, varchar) IS
    'Da de alta una farmacia con sus datos maestros mínimos, su rol SuperAdmin y su '
    'usuario administrador. SECURITY DEFINER porque provisionar cruza tenants. '
    'Devuelve el id del tenant creado.';

REVOKE ALL ON FUNCTION sec.fn_provision_tenant(varchar, varchar, varchar, varchar, varchar) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION sec.fn_provision_tenant(varchar, varchar, varchar, varchar, varchar) TO app_pos;

COMMIT;

-- =============================================================================
-- Uso
-- =============================================================================
--   SELECT sec.fn_provision_tenant(
--       'Farmacia San José', 'san-jose',
--       'admin@sanjose.com', 'Administrador',
--       '$pbkdf2-sha512$60000$...'   -- hash generado por la aplicación
--   );
--
-- Para dar de baja una farmacia, NO se borra: se desactiva.
--   UPDATE sec.tenants SET is_active = false WHERE slug = 'san-jose';
-- (el login todavía no consulta is_active del tenant — ver nota en el resumen)
-- =============================================================================
