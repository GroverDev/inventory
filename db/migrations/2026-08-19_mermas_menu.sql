-- -----------------------------------------------------------------------------
-- Pantalla de "Reporte de Mermas" en el menú
-- -----------------------------------------------------------------------------
-- Mismo criterio que 2026-08-15_vencimientos_menu.sql: el permiso se copia del
-- rol que ya ve el Reporte de Stock (hermano dentro de Reportes), así cada
-- tenant queda con el permiso en su propio rol sin inventar criterios nuevos.
--
-- Idempotente.

DO $$
DECLARE
    v_form    integer;
    v_padre   integer;
    v_stock   integer;
    v_creados integer;
BEGIN
    SELECT f.form_id, f.id INTO v_padre, v_stock
      FROM sec.forms f
     WHERE f.route = 'report-stock'
       AND f.state;

    IF v_padre IS NULL THEN
        RAISE EXCEPTION 'No se encontró el formulario "report-stock"; el menú no tiene la forma esperada.';
    END IF;

    SELECT id INTO v_form FROM sec.forms WHERE route = 'report-write-offs';

    IF v_form IS NULL THEN
        v_form := set_sequences_key('sec.forms');

        INSERT INTO sec.forms
            (id, form_id, name_form, description, icon_css, show_order, route,
             show_menu, is_form_register, module_id, state,
             created_by, created, modified_by, modified, controller)
        SELECT v_form, v_padre, 'Reporte de Mermas',
               'Bajas de stock por vencimiento, con su valor perdido', 'ninguno', 4,
               'report-write-offs', true, true, f.module_id, true,
               1, now(), 1, now(), 'ninguno'
          FROM sec.forms f WHERE f.id = v_stock;

        RAISE NOTICE 'Formulario "Reporte de Mermas" creado con id %.', v_form;
    ELSE
        RAISE NOTICE 'El formulario "Reporte de Mermas" ya existía (id %).', v_form;
    END IF;

    -- Solo lectura: el reporte no crea, edita ni borra nada (la baja en sí se
    -- da desde Vencimientos, con el permiso de esa pantalla).
    INSERT INTO sec.roles_forms
        (rol_id, form_id, can_create, can_read, can_update, can_delete,
         state, created_by, created, modified_by, modified, tenant_id)
    SELECT rf.rol_id, v_form, false, true, false, false,
           true, 1, now(), 1, now(), rf.tenant_id
      FROM sec.roles_forms rf
     WHERE rf.form_id = v_stock
       AND rf.state
       AND rf.can_read
       AND NOT EXISTS (
             SELECT 1 FROM sec.roles_forms x
              WHERE x.rol_id = rf.rol_id
                AND x.form_id = v_form
           );

    GET DIAGNOSTICS v_creados = ROW_COUNT;
    RAISE NOTICE 'Permiso de lectura otorgado a % rol(es).', v_creados;
END $$;
