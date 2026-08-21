-- Corrige las ventas que la app móvil guardó 4 horas antes de haber ocurrido.
--
-- Causa: hasta el fix de hoy, `movil/lib/models/sale.dart` mandaba
--   DateFormat('dd/MM/yyyy HH:mm:ss').format(DateTime.now())
-- es decir la hora local de Bolivia SIN marca de zona. El backend la tomaba
-- como si ya fuera UTC, así que la venta quedaba registrada 4 horas antes del
-- instante real. El POS web nunca tuvo el problema: manda toISOString() (UTC
-- explícito, con milisegundos).
--
-- CÓMO SE IDENTIFICAN — dos señales independientes que coinciden en las mismas
-- filas, y ninguna otra:
--   1. `created` lo escribe el servidor en el momento real de la venta y es
--      correcto (el contenedor corre en UTC), así que en las filas afectadas
--      created - sale_date ≈ 4 h exactas. En las sanas la diferencia es ~1 s.
--   2. El móvil formateaba con resolución de segundos, así que `sale_date` no
--      tiene parte fraccionaria; el web sí la tiene.
--
-- No confundir con otros grupos raros de la tabla: hay ventas de 2023 y de
-- 2026-02 con `sale_date` a medianoche y `created` meses después (datos
-- cargados a mano). Su diferencia es de 10 h a 22.000 h, muy lejos del rango de
-- acá, así que quedan fuera.
--
-- IDEMPOTENTE: después de correr, esas filas pasan a tener created - sale_date
-- ≈ 0 y dejan de cumplir el WHERE. Volver a ejecutarlo no mueve nada.
--
-- No se tocan `modified`/`modified_by`: esto es una corrección de datos hecha
-- por migración, no una edición de la venta por parte de un usuario; dejar la
-- auditoría intacta mantiene el rastro de quién la registró de verdad.

BEGIN;

-- Antes: qué se va a mover (queda en el log del deploy).
SELECT count(*) AS ventas_a_corregir
  FROM sales
 WHERE sale_date >= timestamptz '2026-07-03 00:00:00+00'   -- fecha del primer POS móvil
   AND (date_part('microseconds', sale_date)::bigint % 1000000) = 0
   AND EXTRACT(EPOCH FROM (created - sale_date)) BETWEEN 14100 AND 14700;

UPDATE sales
   SET sale_date = sale_date + interval '4 hours'
 WHERE sale_date >= timestamptz '2026-07-03 00:00:00+00'
   AND (date_part('microseconds', sale_date)::bigint % 1000000) = 0
   AND EXTRACT(EPOCH FROM (created - sale_date)) BETWEEN 14100 AND 14700;

-- Después: debe dar 0. Si no da 0, algo no cerró: revisar antes de confirmar.
SELECT count(*) AS deben_quedar_cero
  FROM sales
 WHERE sale_date >= timestamptz '2026-07-03 00:00:00+00'
   AND (date_part('microseconds', sale_date)::bigint % 1000000) = 0
   AND EXTRACT(EPOCH FROM (created - sale_date)) BETWEEN 14100 AND 14700;

COMMIT;
