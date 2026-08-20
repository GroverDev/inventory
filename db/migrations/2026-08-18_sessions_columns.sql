-- Columnas que faltaban para poder listar/cerrar sesiones desde un panel de
-- administración:
--
--   sec.refresh_tokens.tenant_id
--     Ya existía (la agregó el paso 1 del multitenant), pero el código nunca
--     la escribía: todo insert caía en el DEFAULT estático de esa migración
--     (tenant 1), sin importar el tenant real. No rompía nada porque la tabla
--     nunca se filtraba por tenant_id — hasta ahora, que el panel de "usuarios
--     conectados" sí necesita filtrar por tenant. El código pasa a escribirla
--     explícita; acá solo se documenta que ya existe, no hace falta ALTER.
--
--   sec.refresh_tokens.session_id
--     Nueva. Id de sec.users_login (mismo valor que el claim SessionId del
--     JWT) vigente cuando se emitió o rotó ese refresh token. Sin esto, cerrar
--     una sesión puntual solo podía revocar el refresh token —el usuario
--     seguía entrando con el access token ya emitido hasta que expirara solo—.
--     Con esta columna, cerrar la sesión también puede tumbar ese access token
--     ya emitido de inmediato (vía la lista de revocación en memoria).
--
--   sec.trusted_devices.tenant_id
--     Mismo problema que refresh_tokens pero de origen: la tabla se creó sin
--     la columna. Se agrega ahora, antes de que exista ningún panel que la
--     necesite, para no repetir el mismo hueco dos veces.
--
-- Ninguna de las tres entra en RLS (mismo criterio que el resto de la
-- maquinaria de autenticación, ver 2026-08-14_multitenant_02_rls.sql sección
-- 4): se consultan siempre por user_id/hash, y ahora también por tenant_id de
-- forma explícita en el código, no vía política.
--
-- Idempotente.

ALTER TABLE sec.refresh_tokens
    ADD COLUMN IF NOT EXISTS session_id integer;

ALTER TABLE sec.trusted_devices
    ADD COLUMN IF NOT EXISTS tenant_id integer;

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_tenant
    ON sec.refresh_tokens (tenant_id) WHERE revoked_at IS NULL;
