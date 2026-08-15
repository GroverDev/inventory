-- =============================================================================
-- Existencias, paso 2: lotes, vencimientos y FEFO
-- =============================================================================
-- El paso 1 creó la estructura y dejó todo en tracking_mode = 'none'. Esto le da
-- uso: recibir por lote, vender por FEFO y ver qué está por vencer.
--
-- Sigue sin cambiar el comportamiento de ningún producto hasta que alguien active
-- el modo 'lot' sobre uno. Con 'none' todo funciona exactamente igual.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Activar el seguimiento por lotes en un producto
-- -----------------------------------------------------------------------------
-- El stock que ya tenía queda como una existencia "sin lote". No se fuerza un
-- recuento ni se pone en cero: son unidades reales que están en el estante.
--
-- FEFO las consume PRIMERO, no último. Es stock viejo del que no se conoce el
-- vencimiento; conviene sacarlo mientras todavía se puede mirar la caja, y no
-- dejarlo arrumbado detrás de lotes que sí están fechados.
CREATE OR REPLACE FUNCTION public.fn_activar_lotes(p_product_id uuid)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_modo text;
BEGIN
    SELECT tracking_mode INTO v_modo FROM public.products WHERE id = p_product_id;

    IF v_modo IS NULL THEN
        RAISE EXCEPTION 'No existe el producto %.', p_product_id;
    END IF;

    IF v_modo = 'serial' THEN
        RAISE EXCEPTION 'El producto ya usa números de serie; no se puede pasar a lotes.';
    END IF;

    UPDATE public.products SET tracking_mode = 'lot', modified = now()
     WHERE id = p_product_id AND tracking_mode <> 'lot';
END $$;

COMMENT ON FUNCTION public.fn_activar_lotes(uuid) IS
    'Pasa un producto a seguimiento por lotes. El stock que ya tenía queda como '
    'existencia sin lote, y FEFO la consume primero por ser la más antigua.';

GRANT EXECUTE ON FUNCTION public.fn_activar_lotes(uuid) TO app_pos;

-- -----------------------------------------------------------------------------
-- 2. Recibir un lote
-- -----------------------------------------------------------------------------
-- La recepción es el único momento en que el lote entra al sistema: es cuando la
-- caja física llega a la farmacia con su etiqueta.
CREATE OR REPLACE FUNCTION public.fn_recibir_lote(
    p_product_id  uuid,
    p_cantidad    numeric,
    p_lot_code    varchar,
    p_expiry_date date    DEFAULT NULL,
    p_user_id     integer DEFAULT 0
)
RETURNS TABLE (stock_item_id uuid, stock_before numeric, stock_after numeric)
LANGUAGE plpgsql
AS $$
DECLARE
    v_item   uuid;
    v_tenant integer;
BEGIN
    IF coalesce(trim(p_lot_code), '') = '' THEN
        RAISE EXCEPTION 'El lote es obligatorio para un producto con seguimiento por lotes.';
    END IF;

    IF p_cantidad <= 0 THEN
        RAISE EXCEPTION 'La cantidad recibida debe ser positiva.';
    END IF;

    -- El tenant sale del producto, no de la sesión. Con RLS activo esta consulta
    -- ya está filtrada, así que solo puede encontrar productos propios; y si la
    -- variable de sesión estuviera mal, el lote igual nace en la farmacia
    -- correcta en vez de con tenant nulo.
    SELECT p.tenant_id INTO v_tenant FROM public.products p WHERE p.id = p_product_id;

    IF v_tenant IS NULL THEN
        RAISE EXCEPTION 'No existe el producto %.', p_product_id;
    END IF;

    -- Recibir el mismo lote dos veces suma sobre la misma existencia. Es lo
    -- habitual: un pedido parcial y su reposición traen el mismo lote.
    SELECT si.id INTO v_item
      FROM public.stock_items si
     WHERE si.product_id = p_product_id
       AND si.lot_code = trim(p_lot_code)
       AND si.expiry_date IS NOT DISTINCT FROM p_expiry_date
       AND si.serial_number IS NULL;

    IF v_item IS NULL THEN
        INSERT INTO public.stock_items
            (tenant_id, product_id, lot_code, expiry_date, quantity, created_by, modified_by)
        VALUES (v_tenant, p_product_id, trim(p_lot_code), p_expiry_date, 0, p_user_id, p_user_id)
        RETURNING id INTO v_item;
    END IF;

    RETURN QUERY SELECT * FROM public.fn_mover_stock(p_product_id, p_cantidad, p_user_id, v_item);
