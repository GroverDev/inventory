-- =============================================================================
-- Existencias: el stock deja de ser un número y pasa a ser un saldo por unidad
-- =============================================================================
-- Hoy el stock de un producto es un escalar, products.current_stock. Eso alcanza
-- para una ferretería y no alcanza para una farmacia: no permite FEFO, ni avisar
-- de lo que vence, ni responder a un retiro de lote del laboratorio.
--
-- Este script NO activa nada. Crea el modelo general y deja a todos los productos
-- en tracking_mode = 'none', que es el caso degenerado: exactamente una existencia
-- implícita por producto. La aplicación se comporta igual que antes.
--
-- Se hace ahora, con un solo cliente y 1.180 productos, porque es la única
-- migración del proyecto que se encarece sola: cada venta registrada agrega
-- filas a sales_detail y stock_movements que habrá que reasignar.
--
-- El paso 2 —capturar lote en la recepción, elegir por FEFO al vender, avisar de
-- vencimientos— es funcionalidad y puede esperar. Esto es estructura y no.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Cómo se identifica el stock de cada producto
-- -----------------------------------------------------------------------------
-- Los tres modos son ortogonales al rubro, y por eso el mismo sistema sirve para
-- una farmacia y para una ferretería:
--   none    una sola existencia implícita. Ferretería, librería.
--   lot     lote + vencimiento. Farmacia, perecederos, cosmética.
--   serial  una fila por unidad. Electrónica, herramienta con garantía.
ALTER TABLE public.products
    ADD COLUMN IF NOT EXISTS tracking_mode varchar(10) NOT NULL DEFAULT 'none';

ALTER TABLE public.products DROP CONSTRAINT IF EXISTS products_tracking_mode_check;
ALTER TABLE public.products ADD CONSTRAINT products_tracking_mode_check
    CHECK (tracking_mode IN ('none', 'lot', 'serial'));

COMMENT ON COLUMN public.products.tracking_mode IS
    'Cómo se identifica el stock: none (una existencia implícita), lot (lote y '
    'vencimiento) o serial (una fila por unidad).';

-- -----------------------------------------------------------------------------
-- 2. Las existencias
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.stock_items (
    id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id     integer     NOT NULL DEFAULT public.current_tenant(),
    product_id    uuid        NOT NULL,

    -- Nulos cuando tracking_mode = 'none'. Es lo que hace que el caso genérico
    -- sea el caso degenerado del modelo general, y no una rama aparte.
    lot_code      varchar(50),
    expiry_date   date,
    serial_number varchar(80),

    -- numeric, no integer: el fraccionamiento (vender blísters sueltos de una
    -- caja) necesita decimales, y unit_of_measurement ya tiene proportion y
    -- precision_rounding preparados. Definirlo bien ahora evita migrar de nuevo.
    quantity      numeric(14,4) NOT NULL DEFAULT 0,

    state         boolean     NOT NULL DEFAULT true,
    created_by    integer     NOT NULL DEFAULT 0,
    created       timestamptz NOT NULL DEFAULT now(),
    modified_by   integer     NOT NULL DEFAULT 0,
    modified      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT stock_items_tenant_id_uk UNIQUE (tenant_id, id),
    CONSTRAINT fk_stock_items_product
        FOREIGN KEY (tenant_id, product_id) REFERENCES public.products (tenant_id, id)

    -- A propósito NO hay CHECK (quantity >= 0). La base tiene hoy cuatro productos
    -- con stock negativo y sin movimientos que lo expliquen, de antes del libro
    -- mayor. Rechazarlos abortaría la migración, y forzarlos a cero escondería un
    -- dato real.
    --
    -- Impedir la venta sin stock es una decisión de producto, no de esquema:
    -- muchos puntos de venta la permiten a propósito. Cuando se active el modo
    -- 'lot' conviene revisarla, porque un lote concreto en negativo sí es un
    -- absurdo: significaría haber vendido de un lote que nunca se recibió.
);

COMMENT ON TABLE public.stock_items IS
    'Saldo de stock por unidad identificable. Con tracking_mode = none hay una sola '
    'fila por producto y el comportamiento es idéntico al del stock escalar.';

-- Una existencia por combinación de lote y vencimiento. NULLS NOT DISTINCT hace
-- que la fila "sin lote" sea única por producto, que es lo que necesita el modo
-- none; sin eso, PostgreSQL trataría cada NULL como distinto y admitiría
-- duplicados silenciosos.
CREATE UNIQUE INDEX IF NOT EXISTS stock_items_lote_uk
    ON public.stock_items (tenant_id, product_id, lot_code, expiry_date)
    NULLS NOT DISTINCT
    WHERE serial_number IS NULL;

