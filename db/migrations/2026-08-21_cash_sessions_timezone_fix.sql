-- Corrige que cash_sessions/cash_movements guardaban sus timestamps sin zona
-- horaria (timestamp), mientras sales.sale_date sí la trae (timestamptz).
--
-- El backend los llenaba con DateTime.Now (hora del servidor) y el servidor de
-- Postgres corre en Etc/UTC, así que los dígitos guardados ya eran UTC — pero al
-- no estar marcados, el navegador los tomaba como si ya fueran hora local de
-- Bolivia y no les restaba las 4 horas, mostrando la apertura de una sesión
-- hasta 4 horas más tarde de lo real (y a veces "después" de sus propias
-- ventas, que sí se convierten bien). `AT TIME ZONE 'UTC'` interpreta los
-- valores existentes como UTC y los convierte a timestamptz sin correrles la
-- hora — es la conversión correcta dado que el servidor siempre estuvo en UTC.
--
-- El código ya se actualizó para escribir DateTime.UtcNow en estas columnas
-- (ver CashSessionApplication.OpenSession y CashMovementApplication.CreateMovement).
-- CloseSession no necesita cambios: usa NOW() de Postgres, que ya devuelve
-- timestamptz.

ALTER TABLE cash_sessions
  ALTER COLUMN opened_at TYPE timestamptz USING opened_at AT TIME ZONE 'UTC',
  ALTER COLUMN closed_at TYPE timestamptz USING closed_at AT TIME ZONE 'UTC',
  ALTER COLUMN created   TYPE timestamptz USING created   AT TIME ZONE 'UTC',
  ALTER COLUMN modified  TYPE timestamptz USING modified  AT TIME ZONE 'UTC';

ALTER TABLE cash_movements
  ALTER COLUMN created  TYPE timestamptz USING created  AT TIME ZONE 'UTC',
  ALTER COLUMN modified TYPE timestamptz USING modified AT TIME ZONE 'UTC';
