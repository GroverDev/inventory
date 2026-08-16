-- -----------------------------------------------------------------------------
-- Pantalla de trazabilidad de lote en el menú
-- -----------------------------------------------------------------------------
-- La vista v_trazabilidad_lote existía desde el incremento de lotes pero no la
-- consumía nadie: ni endpoint ni pantalla. Ahora hay
-- GET /api/StockMovement/traceability y la ruta stock-traceability, así que solo
-- falta la entrada de menú — el sidebar se arma desde sec.forms.
--
-- Mismo criterio que la migración de vencimientos: los formularios son GLOBALES,
-- sec.roles_forms es por farmacia, y el permiso se copia desde el rol que ya ve
-- el Control de Stock. Idempotente.

DO $$
DECLARE
    v_form    integer;
    v_padre   integer;
    v_stock   integer;
    v_creados integer;
BEGIN
    SELECT f.form_id, f.id INTO v_padre, v_stock
      FROM sec.forms f
     WHERE f.route = 'inventory-stock'
       AND f.state;

    IF v_padre IS NULL THEN
        RAISE EXCEPTION 'No se encontró el formulario "inventory-stock"; el menú no tiene la forma esperada.';
    END IF;

    SELECT id INTO v_form FROM sec.forms WHERE route = 'stock-traceability';

    IF v_form IS NULL THEN
        v_form := set_sequences_key('sec.forms');

        INSERT INTO sec.forms
            (id, form_id, name_form, description, icon_css, show_order, route,
             show_menu, is_form_register, module_id, state,
             created_by, created, modified_by, modified, controller)
        VALUES (v_form, v_padre, 'Trazabilidad de Lote',
                'A quién se le vendió cada lote', 'ninguno', 4, 'stock-traceability',
                true, true, 1, true,
                1, now(), 1, now(), 'ninguno');

        RAISE NOTICE 'Formulario "Trazabilidad de Lote" creado con id %.', v_form;
    ELSE
        RAISE NOTICE 'El formulario "Trazabilidad de Lote" ya existía (id %).', v_form;
    END IF;

    -- Solo lectura: la pantalla consulta, no modifica nada.
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
