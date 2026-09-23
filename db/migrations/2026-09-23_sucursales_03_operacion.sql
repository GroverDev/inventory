-- =============================================================================
-- Sucursales, paso 3: existencias, ventas, caja y compras por sucursal
-- =============================================================================
-- El paso 1 dejó cada fila operativa marcada con su sucursal. Este paso hace que
-- esa marca signifique algo:
--
--   * El stock es por sucursal. Cada sucursal tiene sus propias existencias
--     (la implícita de un producto sin seguimiento, y sus lotes); las funciones
--     de stock trabajan sobre la sucursal de la sesión.
--   * products.current_stock sigue existiendo, como total de la farmacia. Lo que
--     se muestra al vender es el stock de la sucursal: v_stock_actual.
--   * Caja: una abierta por usuario y sucursal, en vez de una por usuario.
--   * Las reglas de "misma sucursal" van en la estructura, con claves foráneas
--     compuestas, igual que el aislamiento por tenant (ver
--     2026-08-14_multitenant_06_fk_compuestas.sql):
--       - una venta, un movimiento de caja o una devolución solo pueden usar una
--         caja de su sucursal;
--       - un movimiento de stock solo puede tocar una existencia de su sucursal;
--       - una devolución se registra en la sucursal de la venta;
--       - una recepción, en la sucursal del pedido.
--
-- Requiere el paso 1. Idempotente.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Unicidad de existencias, ahora por sucursal
-- -----------------------------------------------------------------------------
-- Cada sucursal tiene su propia existencia implícita por producto y su propio
-- registro de cada lote: el mismo lote recibido en dos sucursales son dos
-- existencias. El número de serie sigue siendo único en la farmacia: es una
-- unidad física, está en una sola sucursal a la vez.
DROP INDEX IF EXISTS public.stock_items_lote_uk;
CREATE UNIQUE INDEX stock_items_lote_uk
    ON public.stock_items (tenant_id, branch_id, product_id, lot_code, expiry_date)
    NULLS NOT DISTINCT
    WHERE serial_number IS NULL;

DROP INDEX IF EXISTS public.stock_items_fefo;
CREATE INDEX stock_items_fefo
    ON public.stock_items (tenant_id, branch_id, product_id, expiry_date)
    WHERE state AND quantity > 0;

-- Para v_stock_actual, que se une a cada listado de productos.
CREATE INDEX IF NOT EXISTS stock_items_sucursal_producto
    ON public.stock_items (branch_id, product_id) WHERE state;

-- -----------------------------------------------------------------------------
-- 2. Caja: una abierta por usuario y sucursal
-- -----------------------------------------------------------------------------
DROP INDEX IF EXISTS public.uix_cash_sessions_user_open;
CREATE UNIQUE INDEX uix_cash_sessions_user_open
    ON public.cash_sessions (user_id, branch_id)
    WHERE closed_at IS NULL AND state = true;

-- -----------------------------------------------------------------------------
-- 3. Reglas de "misma sucursal"
-- -----------------------------------------------------------------------------
-- Destinos de las FK: UNIQUE (tenant_id, branch_id, id). Redundante respecto de
-- la PK, pero PostgreSQL lo exige como destino de una FK compuesta.
DO $$
DECLARE
    t text;
BEGIN
    FOREACH t IN ARRAY ARRAY['cash_sessions', 'stock_items', 'sales', 'purchases'] LOOP
        IF NOT EXISTS (SELECT 1 FROM pg_constraint
                        WHERE conname = format('%s_sucursal_uk', t)
                          AND conrelid = format('public.%I', t)::regclass) THEN
            EXECUTE format('ALTER TABLE public.%I ADD CONSTRAINT %I UNIQUE (tenant_id, branch_id, id)',
                           t, format('%s_sucursal_uk', t));
        END IF;
    END LOOP;
END $$;

-- Son adicionales a las FK existentes, no las reemplazan. MATCH SIMPLE (el
-- default): si la columna referida es nula —una venta sin caja— no se verifica.
DO $$
DECLARE
    -- {constraint, tabla_hija, columna_hija, tabla_padre}
    fks text[][] := ARRAY[
        ARRAY['sales_caja_misma_sucursal',              'sales',              'cash_session_id', 'cash_sessions'],
        ARRAY['cash_movements_caja_misma_sucursal',     'cash_movements',     'cash_session_id', 'cash_sessions'],
        ARRAY['sale_returns_caja_misma_sucursal',       'sale_returns',       'cash_session_id', 'cash_sessions'],
        ARRAY['sale_returns_venta_misma_sucursal',      'sale_returns',       'sale_id',         'sales'],
        ARRAY['stock_movements_existencia_misma_sucursal','stock_movements',  'stock_item_id',   'stock_items'],
        ARRAY['purchases_delivery_pedido_misma_sucursal','purchases_delivery','purchase_id',     'purchases']
    ];
    i integer;
