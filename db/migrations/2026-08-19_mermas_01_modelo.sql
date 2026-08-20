-- -----------------------------------------------------------------------------
-- Mermas por vencimiento: reporte de cuánto se perdió
-- -----------------------------------------------------------------------------
-- Hasta ahora no había forma de registrar ni de reportar una baja de stock por
-- vencimiento de forma distinguible: el único camino era el ajuste genérico
-- (MovementType = 'AJUSTE'), con el motivo como texto libre. El backend agrega
-- un nuevo tipo de movimiento, 'MERMA', emitido por
-- StockMovementRepository.CreateWriteOff (POST api/StockMovement/write-off).
-- No hace falta migrar ningún CHECK constraint: movement_type siempre fue un
-- string libre (VENTA/COMPRA/AJUSTE/DEVOLUCION), sin catálogo.
--
-- security_invoker = true, igual que v_stock_por_vencer/v_trazabilidad_lote:
-- sin esto la vista correría con los privilegios de su dueño (postgres) y se
-- saltaría RLS, mostrando mermas de todas las farmacias.
--
-- Idempotente.

CREATE OR REPLACE VIEW public.v_mermas
    WITH (security_invoker = true) AS
SELECT sm.tenant_id,
       sm.product_id,
       p.product_code,
       p.product_name,
       si.lot_code,
       si.expiry_date,
       abs(sm.quantity)               AS cantidad,
       -- A precio de venta, mismo criterio que v_stock_por_vencer.valor_en_riesgo:
       -- no hay costo de compra cacheado en products para valorar a costo.
       abs(sm.quantity) * p.sale_price AS valor_perdido,
       sm.reason,
       sm.observation,
       sm.created,
       sm.created_by
  FROM public.stock_movements sm
  JOIN public.products p ON p.id = sm.product_id
  LEFT JOIN public.stock_items si ON si.id = sm.stock_item_id
 WHERE sm.movement_type = 'MERMA' AND sm.state;

COMMENT ON VIEW public.v_mermas IS
    'Bajas de stock por vencimiento/pérdida (movement_type=MERMA). valor_perdido '
    'es a precio de venta, mismo criterio que v_stock_por_vencer.valor_en_riesgo.';

GRANT SELECT ON public.v_mermas TO app_pos;
