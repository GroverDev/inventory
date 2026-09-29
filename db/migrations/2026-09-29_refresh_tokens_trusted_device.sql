-- Enlaza cada sesión web con el dispositivo de confianza con el que se abrió.
--
-- Antes, olvidar un dispositivo de confianza solo evitaba saltar el TOTP en el
-- PRÓXIMO login: la sesión ya abierta (refresh token de 30 días) seguía viva y
-- el usuario reentraba sin contraseña ni TOTP. Con esta columna, olvidar el
-- dispositivo también revoca los refresh tokens que nacieron de él.
--
--   sec.refresh_tokens.trusted_device_id
--     Id de sec.trusted_devices. Solo la web lo llena (el móvil sigue sin
--     enlace, su comportamiento no cambia). La rotación del refresh lo hereda.
--     NULL en sesiones sin dispositivo de confianza y en las anteriores a esta
--     migración: esas no se ven afectadas.
--
-- Sin FOREIGN KEY a propósito: sec.trusted_devices y sec.refresh_tokens son
-- maquinaria de autenticación fuera de RLS y se consultan por user_id/hash; el
-- enlace es informativo para poder revocar, no una integridad que proteger.
--
-- Idempotente.

ALTER TABLE sec.refresh_tokens
    ADD COLUMN IF NOT EXISTS trusted_device_id bigint;

CREATE INDEX IF NOT EXISTS ix_refresh_tokens_trusted_device
    ON sec.refresh_tokens (trusted_device_id) WHERE trusted_device_id IS NOT NULL AND revoked_at IS NULL;
