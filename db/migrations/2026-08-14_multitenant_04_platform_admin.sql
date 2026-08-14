-- =============================================================================
-- Multi-tenant: administrador de plataforma
-- =============================================================================
-- Dar de alta una farmacia NO es una operación de farmacia, es una operación de
-- quien opera el servicio.
--
-- El rol SuperAdmin no sirve para autorizarla: la provisión le crea uno propio a
-- cada farmacia, así que si SuperAdmin pudiera crear tenants, cualquier cliente
-- podría darse de alta clientes nuevos y verlos en su factura.
--
-- Por eso un atributo aparte, fuera del sistema de roles. No es asignable desde
-- la aplicación: los UPDATE de sec.users enumeran sus columnas explícitamente y
-- esta no está entre ellas, así que un admin de farmacia no puede otorgárselo.
-- Se concede a mano, con acceso a la base.
-- =============================================================================

BEGIN;

ALTER TABLE sec.users
    ADD COLUMN IF NOT EXISTS is_platform_admin boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN sec.users.is_platform_admin IS
    'Habilita operaciones de plataforma (alta de farmacias). Se concede solo desde '
    'la base de datos, nunca desde la aplicación.';

-- El usuario 1 es el operador del servicio.
UPDATE sec.users SET is_platform_admin = true WHERE id = 1;

COMMIT;

-- =============================================================================
-- Conceder o revocar el permiso más adelante:
--   UPDATE sec.users SET is_platform_admin = true  WHERE email = '...';
--   UPDATE sec.users SET is_platform_admin = false WHERE email = '...';
-- =============================================================================
