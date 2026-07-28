-- ============================================================================
-- Recepción de compras: pendientes, estados derivados, precio real e idempotencia
-- Fecha: 2026-07-28
-- PostgreSQL 16
--
-- Idempotente: se puede ejecutar más de una vez sin efectos secundarios.
-- ============================================================================

BEGIN;

-- ----------------------------------------------------------------------------
-- 1. Corrige las filas afectadas por el bug del id fijo en Guid.Empty.
--    Conserva el histórico (cantidades y movimientos de stock siguen siendo
--    válidos); solo reemplaza el identificador inválido.
-- ----------------------------------------------------------------------------
UPDATE public.purchases_delivery_detail
   SET id = gen_random_uuid()
 WHERE id = '00000000-0000-0000-0000-000000000000';


-- ----------------------------------------------------------------------------
-- 2. Estados de salida de la orden.
--    CANCELADO: la orden nunca se recibió y se anula.
--    CERRADO:   se recibió parcialmente y el proveedor no enviará el resto.
--               Es distinto de "Totalmente Recibido" para no falsear reportes
--               de cumplimiento del proveedor.
-- ----------------------------------------------------------------------------
INSERT INTO public.purchases_status (id, description)
VALUES (4, 'Cancelado'),
       (5, 'Cerrado con Faltante')
    ON CONFLICT (id) DO NOTHING;


-- ----------------------------------------------------------------------------
-- 3. Precio unitario real de la recepción.
--    El pedido guarda el precio pactado (purchases_detail.order_unit_price);
--    la recepción guarda el efectivamente facturado. Sin esta columna no se
--    puede detectar una diferencia de precio ni costear bien el inventario.
-- ----------------------------------------------------------------------------
ALTER TABLE public.purchases_delivery_detail
    ADD COLUMN IF NOT EXISTS unit_price numeric(18,2) DEFAULT 0 NOT NULL;

-- Backfill: para lo ya recibido, el mejor dato disponible es el precio pactado.
UPDATE public.purchases_delivery_detail pdd
   SET unit_price = pd.order_unit_price
  FROM public.purchases_delivery pdl
       INNER JOIN public.purchases_detail pd
               ON pd.purchase_id = pdl.purchase_id
 WHERE pdd.purchase_delivery_id = pdl.id
   AND pd.product_id = pdd.product_id
   AND pdd.unit_price = 0;


-- ----------------------------------------------------------------------------
-- 4. Idempotencia de la recepción.
--    Un doble click o un reintento de red no puede duplicar stock. El front
--    genera un uid por operación; el índice único rechaza el segundo intento.
--    Las filas existentes reciben un uid aleatorio distinto entre sí.
-- ----------------------------------------------------------------------------
ALTER TABLE public.purchases_delivery
    ADD COLUMN IF NOT EXISTS operation_uid uuid NOT NULL DEFAULT gen_random_uuid();

CREATE UNIQUE INDEX IF NOT EXISTS uq_purchases_delivery_operation_uid
    ON public.purchases_delivery (operation_uid);


-- ----------------------------------------------------------------------------
-- 5. Índices para el cálculo de pendientes (se ejecuta en cada recepción).
-- ----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS ix_purchases_delivery_purchase
    ON public.purchases_delivery (purchase_id) WHERE state;

CREATE INDEX IF NOT EXISTS ix_purchases_delivery_detail_delivery
    ON public.purchases_delivery_detail (purchase_delivery_id, product_id) WHERE state;

CREATE INDEX IF NOT EXISTS ix_purchases_detail_purchase
    ON public.purchases_detail (purchase_id) WHERE state;