BEGIN
    FOR i IN 1 .. array_length(fks, 1) LOOP
        IF NOT EXISTS (SELECT 1 FROM pg_constraint
                        WHERE conname = fks[i][1]
                          AND conrelid = format('public.%I', fks[i][2])::regclass) THEN
            EXECUTE format(
                'ALTER TABLE public.%I ADD CONSTRAINT %I FOREIGN KEY (tenant_id, branch_id, %I) '
                'REFERENCES public.%I (tenant_id, branch_id, id)',
                fks[i][2], fks[i][1], fks[i][3], fks[i][4]);
        END IF;
    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- 4. Funciones de stock por sucursal
-- -----------------------------------------------------------------------------
-- Todas reciben p_branch_id con DEFAULT current_branch(): los llamadores de hoy
-- no cambian, y los traspasos (paso 4) podrán mover stock en otra sucursal
-- dentro de la misma transacción.
--
-- Cambia la lista de argumentos, así que hay que borrar las versiones viejas:
-- CREATE OR REPLACE con otros argumentos crearía una sobrecarga, y las llamadas
-- con argumentos por defecto quedarían ambiguas.
DROP FUNCTION IF EXISTS public.fn_mover_stock(uuid, numeric, integer, uuid);
DROP FUNCTION IF EXISTS public.fn_asignar_fefo(uuid, numeric);
DROP FUNCTION IF EXISTS public.fn_recibir_lote(uuid, numeric, varchar, date, integer);
DROP FUNCTION IF EXISTS public.fn_recibir_serie(uuid, varchar, date, integer);

-- Sucursal efectiva: la indicada, o la de la sesión. Sin ninguna de las dos no
-- hay dónde registrar el movimiento.
CREATE OR REPLACE FUNCTION public.fn_sucursal_requerida(p_branch_id uuid)
RETURNS uuid
LANGUAGE plpgsql STABLE
AS $$
BEGIN
    IF coalesce(p_branch_id, public.current_branch()) IS NULL THEN
        RAISE EXCEPTION 'No hay sucursal en la sesión: no se sabe dónde registrar el movimiento de stock.';
    END IF;
    RETURN coalesce(p_branch_id, public.current_branch());
END $$;

-- La existencia implícita (sin lote ni serie) de un producto en una sucursal.
-- Se crea al primer movimiento: una sucursal nueva, o un producto dado de alta
-- después, no tienen ninguna hasta que algo entra o sale.
--
-- Solo para productos sin seguimiento. Con lotes o series, crear una existencia
-- sin identificar sería justamente lo que el seguimiento prohíbe; ahí devuelve
-- la heredada si existe (stock anterior a activar lotes) o NULL.
CREATE OR REPLACE FUNCTION public.fn_existencia_implicita(p_product_id uuid, p_branch_id uuid, p_user_id integer DEFAULT 0)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    v_item   uuid;
    v_modo   text;
    v_tenant integer;
BEGIN
    SELECT si.id INTO v_item
      FROM public.stock_items si
     WHERE si.product_id = p_product_id
       AND si.branch_id  = p_branch_id
       AND si.lot_code IS NULL AND si.serial_number IS NULL AND si.state
     LIMIT 1;

    IF v_item IS NOT NULL THEN
        RETURN v_item;
    END IF;

    SELECT p.tracking_mode, p.tenant_id INTO v_modo, v_tenant
      FROM public.products p WHERE p.id = p_product_id;

    IF v_modo IS NULL THEN
        RAISE EXCEPTION 'No existe el producto %.', p_product_id;
    END IF;

    IF v_modo <> 'none' THEN
        RETURN NULL;
    END IF;

    -- ON CONFLICT: dos ventas simultáneas del primer movimiento en la sucursal
    -- intentarían crearla a la vez. La segunda encuentra la de la primera.
    INSERT INTO public.stock_items
        (tenant_id, branch_id, product_id, quantity, created_by, modified_by)
    VALUES (v_tenant, p_branch_id, p_product_id, 0, p_user_id, p_user_id)
    ON CONFLICT (tenant_id, branch_id, product_id, lot_code, expiry_date)
        WHERE serial_number IS NULL
        DO NOTHING
    RETURNING id INTO v_item;

    IF v_item IS NULL THEN
        SELECT si.id INTO v_item
          FROM public.stock_items si
         WHERE si.product_id = p_product_id
           AND si.branch_id  = p_branch_id
           AND si.lot_code IS NULL AND si.serial_number IS NULL;
    END IF;

    RETURN v_item;