END $$;

COMMENT ON FUNCTION public.fn_recibir_lote(uuid, numeric, varchar, date, integer) IS
    'Da entrada a un lote. Si ya existe esa combinación de lote y vencimiento, suma '
    'sobre la misma existencia en vez de duplicarla.';

GRANT EXECUTE ON FUNCTION public.fn_recibir_lote(uuid, numeric, varchar, date, integer) TO app_pos;

-- -----------------------------------------------------------------------------
-- 3. FEFO: qué existencias consumir y en qué orden
-- -----------------------------------------------------------------------------
-- First Expired, First Out. Devuelve el reparto de una cantidad entre las
-- existencias disponibles, sin tocar nada: quien vende decide qué hacer con el
-- resultado. Separar el cálculo del efecto permite mostrarle al cajero de qué
-- lotes va a salir antes de confirmar.
--
-- El orden es:
--   1. Sin vencimiento conocido (el stock heredado), lo más antiguo
--   2. Por vencimiento más próximo
--   3. Por antigüedad de la existencia, para desempatar de forma estable
CREATE OR REPLACE FUNCTION public.fn_asignar_fefo(
    p_product_id uuid,
    p_cantidad   numeric
)
RETURNS TABLE (stock_item_id uuid, lot_code varchar, expiry_date date, cantidad numeric)
LANGUAGE plpgsql
AS $$
DECLARE
    v_disponible numeric;
    v_modo       text;
    v_implicita  uuid;
BEGIN
    IF p_cantidad <= 0 THEN
        RAISE EXCEPTION 'La cantidad a asignar debe ser positiva.';
    END IF;

    SELECT p.tracking_mode INTO v_modo FROM public.products p WHERE p.id = p_product_id;

    IF v_modo IS NULL THEN
        RAISE EXCEPTION 'No existe el producto %.', p_product_id;
    END IF;

    SELECT COALESCE(sum(si.quantity), 0) INTO v_disponible
      FROM public.stock_items si
     WHERE si.product_id = p_product_id AND si.state AND si.quantity > 0;

    IF v_disponible < p_cantidad THEN
        -- Sin seguimiento, vender más de lo registrado sigue permitido: es lo que
        -- el sistema hace hoy, y hay productos con saldo negativo que lo prueban.
        -- Cambiar esa política es una decisión de negocio, no de este script.
        IF v_modo = 'none' THEN
            SELECT si.id INTO v_implicita
              FROM public.stock_items si
             WHERE si.product_id = p_product_id
               AND si.lot_code IS NULL AND si.serial_number IS NULL AND si.state
             LIMIT 1;

            IF v_implicita IS NULL THEN
                RAISE EXCEPTION 'El producto % no tiene existencia sin lote.', p_product_id;
            END IF;

            RETURN QUERY SELECT v_implicita, NULL::varchar, NULL::date, p_cantidad;
            RETURN;
        END IF;

        -- Con lotes sí se rechaza: vender de un lote que no se recibió no
        -- significa nada, y rompería la trazabilidad que justifica todo esto.
        RAISE EXCEPTION 'Stock insuficiente: se piden % y hay %.', p_cantidad, v_disponible;
    END IF;

    RETURN QUERY
    WITH ordenadas AS (
        SELECT si.id, si.lot_code, si.expiry_date, si.quantity,
               COALESCE(sum(si.quantity) OVER (
                   ORDER BY si.expiry_date NULLS FIRST, si.created, si.id
                   ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING), 0) AS acumulado_previo
          FROM public.stock_items si
         WHERE si.product_id = p_product_id AND si.state AND si.quantity > 0
    )
    SELECT o.id, o.lot_code, o.expiry_date,
           LEAST(o.quantity, p_cantidad - o.acumulado_previo)
      FROM ordenadas o
     WHERE o.acumulado_previo < p_cantidad
     ORDER BY o.expiry_date NULLS FIRST, o.id;
