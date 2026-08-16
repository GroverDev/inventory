-- -----------------------------------------------------------------------------
-- Pantalla de alta de empresas en el menú
-- -----------------------------------------------------------------------------
-- El endpoint POST /api/Admin/tenants existe desde el trabajo multi-tenant, pero
-- no había pantalla: crear una farmacia exigía Swagger o curl.
--
-- El permiso NO se copia desde el Control de Stock como en las otras dos
-- migraciones: dar de alta empresas no es una operación de inventario y no
-- corresponde que la vea un cajero. Se copia desde "Resetear Empresa"
-- (company-reset), que es la frontera de seguridad equivalente que ya existe en
-- el sistema: ambas son operaciones de plataforma y peligrosas.
--
-- El servidor igual exige `is_platform_admin`, que es un atributo del usuario y
-- no un permiso de rol: el menú decide quién VE la pantalla, el backend decide
-- quién puede USARLA. Idempotente.

DO $$
DECLARE
    v_form    integer;
    v_padre   integer;
    v_reset   integer;
    v_creados integer;
BEGIN
    SELECT f.form_id, f.id INTO v_padre, v_reset
      FROM sec.forms f
     WHERE f.route = 'company-reset'
       AND f.state;

    IF v_padre IS NULL THEN
        RAISE EXCEPTION 'No se encontró el formulario "company-reset"; el menú no tiene la forma esperada.';
    END IF;

    SELECT id INTO v_form FROM sec.forms WHERE route = 'company-create';

    IF v_form IS NULL THEN
        v_form := set_sequences_key('sec.forms');

        INSERT INTO sec.forms
            (id, form_id, name_form, description, icon_css, show_order, route,
             show_menu, is_form_register, module_id, state,
             created_by, created, modified_by, modified, controller)
        SELECT v_form, v_padre, 'Nueva Empresa',
               'Alta de una farmacia con su administrador', 'fal fa-store', 1,
               'company-create', true, true, f.module_id, true,
               1, now(), 1, now(), 'ninguno'
          FROM sec.forms f WHERE f.id = v_reset;

        RAISE NOTICE 'Formulario "Nueva Empresa" creado con id %.', v_form;
    ELSE
        RAISE NOTICE 'El formulario "Nueva Empresa" ya existía (id %).', v_form;
    END IF;

    INSERT INTO sec.roles_forms
        (rol_id, form_id, can_create, can_read, can_update, can_delete,
         state, created_by, created, modified_by, modified, tenant_id)
    SELECT rf.rol_id, v_form, true, true, false, false,
           true, 1, now(), 1, now(), rf.tenant_id
      FROM sec.roles_forms rf
     WHERE rf.form_id = v_reset
       AND rf.state
       AND rf.can_read
       AND NOT EXISTS (
             SELECT 1 FROM sec.roles_forms x
              WHERE x.rol_id = rf.rol_id
                AND x.form_id = v_form
           );

    GET DIAGNOSTICS v_creados = ROW_COUNT;
    RAISE NOTICE 'Permiso otorgado a % rol(es).', v_creados;
END $$;