END $$;

COMMENT ON FUNCTION public.fn_existencia_implicita(uuid, uuid, integer) IS
    'Existencia sin lote de un producto en una sucursal; la crea si el producto no '
    'tiene seguimiento y todavía no existe. NULL si el producto usa lotes o series y '
    'no tiene existencia heredada.';

CREATE OR REPLACE FUNCTION public.fn_mover_stock(
    p_product_id    uuid,
    p_delta         numeric,
    p_user_id       integer DEFAULT 0,
    p_stock_item_id uuid    DEFAULT NULL,
    p_branch_id     uuid    DEFAULT NULL
)
RETURNS TABLE (stock_item_id uuid, stock_before numeric, stock_after numeric)
LANGUAGE plpgsql
AS $$
DECLARE
    v_branch  uuid := public.fn_sucursal_requerida(p_branch_id);
    v_item    uuid;
    v_suc     uuid;
    v_antes   numeric;
    v_despues numeric;
BEGIN
    IF p_stock_item_id IS NOT NULL THEN
        v_item := p_stock_item_id;

        -- Una existencia indicada tiene que ser de la sucursal donde se opera:
        -- vender o devolver sobre el lote de otra sucursal movería stock que
        -- está físicamente en otro lugar.
        SELECT si.branch_id INTO v_suc FROM public.stock_items si WHERE si.id = v_item;
        IF v_suc IS NOT NULL AND v_suc <> v_branch THEN
            RAISE EXCEPTION 'La existencia % pertenece a otra sucursal.', v_item;
        END IF;
    ELSE
        v_item := public.fn_existencia_implicita(p_product_id, v_branch, p_user_id);

        IF v_item IS NULL THEN
            RAISE EXCEPTION 'El producto % no tiene una existencia sin lote en esta sucursal. Si usa lotes, '
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

    -- La caché es el total de la farmacia, sumando todas las sucursales. La
    -- mantiene esta función, no el backend: es lo que garantiza que
    -- v_stock_descuadrado siga vacía.
    UPDATE public.products p
       SET current_stock = COALESCE(p.current_stock, 0) + p_delta,
           modified_by   = p_user_id,
           modified      = now()
     WHERE p.id = p_product_id;

    RETURN QUERY SELECT v_item, v_antes, v_despues;
END $$;

COMMENT ON FUNCTION public.fn_mover_stock(uuid, numeric, integer, uuid, uuid) IS
    'Único camino para mover stock. Opera en la sucursal indicada o en la de la sesión; '
    'actualiza la existencia y el total de products en una sola operación, y devuelve '
    'la existencia afectada con su saldo antes y después.';

CREATE OR REPLACE FUNCTION public.fn_asignar_fefo(
    p_product_id uuid,
    p_cantidad   numeric,
    p_branch_id  uuid DEFAULT NULL
)
RETURNS TABLE (stock_item_id uuid, lot_code varchar, expiry_date date, cantidad numeric)
LANGUAGE plpgsql
AS $$
DECLARE
    v_branch     uuid := public.fn_sucursal_requerida(p_branch_id);
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

    -- Solo lo que hay en esta sucursal: el stock de otra no se puede entregar
    -- desde el mostrador de esta.
    SELECT COALESCE(sum(si.quantity), 0) INTO v_disponible
      FROM public.stock_items si
     WHERE si.product_id = p_product_id AND si.branch_id = v_branch
       AND si.state AND si.quantity > 0;

    IF v_disponible < p_cantidad THEN
        -- Sin seguimiento, vender más de lo registrado sigue permitido: es lo que
        -- el sistema hace hoy, y hay productos con saldo negativo que lo prueban.
        -- Cambiar esa política es una decisión de negocio, no de este script.
        IF v_modo = 'none' THEN
            v_implicita := public.fn_existencia_implicita(p_product_id, v_branch);
            RETURN QUERY SELECT v_implicita, NULL::varchar, NULL::date, p_cantidad;
            RETURN;
        END IF;

        -- Con lotes sí se rechaza: vender de un lote que no se recibió no
        -- significa nada, y rompería la trazabilidad que justifica todo esto.
        RAISE EXCEPTION 'Stock insuficiente en esta sucursal: se piden % y hay %.', p_cantidad, v_disponible;
    END IF;

    RETURN QUERY
    WITH ordenadas AS (
        SELECT si.id, si.lot_code, si.expiry_date, si.quantity,
               COALESCE(sum(si.quantity) OVER (
                   ORDER BY si.expiry_date NULLS FIRST, si.created, si.id
                   ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING), 0) AS acumulado_previo
          FROM public.stock_items si
         WHERE si.product_id = p_product_id AND si.branch_id = v_branch
           AND si.state AND si.quantity > 0
    )
    SELECT o.id, o.lot_code, o.expiry_date,
           LEAST(o.quantity, p_cantidad - o.acumulado_previo)
      FROM ordenadas o
     WHERE o.acumulado_previo < p_cantidad
     ORDER BY o.expiry_date NULLS FIRST, o.id;
