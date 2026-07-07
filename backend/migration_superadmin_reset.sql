-- ============================================================================
-- Migración: rol SuperAdmin + opción "Resetear Empresa"
-- ----------------------------------------------------------------------------
-- Habilita la funcionalidad de reinicio de empresa (endpoint POST /api/Admin/ResetCompany).
-- Es IDEMPOTENTE: puede ejecutarse varias veces sin duplicar datos.
--
-- Qué hace:
--   1. Crea el rol 'SuperAdmin' (si no existe).
--   2. Registra el formulario/menú 'Resetear Empresa' (route 'company-reset') bajo ADMINISTRACIÓN.
--   3. Otorga al rol SuperAdmin acceso a TODOS los formularios (menú completo).
--   4. Asigna el rol SuperAdmin a los usuarios que hoy tienen el rol 'Administrador'
--      (para que exista al menos un usuario capaz de ejecutar el reinicio).
--
-- Ejecutar una sola vez contra la base 'punto_venta' (o la que corresponda).
-- ============================================================================

BEGIN;

-- 1) Rol SuperAdmin -----------------------------------------------------------
INSERT INTO sec.roles (id, name_rol, description, state, created_by, created, modified_by, modified)
SELECT set_sequences_key('sec.roles'), 'SuperAdmin', 'Super administrador del sistema', true, 1, now(), 1, now()
WHERE NOT EXISTS (SELECT 1 FROM sec.roles WHERE LOWER(name_rol) = LOWER('SuperAdmin'));

-- 2) Formulario/menú "Resetear Empresa" --------------------------------------
INSERT INTO sec.forms
      (id, form_id, name_form, description, icon_css, show_order, route, show_menu,
       is_form_register, module_id, state, created_by, created, modified_by, modified, controller)
SELECT set_sequences_key('sec.forms'),
       COALESCE((SELECT id      FROM sec.forms WHERE name_form = 'ADMINISTRACIÓN' LIMIT 1), 0),
       'Resetear Empresa',
       'Reinicia la base de datos para una empresa nueva',
       'fal fa-exclamation-triangle',
       99,
       'company-reset',
       true,
       true,
       COALESCE((SELECT module_id FROM sec.forms WHERE name_form = 'ADMINISTRACIÓN' LIMIT 1), 2),
       true, 1, now(), 1, now(), 'ninguno'
WHERE NOT EXISTS (SELECT 1 FROM sec.forms WHERE route = 'company-reset');

-- 3) SuperAdmin con acceso a todos los formularios ---------------------------
INSERT INTO sec.roles_forms
      (rol_id, form_id, can_create, can_read, can_update, can_delete, state, created_by, created, modified_by, modified)
SELECT sa.id, f.id, true, true, true, true, true, 1, now(), 1, now()
  FROM sec.roles sa
  CROSS JOIN sec.forms f
 WHERE LOWER(sa.name_rol) = LOWER('SuperAdmin')
   AND f.state
   AND NOT EXISTS (SELECT 1 FROM sec.roles_forms rf WHERE rf.rol_id = sa.id AND rf.form_id = f.id);

UPDATE sec.roles_forms rf
   SET state = true, can_read = true, modified = now()
  FROM sec.roles sa
 WHERE rf.rol_id = sa.id AND LOWER(sa.name_rol) = LOWER('SuperAdmin');

-- 4) Asignar SuperAdmin a los administradores actuales -----------------------
INSERT INTO sec.users_roles (user_id, rol_id, state, created_by, created, modified_by, modified)
SELECT ur.user_id, sa.id, true, 1, now(), 1, now()
  FROM sec.users_roles ur
  INNER JOIN sec.roles adm ON adm.id = ur.rol_id AND LOWER(adm.name_rol) = LOWER('Administrador')
  CROSS JOIN sec.roles sa
 WHERE LOWER(sa.name_rol) = LOWER('SuperAdmin')
   AND ur.state
   AND NOT EXISTS (
        SELECT 1 FROM sec.users_roles x WHERE x.user_id = ur.user_id AND x.rol_id = sa.id
   );

COMMIT;
