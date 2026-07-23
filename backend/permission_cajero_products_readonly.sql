-- =====================================================================
-- Cajero: acceso SOLO LECTURA al formulario de Productos.
--   - Puede VER productos (can_read = true)
--   - No puede crear / editar / eliminar (can_create/update/delete = false)
--
-- Idempotente: se puede ejecutar varias veces sin duplicar filas.
-- Resuelve los IDs por nombre de rol y por 'route', así funciona
-- aunque los IDs difieran entre bases.
--
-- Ejecutar en la base que usa la API (por defecto en desarrollo: punto_venta).
-- =====================================================================
DO $$
DECLARE
  v_rol_id  int;
  v_form_id int;
  v_cur     int;
  v_parent  int;
  v_rows    int;
BEGIN
  SELECT id INTO v_rol_id  FROM sec.roles WHERE name_rol = 'Cajero'      LIMIT 1;
  IF v_rol_id IS NULL THEN RAISE EXCEPTION 'Rol "Cajero" no encontrado'; END IF;

  SELECT id INTO v_form_id FROM sec.forms WHERE route   = 'products-admin' LIMIT 1;
  IF v_form_id IS NULL THEN RAISE EXCEPTION 'Formulario "products-admin" no encontrado'; END IF;

  -- 1) Asegurar la cadena de formularios padre (para que Productos sea VISIBLE en el menú),
  --    sin sobrescribir permisos ya existentes de esos nodos contenedores.
  v_cur := v_form_id;
  LOOP
    SELECT form_id INTO v_parent FROM sec.forms WHERE id = v_cur;
    EXIT WHEN v_parent IS NULL OR v_parent = 0;

    IF NOT EXISTS (SELECT 1 FROM sec.roles_forms WHERE rol_id = v_rol_id AND form_id = v_parent) THEN
      INSERT INTO sec.roles_forms
        (rol_id, form_id, can_create, can_read, can_update, can_delete, state, created_by, created, modified_by, modified)
      VALUES
        (v_rol_id, v_parent, false, true, false, false, true, 1, now(), 1, now());
    ELSE
      UPDATE sec.roles_forms SET state = true, modified = now()
       WHERE rol_id = v_rol_id AND form_id = v_parent;
    END IF;

    v_cur := v_parent;
  END LOOP;

  -- 2) Productos: solo lectura.
  UPDATE sec.roles_forms
     SET can_create = false, can_read = true, can_update = false, can_delete = false,
         state = true, modified_by = 1, modified = now()
   WHERE rol_id = v_rol_id AND form_id = v_form_id;
  GET DIAGNOSTICS v_rows = ROW_COUNT;

  IF v_rows = 0 THEN
    INSERT INTO sec.roles_forms
      (rol_id, form_id, can_create, can_read, can_update, can_delete, state, created_by, created, modified_by, modified)
    VALUES
      (v_rol_id, v_form_id, false, true, false, false, true, 1, now(), 1, now());
  END IF;

  RAISE NOTICE 'Cajero -> Productos: solo lectura aplicado (rol_id=%, form_id=%).', v_rol_id, v_form_id;
END $$;

-- Verificación:
-- SELECT r.name_rol, f.name_form, f.route, rf.can_create, rf.can_read, rf.can_update, rf.can_delete, rf.state
-- FROM sec.roles_forms rf
-- JOIN sec.roles r ON r.id = rf.rol_id
-- JOIN sec.forms f ON f.id = rf.form_id
-- WHERE r.name_rol = 'Cajero' AND f.route = 'products-admin';