END $$;

COMMENT ON FUNCTION public.fn_asignar_fefo(uuid, numeric, uuid) IS
    'Reparte una cantidad entre las existencias de la sucursal, primero lo que vence '
    'antes. Sin seguimiento admite vender por encima del saldo, sobre la existencia implícita.';

CREATE OR REPLACE FUNCTION public.fn_recibir_lote(
    p_product_id  uuid,
    p_cantidad    numeric,
    p_lot_code    varchar,
    p_expiry_date date    DEFAULT NULL,
    p_user_id     integer DEFAULT 0,
    p_branch_id   uuid    DEFAULT NULL
)
RETURNS TABLE (stock_item_id uuid, stock_before numeric, stock_after numeric)
LANGUAGE plpgsql
AS $$
DECLARE
    v_branch uuid := public.fn_sucursal_requerida(p_branch_id);
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

    -- Recibir el mismo lote dos veces en la misma sucursal suma sobre la misma
    -- existencia. Es lo habitual: un pedido parcial y su reposición traen el
    -- mismo lote.
    SELECT si.id INTO v_item
      FROM public.stock_items si
     WHERE si.product_id = p_product_id
       AND si.branch_id  = v_branch
       AND si.lot_code = trim(p_lot_code)
       AND si.expiry_date IS NOT DISTINCT FROM p_expiry_date
       AND si.serial_number IS NULL;

    IF v_item IS NULL THEN
        INSERT INTO public.stock_items
            (tenant_id, branch_id, product_id, lot_code, expiry_date, quantity, created_by, modified_by)
        VALUES (v_tenant, v_branch, p_product_id, trim(p_lot_code), p_expiry_date, 0, p_user_id, p_user_id)
        RETURNING id INTO v_item;
    END IF;

    RETURN QUERY SELECT * FROM public.fn_mover_stock(p_product_id, p_cantidad, p_user_id, v_item, v_branch);
END $$;

COMMENT ON FUNCTION public.fn_recibir_lote(uuid, numeric, varchar, date, integer, uuid) IS
    'Ingresa stock de un lote en la sucursal: suma a la existencia del lote si ya '
    'existe allí, o la crea.';

CREATE OR REPLACE FUNCTION public.fn_recibir_serie(
    p_product_id    uuid,
    p_serial_number varchar,
    p_expiry_date   date    DEFAULT NULL,
    p_user_id       integer DEFAULT 0,
    p_branch_id     uuid    DEFAULT NULL
)
RETURNS TABLE (stock_item_id uuid, stock_before numeric, stock_after numeric)
LANGUAGE plpgsql
AS $$
DECLARE
    v_branch uuid := public.fn_sucursal_requerida(p_branch_id);
    v_item   uuid;
    v_tenant integer;
    v_serie  varchar;
