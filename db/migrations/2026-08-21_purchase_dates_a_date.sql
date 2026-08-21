-- Las fechas de compra pasan de `timestamptz` a `date`.
--
-- POR QUÉ: `purchase_date`, `estimated_delivery_date` y `delivery_date` nunca
-- fueron instantes: son días del calendario (cuándo se emitió la orden, cuándo
-- se espera / se recibió la mercadería). Guardarlas como timestamptz obligaba a
-- escribirlas como "medianoche UTC" y a tratarlas distinto que al resto de las
-- marcas de tiempo: los filtros NO podían convertir de hora boliviana a UTC
-- —hacerlo corría la ventana 4 h y dejaba fuera las filas de ese mismo día— y
-- el frontend necesitaba un `formatDateOnly` aparte para no mostrarlas un día
-- antes. Con `date` la semántica queda en el tipo y la excepción desaparece.
--
-- CONVERSIÓN: se toman los dígitos del día tal como están guardados
-- (`AT TIME ZONE 'UTC'`), NO se convierte a hora boliviana. Es lo correcto
-- porque así fueron escritas: convertir a Bolivia le restaría 4 h a una
-- medianoche UTC y devolvería el día anterior.
--
-- Verificado sobre los datos antes de migrar: todas las filas son medianoche
-- UTC salvo dos (2026-05-23 04:00 y 2026-08-16 19:25), y en esas dos ambas
-- interpretaciones coinciden en el mismo día, así que la regla no las altera.
--
-- Ninguna vista depende de estas columnas (se comprobó en pg_depend), así que
-- no hace falta recrear nada.

BEGIN;

ALTER TABLE purchases
  ALTER COLUMN purchase_date TYPE date
        USING (purchase_date AT TIME ZONE 'UTC')::date,
  ALTER COLUMN estimated_delivery_date TYPE date
        USING (estimated_delivery_date AT TIME ZONE 'UTC')::date;

ALTER TABLE purchases_delivery
  ALTER COLUMN delivery_date TYPE date
        USING (delivery_date AT TIME ZONE 'UTC')::date;

COMMIT;