-- Un número de serie no se repite dentro de una farmacia.
CREATE UNIQUE INDEX IF NOT EXISTS stock_items_serie_uk
    ON public.stock_items (tenant_id, serial_number)
    WHERE serial_number IS NOT NULL;

-- Ruta caliente del FEFO: lo que vence primero, primero.
CREATE INDEX IF NOT EXISTS stock_items_fefo
    ON public.stock_items (tenant_id, product_id, expiry_date NULLS LAST)
    WHERE state AND quantity > 0;

-- Para el reporte de próximos a vencer.
CREATE INDEX IF NOT EXISTS stock_items_vencimiento
    ON public.stock_items (tenant_id, expiry_date)
    WHERE state AND quantity > 0 AND expiry_date IS NOT NULL;

ALTER TABLE public.stock_items ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.stock_items FORCE  ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_aislado ON public.stock_items;
CREATE POLICY tenant_aislado ON public.stock_items
    USING      (tenant_id = public.current_tenant())
    WITH CHECK (tenant_id = public.current_tenant());

-- -----------------------------------------------------------------------------
-- 3. Una existencia implícita por producto, con el saldo actual
-- -----------------------------------------------------------------------------
INSERT INTO public.stock_items (tenant_id, product_id, quantity, created_by, modified_by)
SELECT p.tenant_id, p.id, COALESCE(p.current_stock, 0), 0, 0
  FROM public.products p
 WHERE NOT EXISTS (
       SELECT 1 FROM public.stock_items si
        WHERE si.product_id = p.id AND si.tenant_id = p.tenant_id);

-- -----------------------------------------------------------------------------
-- 4. El histórico se ata a esa existencia
-- -----------------------------------------------------------------------------
-- Reasignar filas viejas es barato hoy y caro más adelante: es exactamente el
-- costo que crece con cada venta que se registre de aquí en más.
ALTER TABLE public.stock_movements ADD COLUMN IF NOT EXISTS stock_item_id uuid;
ALTER TABLE public.sales_detail    ADD COLUMN IF NOT EXISTS stock_item_id uuid;

UPDATE public.stock_movements m
   SET stock_item_id = si.id
  FROM public.stock_items si
 WHERE si.product_id = m.product_id AND si.tenant_id = m.tenant_id
   AND m.stock_item_id IS NULL;

UPDATE public.sales_detail d
   SET stock_item_id = si.id
  FROM public.stock_items si
 WHERE si.product_id = d.product_id AND si.tenant_id = d.tenant_id
   AND d.stock_item_id IS NULL;

-- Quedan NOT NULL: de acá en adelante todo movimiento y toda línea de venta
-- salen de una existencia concreta.
ALTER TABLE public.stock_movements ALTER COLUMN stock_item_id SET NOT NULL;
ALTER TABLE public.sales_detail    ALTER COLUMN stock_item_id SET NOT NULL;

ALTER TABLE public.stock_movements DROP CONSTRAINT IF EXISTS fk_stock_movements_stock_item;
ALTER TABLE public.stock_movements ADD CONSTRAINT fk_stock_movements_stock_item
    FOREIGN KEY (tenant_id, stock_item_id) REFERENCES public.stock_items (tenant_id, id);

ALTER TABLE public.sales_detail DROP CONSTRAINT IF EXISTS fk_sales_detail_stock_item;
ALTER TABLE public.sales_detail ADD CONSTRAINT fk_sales_detail_stock_item
    FOREIGN KEY (tenant_id, stock_item_id) REFERENCES public.stock_items (tenant_id, id);

CREATE INDEX IF NOT EXISTS idx_stock_movements_item
    ON public.stock_movements (tenant_id, stock_item_id, created DESC);

-- -----------------------------------------------------------------------------
-- 5. products.current_stock pasa a ser una caché
-- -----------------------------------------------------------------------------
-- La verdad está en stock_items. current_stock se mantiene porque lo leen unos
-- veinte lugares del backend —listados, dashboard, reportes— y reescribirlos
-- todos ahora sería mezclar dos cambios. La función de abajo permite verificar
-- que coinciden, y repararlo si no.
CREATE OR REPLACE FUNCTION public.fn_recalcular_stock_producto(p_product_id uuid)
RETURNS numeric
LANGUAGE plpgsql
AS $$
DECLARE
    v_total numeric;
BEGIN
    SELECT COALESCE(sum(quantity), 0) INTO v_total
      FROM public.stock_items
     WHERE product_id = p_product_id AND state;

    UPDATE public.products
       SET current_stock = v_total, modified = now()
     WHERE id = p_product_id;

    RETURN v_total;
END $$;

