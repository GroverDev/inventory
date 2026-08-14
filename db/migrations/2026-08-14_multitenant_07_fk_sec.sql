-- =============================================================================
-- Integridad referencial por tenant en el schema sec
-- =============================================================================
-- La migración 06 cubrió las claves foráneas de public y se olvidó de sec. Lo
-- detectó la prueba de guardarraíl que recorre pg_constraint buscando FK simples
-- entre dos tablas por tenant.
--
-- La más relevante es sec.users_roles: sin tenant_id en la clave, se podría
-- asignar a un usuario un rol de otra farmacia. El usuario terminaría con un rol
-- que su propia farmacia no ve —o sea, sin permisos— y sin ningún error.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. UNIQUE (tenant_id, id) faltantes en las tablas referenciadas
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    t          text;
    solo_tabla text;
    padres     text[] := ARRAY['sec.roles', 'sec.user_mfa'];
BEGIN
    FOREACH t IN ARRAY padres LOOP
        solo_tabla := split_part(t, '.', 2);

        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = format('%s_tenant_id_uk', solo_tabla)
              AND conrelid = t::regclass
        ) THEN
            EXECUTE format('ALTER TABLE sec.%I ADD CONSTRAINT %I UNIQUE (tenant_id, id)',
                           solo_tabla, format('%s_tenant_id_uk', solo_tabla));
        END IF;
    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- 2. Claves foráneas compuestas
-- -----------------------------------------------------------------------------
-- user_mfa_user_id_fkey tiene ON DELETE CASCADE y hay que conservarlo: borrar un
-- usuario debe llevarse su configuración de segundo factor.
DO $$
DECLARE
    fks text[][] := ARRAY[
        ARRAY['users_roles_user_id_fk',                    'users_roles',             'user_id',     'users',    ''],
        ARRAY['users_roles_rol_id_fk',                     'users_roles',             'rol_id',      'roles',    ''],
        ARRAY['roles_forms_roles_id_fk',                   'roles_forms',             'rol_id',      'roles',    ''],
        ARRAY['user_mfa_user_id_fkey',                     'user_mfa',                'user_id',     'users',    ' ON DELETE CASCADE'],
        ARRAY['user_mfa_recovery_codes_user_mfa_id_fkey',  'user_mfa_recovery_codes', 'user_mfa_id', 'user_mfa', ' ON DELETE CASCADE'],
        ARRAY['fk_users_changepass_users',                 'users_changepass',        'user_id',     'users',    ''],
        ARRAY['users_resetpass_users_id_fk',               'users_resetpass',         'user_id',     'users',    '']
    ];
    i integer;
BEGIN
    FOR i IN 1 .. array_length(fks, 1) LOOP
        EXECUTE format('ALTER TABLE sec.%I DROP CONSTRAINT IF EXISTS %I', fks[i][2], fks[i][1]);
        EXECUTE format(
            'ALTER TABLE sec.%I ADD CONSTRAINT %I FOREIGN KEY (tenant_id, %I) REFERENCES sec.%I (tenant_id, id)%s',
            fks[i][2], fks[i][1], fks[i][3], fks[i][4], fks[i][5]);
    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- 3. sec.users_login queda como está, a propósito
-- -----------------------------------------------------------------------------
-- Su tenant_id admite NULL: registra intentos fallidos de correos que no existen
-- en ninguna farmacia, donde el tenant es genuinamente desconocido. Una clave
-- compuesta con MATCH SIMPLE no se evalúa cuando alguna columna es NULL, que es
-- el caso de todas las filas nuevas, así que sería una restricción que nunca
-- dispara. La prueba de guardarraíl la lista como excepción documentada.

COMMIT;