END $$;

COMMENT ON FUNCTION public.fn_asignar_fefo(uuid, numeric) IS
    'Reparte una cantidad entre las existencias disponibles por vencimiento más '
    'próximo (FEFO). No modifica nada: solo calcula el reparto.';

GRANT EXECUTE ON FUNCTION public.fn_asignar_fefo(uuid, numeric) TO app_pos;

-- -----------------------------------------------------------------------------
-- 4. Qué está por vencer
-- -----------------------------------------------------------------------------
-- security_invoker: sin esto la vista se salta RLS y expone los lotes de todas
-- las farmacias. Las vistas corren con los privilegios de su dueño salvo que se
-- indique lo contrario.
CREATE OR REPLACE VIEW public.v_stock_por_vencer
    WITH (security_invoker = true) AS
SELECT si.tenant_id,
       si.id            AS stock_item_id,
       p.id             AS product_id,
       p.product_code,
       p.product_name,
       si.lot_code,
       si.expiry_date,
       si.quantity,
       (si.expiry_date - CURRENT_DATE) AS dias_restantes,
       CASE
           WHEN si.expiry_date <  CURRENT_DATE                     THEN 'VENCIDO'
           WHEN si.expiry_date <= CURRENT_DATE + 30                THEN 'CRITICO'
           WHEN si.expiry_date <= CURRENT_DATE + 90                THEN 'PROXIMO'
           ELSE 'VIGENTE'
       END AS estado,
       si.quantity * p.sale_price AS valor_en_riesgo
  FROM public.stock_items si
  JOIN public.products p ON p.id = si.product_id
 WHERE si.state AND si.quantity > 0 AND si.expiry_date IS NOT NULL;

COMMENT ON VIEW public.v_stock_por_vencer IS
    'Existencias con vencimiento, clasificadas por urgencia. valor_en_riesgo es a '
    'precio de venta: es lo que se pierde si no se rota a tiempo.';

GRANT SELECT ON public.v_stock_por_vencer TO app_pos;

-- -----------------------------------------------------------------------------
-- 5. Trazabilidad de un lote
-- -----------------------------------------------------------------------------
-- El caso que justifica todo esto: el laboratorio retira un lote y hay que saber
-- si se compró y a quién se le vendió.
CREATE OR REPLACE VIEW public.v_trazabilidad_lote
    WITH (security_invoker = true) AS
SELECT si.tenant_id,
       si.lot_code,
       si.expiry_date,
       p.product_code,
       p.product_name,
       s.id            AS sale_id,
       s.sale_date,
       c.full_name     AS cliente,
       c.document_number,
       c.cellphone,
       sd.quantity
  FROM public.stock_items si
  JOIN public.products     p  ON p.id  = si.product_id
  JOIN public.sales_detail sd ON sd.stock_item_id = si.id
  JOIN public.sales        s  ON s.id  = sd.sale_id
  JOIN public.customers    c  ON c.id  = s.customer_id
 WHERE si.lot_code IS NOT NULL AND sd.state AND s.state;

COMMENT ON VIEW public.v_trazabilidad_lote IS
    'A quién se le vendió cada lote. Es lo que permite responder a un retiro de '
    'mercado del laboratorio.';

GRANT SELECT ON public.v_trazabilidad_lote TO app_pos;

COMMIT;