COMMENT ON FUNCTION public.fn_recalcular_stock_producto(uuid) IS
    'Recalcula products.current_stock a partir de stock_items, que es la fuente de '
    'verdad. Sirve para verificar y para reparar desvíos.';

GRANT EXECUTE ON FUNCTION public.fn_recalcular_stock_producto(uuid) TO app_pos;

-- -----------------------------------------------------------------------------
-- 6. El único camino para mover stock
-- -----------------------------------------------------------------------------
-- Los cuatro lugares que mueven stock —venta, recepción, devolución y ajuste—
-- repetían el mismo baile: leer current_stock, actualizarlo, calcular antes y
-- después, insertar el movimiento. Cuatro copias de la misma lógica, y ahora
-- además habría que acordarse de tocar stock_items en todas.
--
-- Concentrarlo acá hace que la caché y el libro mayor no puedan separarse: es
-- la misma idea que RLS, mover la garantía de la disciplina a la estructura.
--
-- No es SECURITY DEFINER a propósito: corre con los permisos de quien llama, así
-- que RLS le impide mover el stock de otra farmacia.
CREATE OR REPLACE FUNCTION public.fn_mover_stock(
    p_product_id    uuid,
    p_delta         numeric,
    p_user_id       integer DEFAULT 0,
    p_stock_item_id uuid    DEFAULT NULL
)
RETURNS TABLE (stock_item_id uuid, stock_before numeric, stock_after numeric)
LANGUAGE plpgsql
AS $$
DECLARE
    v_item    uuid;
    v_antes   numeric;
    v_despues numeric;
BEGIN
    IF p_stock_item_id IS NOT NULL THEN
        v_item := p_stock_item_id;
    ELSE
        -- Sin existencia indicada se usa la implícita, que es la única que tiene
        -- un producto en modo 'none'. Cuando se active 'lot', quien venda deberá
        -- elegir el lote (por FEFO) y pasarlo explícitamente.
        SELECT si.id INTO v_item
          FROM public.stock_items si
         WHERE si.product_id = p_product_id
           AND si.lot_code IS NULL
           AND si.serial_number IS NULL
           AND si.state
         LIMIT 1;

        IF v_item IS NULL THEN
            RAISE EXCEPTION 'El producto % no tiene una existencia sin lote. Si usa lotes, '
                            'hay que indicar cuál mover.', p_product_id;
        END IF;
    END IF;

    UPDATE public.stock_items si
       SET quantity    = si.quantity + p_delta,
           modified_by = p_user_id,
           modified    = now()
     WHERE si.id = v_item
    RETURNING si.quantity - p_delta, si.quantity INTO v_antes, v_despues;

    IF v_antes IS NULL THEN
        RAISE EXCEPTION 'No se encontró la existencia %.', v_item;
    END IF;

    -- La caché se mantiene acá, no en el backend. Es lo que garantiza que
    -- v_stock_descuadrado siga vacía.
    UPDATE public.products p
       SET current_stock = COALESCE(p.current_stock, 0) + p_delta,
           modified_by   = p_user_id,
           modified      = now()
     WHERE p.id = p_product_id;

    RETURN QUERY SELECT v_item, v_antes, v_despues;
END $$;

COMMENT ON FUNCTION public.fn_mover_stock(uuid, numeric, integer, uuid) IS
    'Único camino para mover stock. Actualiza la existencia y la caché de products '
    'en una sola operación, y devuelve la existencia afectada con su saldo antes y '
    'después, para registrar el movimiento.';

GRANT EXECUTE ON FUNCTION public.fn_mover_stock(uuid, numeric, integer, uuid) TO app_pos;

-- Discrepancias entre la caché y el saldo real. Debe devolver cero filas.
CREATE OR REPLACE VIEW public.v_stock_descuadrado AS
SELECT p.tenant_id,
       p.id AS product_id,
       p.product_name,
       p.current_stock                    AS cache,
       COALESCE(sum(si.quantity), 0)      AS real,
       p.current_stock - COALESCE(sum(si.quantity), 0) AS diferencia
  FROM public.products p
  LEFT JOIN public.stock_items si ON si.product_id = p.id AND si.state
 GROUP BY p.tenant_id, p.id, p.product_name, p.current_stock
HAVING p.current_stock IS DISTINCT FROM COALESCE(sum(si.quantity), 0);

COMMENT ON VIEW public.v_stock_descuadrado IS
    'Productos donde products.current_stock no coincide con la suma de sus existencias. '
    'Debe estar vacía; si no, algo escribió el stock sin pasar por el libro mayor.';

GRANT SELECT ON public.v_stock_descuadrado TO app_pos;

COMMIT;
