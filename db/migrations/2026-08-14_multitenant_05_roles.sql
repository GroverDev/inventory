-- =============================================================================
-- Roles múltiples: resolución determinista
-- =============================================================================
-- sec.users_roles es muchos a muchos por diseño (PK compuesta user_id + rol_id),
-- pero la autenticación devolvía una fila por rol y el backend se quedaba con la
-- primera. Sin ORDER BY, "la primera" la decide el planificador: el mismo usuario
-- podía recibir un rol u otro entre dos inicios de sesión.
--
-- Hoy no tiene efecto observable —el único consumo es `rol == 'Cajero'`, y quien
-- tiene varios roles no es cajero— pero se vuelve un bug intermitente en cuanto
-- alguien acumule Cajero junto a otro rol.
--
-- Esta versión devuelve UNA fila por usuario, con:
--   roles     todos los roles activos, separados por coma (fuente de verdad)
--   rol_name  el rol EFECTIVO, elegido de forma determinista
--
-- El rol efectivo es el primero que no sea 'Cajero'; si Cajero es el único, es
-- Cajero. Así `rol_name = 'Cajero'` equivale exactamente a "está restringido a
-- sus propios datos", que es la regla que aplica el backend, y los clientes que
-- ya comparaban contra RolName (el POS web y el móvil) quedan correctos sin
-- cambiar una línea.
-- =============================================================================

BEGIN;

-- CREATE OR REPLACE no puede cambiar el tipo de retorno de una función, y esta
-- versión agrega la columna `roles`. Hay que soltarla primero. Va dentro de la
-- transacción, así que si algo falla la función anterior sigue en pie.
DROP FUNCTION IF EXISTS sec.fn_auth_lookup(varchar, integer);

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
    rol_name        varchar,
    roles           varchar
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
           COALESCE(pr.rol_id,   0),
           COALESCE(pr.rol_name, '')::varchar,
           COALESCE(pr.roles,    '')::varchar
    FROM sec.users u
    -- JOIN, no LEFT JOIN: si la farmacia está desactivada, sus usuarios no entran.
    JOIN sec.tenants       t ON t.id = u.tenant_id AND t.is_active
    LEFT JOIN sec.user_mfa m ON m.user_id = u.id AND m.mfa_type = 'totp'
    LEFT JOIN LATERAL (
        SELECT
            -- Rol efectivo: los que no son 'Cajero' ordenan primero, y entre
            -- iguales decide el id. Determinista entre ejecuciones.
            (array_agg(r.id       ORDER BY (r.name_rol = 'Cajero'), r.id))[1] AS rol_id,
            (array_agg(r.name_rol ORDER BY (r.name_rol = 'Cajero'), r.id))[1] AS rol_name,
            string_agg(r.name_rol, ',' ORDER BY r.name_rol)                   AS roles
        FROM sec.users_roles ur
        JOIN sec.roles r ON r.id = ur.rol_id AND r.state
        WHERE ur.user_id = u.id AND ur.state
    ) pr ON true
    WHERE u.is_active
      AND ((p_email   IS NOT NULL AND u.email = p_email)
        OR (p_user_id IS NOT NULL AND u.id    = p_user_id));

    -- Encontrado el usuario, ya se sabe el tenant. Fijarlo acá hace que todo lo
    -- que siga en ESTA conexión corra con el tenant correcto y bajo RLS.
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

REVOKE ALL  ON FUNCTION sec.fn_auth_lookup(varchar, integer) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION sec.fn_auth_lookup(varchar, integer) TO app_pos;

-- -----------------------------------------------------------------------------
-- Limpieza del dato heredado
-- -----------------------------------------------------------------------------
-- gpenafiel acumuló 'Administrador' y 'SuperAdmin'. Es un residuo del antiguo
-- ResetCompany, que garantizaba el rol SuperAdmin sin quitar el anterior. Con la
-- resolución determinista ya no rompe nada, pero conviene no arrastrarlo.
DELETE FROM sec.users_roles ur
 USING sec.roles r
 WHERE r.id = ur.rol_id
   AND ur.user_id = 1
   AND r.name_rol = 'Administrador'
   AND EXISTS (
       SELECT 1 FROM sec.users_roles ur2
         JOIN sec.roles r2 ON r2.id = ur2.rol_id
        WHERE ur2.user_id = 1 AND r2.name_rol = 'SuperAdmin' AND ur2.state
   );

COMMIT;
