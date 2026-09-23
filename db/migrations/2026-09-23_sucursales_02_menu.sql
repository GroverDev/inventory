-- -----------------------------------------------------------------------------
-- Pantalla de "Sucursales" en el menú
-- -----------------------------------------------------------------------------
-- Dar de alta sucursales y habilitar usuarios en ellas es administración de la
-- propia farmacia, igual que crear usuarios: va junto a "Usuarios" y cada rol
-- recibe sobre ella los mismos permisos que ya tiene sobre users-admin.
--
-- El servidor exige el permiso de create/update/delete de este formulario
-- (HasFormPermission en BranchController): el menú decide quién VE la
-- pantalla, el backend decide quién puede USARLA.
--
-- Idempotente.

DO $$
DECLARE
    v_form    integer;
    v_padre   integer;
    v_source  integer;
    v_creados integer;
BEGIN
    SELECT f.form_id, f.id INTO v_padre, v_source
      FROM sec.forms f
     WHERE f.route = 'users-admin'
       AND f.state;

    IF v_padre IS NULL THEN
        RAISE EXCEPTION 'No se encontró el formulario "users-admin"; el menú no tiene la forma esperada.';
    END IF;

    SELECT id INTO v_form FROM sec.forms WHERE route = 'branches-admin';

    IF v_form IS NULL THEN
        v_form := set_sequences_key('sec.forms');

        INSERT INTO sec.forms
            (id, form_id, name_form, description, icon_css, show_order, route,
             show_menu, is_form_register, module_id, state,
             created_by, created, modified_by, modified, controller)
        SELECT v_form, v_padre, 'Sucursales',
               'Sucursales de la farmacia', 'fal fa-store-alt', 4,
               'branches-admin', true, true, f.module_id, true,
               1, now(), 1, now(), 'ninguno'
          FROM sec.forms f WHERE f.id = v_source;

        RAISE NOTICE 'Formulario "Sucursales" creado con id %.', v_form;
    ELSE
        RAISE NOTICE 'El formulario "Sucursales" ya existía (id %).', v_form;
    END IF;

    INSERT INTO sec.roles_forms
        (rol_id, form_id, can_create, can_read, can_update, can_delete,
         state, created_by, created, modified_by, modified, tenant_id)
    SELECT rf.rol_id, v_form, rf.can_create, rf.can_read, rf.can_update, rf.can_delete,
           true, 1, now(), 1, now(), rf.tenant_id
      FROM sec.roles_forms rf
     WHERE rf.form_id = v_source
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