-- ----------------------------------------------------------------------------
-- 6. Guardas de integridad a nivel de motor (defensa en profundidad).
--    NOT VALID: aplican a filas nuevas sin rechazar el histórico previo a
--    estas reglas. Para validarlas después de auditar los datos:
--        ALTER TABLE public.purchases_delivery_detail
--          VALIDATE CONSTRAINT ck_pdd_delivery_quantity_positive;
-- ----------------------------------------------------------------------------
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                    WHERE conname = 'ck_pdd_delivery_quantity_positive') THEN
        ALTER TABLE public.purchases_delivery_detail
            ADD CONSTRAINT ck_pdd_delivery_quantity_positive
            CHECK (delivery_quantity > 0) NOT VALID;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                    WHERE conname = 'ck_pdd_unit_price_not_negative') THEN
        ALTER TABLE public.purchases_delivery_detail
            ADD CONSTRAINT ck_pdd_unit_price_not_negative
            CHECK (unit_price >= 0) NOT VALID;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                    WHERE conname = 'ck_pd_ordered_quantity_positive') THEN
        ALTER TABLE public.purchases_detail
            ADD CONSTRAINT ck_pd_ordered_quantity_positive
            CHECK (ordered_quantity > 0) NOT VALID;
    END IF;
END $$;


-- ----------------------------------------------------------------------------
-- 6b. Un producto, una línea por orden.
--     Con el mismo producto en dos líneas el pendiente deja de ser atribuible
--     a una línea concreta. El índice solo se crea si los datos actuales ya
--     cumplen la regla; si no, avisa cuáles hay que consolidar a mano.
-- ----------------------------------------------------------------------------
DO $$
DECLARE
    duplicados integer;
BEGIN
    SELECT COUNT(*) INTO duplicados
      FROM (SELECT purchase_id, product_id
              FROM public.purchases_detail
             WHERE state
             GROUP BY purchase_id, product_id
            HAVING COUNT(*) > 1) d;

    IF duplicados > 0 THEN
        RAISE NOTICE 'ATENCION: % combinaciones orden/producto duplicadas. No se creo el indice unico. Revisar con: SELECT purchase_id, product_id, COUNT(*) FROM purchases_detail WHERE state GROUP BY 1,2 HAVING COUNT(*) > 1;', duplicados;
    ELSE
        CREATE UNIQUE INDEX IF NOT EXISTS uq_purchases_detail_purchase_product
            ON public.purchases_detail (purchase_id, product_id) WHERE state;
    END IF;
END $$;


-- ----------------------------------------------------------------------------
-- 7. Resincroniza el acumulado recibido en la línea del pedido.
--    purchases_detail.delivered_quantity es un caché denormalizado: la verdad
--    es el log de recepciones. Esto lo deja consistente con el histórico.
-- ----------------------------------------------------------------------------
UPDATE public.purchases_detail pd
   SET delivered_quantity   = COALESCE(agg.received, 0),
       delivery_final_price = COALESCE(agg.amount, 0),
       delivery_unit_price  = CASE WHEN COALESCE(agg.received, 0) > 0
                                   THEN ROUND(COALESCE(agg.amount, 0) / agg.received, 2)
                                   ELSE 0 END
  FROM (
        SELECT pdl.purchase_id,
               pdd.product_id,
               SUM(pdd.delivery_quantity)                    AS received,
               SUM(pdd.delivery_quantity * pdd.unit_price)   AS amount
          FROM public.purchases_delivery pdl
               INNER JOIN public.purchases_delivery_detail pdd
                       ON pdd.purchase_delivery_id = pdl.id AND pdd.state
         WHERE pdl.state
         GROUP BY pdl.purchase_id, pdd.product_id
       ) agg
 WHERE pd.purchase_id = agg.purchase_id
   AND pd.product_id  = agg.product_id
   AND pd.state;


-- ----------------------------------------------------------------------------
-- 8. Recalcula el estado de las órdenes según el acumulado real.
--    Corrige las que quedaron en "Solicitado" pese a tener recepciones, por el
--    bug de estado derivado. No toca las canceladas ni las cerradas.
-- ----------------------------------------------------------------------------
UPDATE public.purchases p
   SET purchase_status_id = calc.new_status,
       modified           = now()
  FROM (
        SELECT pd.purchase_id,
               CASE
                   WHEN BOOL_AND(pd.delivered_quantity >= pd.ordered_quantity) THEN 3
                   WHEN BOOL_OR(pd.delivered_quantity > 0)                     THEN 2
                   ELSE 1
               END AS new_status
          FROM public.purchases_detail pd
         WHERE pd.state
         GROUP BY pd.purchase_id
       ) calc
 WHERE p.id = calc.purchase_id
   AND p.state
   AND p.purchase_status_id NOT IN (4, 5)
   AND p.purchase_status_id <> calc.new_status;

COMMIT;