BEGIN
    v_serie := trim(p_serial_number);

    IF coalesce(v_serie, '') = '' THEN
        RAISE EXCEPTION 'El número de serie es obligatorio para un producto con seguimiento por series.';
    END IF;

    -- El tenant sale del producto, no de la sesión: mismo motivo que en
    -- fn_recibir_lote.
    SELECT p.tenant_id INTO v_tenant FROM public.products p WHERE p.id = p_product_id;

    IF v_tenant IS NULL THEN
        RAISE EXCEPTION 'No existe el producto %.', p_product_id;
    END IF;

    -- En toda la farmacia, no solo en la sucursal: una serie es una unidad
    -- física. El índice único lo impediría igual, pero su mensaje no le dice
    -- nada a quien está recibiendo mercadería con el lector en la mano.
    SELECT si.id INTO v_item
      FROM public.stock_items si
     WHERE si.serial_number = v_serie;

    IF v_item IS NOT NULL THEN
        RAISE EXCEPTION 'El número de serie % ya está registrado.', v_serie;
    END IF;

    INSERT INTO public.stock_items
        (tenant_id, branch_id, product_id, serial_number, expiry_date, quantity, created_by, modified_by)
    VALUES (v_tenant, v_branch, p_product_id, v_serie, p_expiry_date, 0, p_user_id, p_user_id)
    RETURNING id INTO v_item;

    RETURN QUERY SELECT * FROM public.fn_mover_stock(p_product_id, 1, p_user_id, v_item, v_branch);
END $$;

COMMENT ON FUNCTION public.fn_recibir_serie(uuid, varchar, date, integer, uuid) IS
    'Ingresa una unidad con número de serie en la sucursal. La serie es única en '
    'toda la farmacia.';

GRANT EXECUTE ON FUNCTION
    public.fn_sucursal_requerida(uuid),
    public.fn_existencia_implicita(uuid, uuid, integer),
    public.fn_mover_stock(uuid, numeric, integer, uuid, uuid),
    public.fn_asignar_fefo(uuid, numeric, uuid),
    public.fn_recibir_lote(uuid, numeric, varchar, date, integer, uuid),
    public.fn_recibir_serie(uuid, varchar, date, integer, uuid)
TO app_pos;

-- -----------------------------------------------------------------------------
-- 5. Vistas de stock por sucursal
-- -----------------------------------------------------------------------------
-- security_invoker en todas: sin él la vista corre como su dueño (superusuario)
-- y RLS no se aplica adentro. Ver v_stock_descuadrado.

-- Stock de cada producto en cada sucursal. Para reportes y traspasos.
CREATE OR REPLACE VIEW public.v_stock_sucursal
    WITH (security_invoker = true) AS
SELECT si.tenant_id, si.branch_id, si.product_id, sum(si.quantity) AS quantity
  FROM public.stock_items si
 WHERE si.state
 GROUP BY si.tenant_id, si.branch_id, si.product_id;

COMMENT ON VIEW public.v_stock_sucursal IS
    'Stock de cada producto en cada sucursal: suma de sus existencias activas.';

-- Stock en la sucursal de la sesión. Es lo que muestran el POS y los listados:
-- se une con LEFT JOIN a products y reemplaza a products.current_stock, que es
-- el total de la farmacia.
CREATE OR REPLACE VIEW public.v_stock_actual
    WITH (security_invoker = true) AS
SELECT si.product_id, sum(si.quantity) AS quantity
  FROM public.stock_items si
 WHERE si.state AND si.branch_id = public.current_branch()
 GROUP BY si.product_id;

COMMENT ON VIEW public.v_stock_actual IS
    'Stock de cada producto en la sucursal de la sesión. Un producto sin fila acá '
    'tiene 0 en esta sucursal.';

GRANT SELECT ON public.v_stock_sucursal, public.v_stock_actual TO app_pos;

-- Las vistas existentes ganan la sucursal como columnas nuevas al final (es lo
-- único que CREATE OR REPLACE VIEW admite sin recrearlas).
CREATE OR REPLACE VIEW public.v_stock_por_vencer
    WITH (security_invoker = true) AS
SELECT si.tenant_id,
       si.id AS stock_item_id,
       p.id AS product_id,
       p.product_code,
       p.product_name,
       si.lot_code,
       si.expiry_date,
       si.quantity,
       (si.expiry_date - CURRENT_DATE) AS dias_restantes,
       CASE
           WHEN si.expiry_date < CURRENT_DATE        THEN 'VENCIDO'
           WHEN si.expiry_date <= CURRENT_DATE + 30  THEN 'CRITICO'
           WHEN si.expiry_date <= CURRENT_DATE + 90  THEN 'PROXIMO'
           ELSE 'VIGENTE'
       END AS estado,
       (si.quantity * p.sale_price) AS valor_en_riesgo,
       si.branch_id,
       b.name AS branch_name
  FROM public.stock_items si
  JOIN public.products p ON p.id = si.product_id
  JOIN public.branches b ON b.id = si.branch_id
 WHERE si.state AND si.quantity > 0 AND si.expiry_date IS NOT NULL;

