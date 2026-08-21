-- Termina de pasar a timestamptz las columnas de fecha/hora que seguían sin zona.
--
-- Continuación de 2026-08-21_cash_sessions_timezone_fix.sql, que arregló solo
-- cash_sessions/cash_movements. Quedaban estas 12 columnas en 6 tablas; el
-- inventario completo se sacó de information_schema, así que después de esta
-- migración no queda ninguna columna `timestamp without time zone` en public/sec
-- salvo zlogs_app.raise_date (ver abajo).
--
-- Por qué importa: una columna sin zona viaja al navegador sin marca de UTC, y
-- el navegador la interpreta como hora local — corriendo la hora 4 horas en
-- Bolivia. Es lo que hacía que un turno de caja pareciera abierto después de sus
-- propias ventas. `AT TIME ZONE 'UTC'` reinterpreta los valores existentes como
-- UTC sin correrles la hora, que es lo correcto porque el contenedor de la API
-- en producción siempre corrió en UTC.
--
-- El código que las escribe ya usa DateTime.UtcNow (AuditHelper y los repos que
-- ponían la fecha a mano). Ver el <remarks> de AuditHelper para el detalle de
-- por qué DateTime.Now era incorrecto incluso en columnas timestamptz.
--
-- NO se toca zlogs_app.raise_date: esa tabla la escribe el sink de Serilog con
-- su propia configuración de columnas, no el código de la aplicación.

BEGIN;

-- v_mermas depende de stock_movements.created, así que Postgres rechaza el
-- ALTER mientras exista. Se recrea al final con su definición exacta (tomada de
-- pg_get_viewdef antes de migrar) MÁS security_invoker y los permisos de
-- app_pos, que un CREATE VIEW pelado no restituye.
--
-- security_invoker=true no es opcional: sin él la vista se evalúa con los
-- permisos de su dueño (postgres, que tiene BYPASSRLS) y la política
-- tenant_aislado de stock_movements deja de aplicarse — cada farmacia vería las
-- mermas de las demás, sin ningún error visible.
DROP VIEW IF EXISTS v_mermas;

ALTER TABLE stock_movements
  ALTER COLUMN created  TYPE timestamptz USING created  AT TIME ZONE 'UTC',
  ALTER COLUMN modified TYPE timestamptz USING modified AT TIME ZONE 'UTC';

ALTER TABLE sale_returns
  ALTER COLUMN return_date TYPE timestamptz USING return_date AT TIME ZONE 'UTC',
  ALTER COLUMN created     TYPE timestamptz USING created     AT TIME ZONE 'UTC',
  ALTER COLUMN modified    TYPE timestamptz USING modified    AT TIME ZONE 'UTC';

ALTER TABLE sale_return_detail
  ALTER COLUMN created  TYPE timestamptz USING created  AT TIME ZONE 'UTC',
  ALTER COLUMN modified TYPE timestamptz USING modified AT TIME ZONE 'UTC';

ALTER TABLE categories
  ALTER COLUMN created  TYPE timestamptz USING created  AT TIME ZONE 'UTC',
  ALTER COLUMN modified TYPE timestamptz USING modified AT TIME ZONE 'UTC';

ALTER TABLE payment_methods
  ALTER COLUMN created  TYPE timestamptz USING created  AT TIME ZONE 'UTC',
  ALTER COLUMN modified TYPE timestamptz USING modified AT TIME ZONE 'UTC';

CREATE VIEW v_mermas WITH (security_invoker = true) AS
 SELECT sm.tenant_id,
    sm.product_id,
    p.product_code,
    p.product_name,
    si.lot_code,
    si.expiry_date,
    abs(sm.quantity) AS cantidad,
    abs(sm.quantity)::numeric * p.sale_price AS valor_perdido,
    sm.reason,
    sm.observation,
    sm.created,
    sm.created_by
   FROM stock_movements sm
     JOIN products p ON p.id = sm.product_id
     LEFT JOIN stock_items si ON si.id = sm.stock_item_id
  WHERE sm.movement_type::text = 'MERMA'::text AND sm.state;

GRANT SELECT, INSERT, UPDATE, DELETE ON v_mermas TO app_pos;

COMMIT;