-- Trazabilidad: la sucursal es la de la venta. Un retiro de lote se responde
-- para toda la farmacia, así que la vista no filtra; la columna dice dónde se
-- entregó.
CREATE OR REPLACE VIEW public.v_trazabilidad_lote
    WITH (security_invoker = true) AS
SELECT si.tenant_id,
       si.lot_code,
       si.expiry_date,
       p.product_code,
       p.product_name,
       s.id AS sale_id,
       s.sale_date,
       c.full_name AS cliente,
       c.document_number,
       c.cellphone,
       sd.quantity,
       s.branch_id,
       b.name AS branch_name
  FROM public.stock_items si
  JOIN public.products p      ON p.id = si.product_id
  JOIN public.sales_detail sd ON sd.stock_item_id = si.id
  JOIN public.sales s         ON s.id = sd.sale_id
  JOIN public.customers c     ON c.id = s.customer_id
  JOIN public.branches b      ON b.id = s.branch_id
 WHERE si.lot_code IS NOT NULL AND sd.state AND s.state;

CREATE OR REPLACE VIEW public.v_trazabilidad_serie
    WITH (security_invoker = true) AS
SELECT si.tenant_id,
       si.serial_number,
       si.expiry_date,
       p.product_code,
       p.product_name,
       s.id AS sale_id,
       s.sale_date,
       c.full_name AS cliente,
       c.document_number,
       c.cellphone,
       s.branch_id,
       b.name AS branch_name
  FROM public.stock_items si
  JOIN public.products p      ON p.id = si.product_id
  JOIN public.sales_detail sd ON sd.stock_item_id = si.id
  JOIN public.sales s         ON s.id = sd.sale_id
  JOIN public.customers c     ON c.id = s.customer_id
  JOIN public.branches b      ON b.id = s.branch_id
 WHERE si.serial_number IS NOT NULL AND sd.state AND s.state;

CREATE OR REPLACE VIEW public.v_mermas
    WITH (security_invoker = true) AS
SELECT sm.tenant_id,
       sm.product_id,
       p.product_code,
       p.product_name,
       si.lot_code,
       si.expiry_date,
       abs(sm.quantity) AS cantidad,
       (abs(sm.quantity)::numeric * p.sale_price) AS valor_perdido,
       sm.reason,
       sm.observation,
       sm.created,
       sm.created_by,
       sm.branch_id,
       b.name AS branch_name
  FROM public.stock_movements sm
  JOIN public.products p ON p.id = sm.product_id
  JOIN public.branches b ON b.id = sm.branch_id
  LEFT JOIN public.stock_items si ON si.id = sm.stock_item_id
 WHERE sm.movement_type = 'MERMA' AND sm.state;

CREATE OR REPLACE VIEW public.v_sales_net
    WITH (security_invoker = true) AS
SELECT s.id,
       s.customer_id,
       s.sale_date,
       s.is_active,
       s.state,
       s.created_by,
       s.created,
       s.modified_by,
       s.modified,
       s.subtotal,
       s.total_discounts,
       s.total,
       s.cash_session_id,
       s.header_discount_id,
       s.header_discount_amount,
       s.tenant_id,
       COALESCE(r.total_returned, 0) AS total_returned,
       s.total - COALESCE(r.total_returned, 0) AS net_total,
       CASE
           WHEN NOT s.is_active                        THEN 'anulada'
           WHEN COALESCE(r.total_returned, 0) > 0      THEN 'con_devolucion'
           ELSE 'activa'
       END AS sale_status,
       s.branch_id
  FROM public.sales s
  LEFT JOIN LATERAL (
        SELECT sum(sr.total_returned) AS total_returned
          FROM public.sale_returns sr
         WHERE sr.sale_id = s.id AND sr.state
       ) r ON true;

COMMIT;

-- =============================================================================
-- Verificación
-- =============================================================================
--   -- El total de la farmacia sigue cuadrando con las existencias (vacía):
--   SELECT * FROM v_stock_descuadrado;
--
--   -- Stock por sucursal:
--   SELECT b.name, count(*), sum(quantity) FROM v_stock_sucursal v
--     JOIN branches b ON b.id = v.branch_id GROUP BY b.name;
-- =============================================================================
