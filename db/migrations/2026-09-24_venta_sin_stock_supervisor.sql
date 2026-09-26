-- =============================================================================
-- Venta sin stock: el cajero necesita a un supervisor
-- =============================================================================
-- Hasta hoy, un producto SIN seguimiento se podía vender por encima de su saldo y
-- quedaba en negativo, sin que nadie lo notara ni lo autorizara. Con lotes o
-- series ya se rechazaba. Ahora, en la venta de un cajero, el faltante de un
-- producto sin seguimiento se rechaza con un código propio (PS001) que el punto
-- de venta interpreta como "pedir la autorización de un supervisor".
--
-- Se activa por transacción, no por defecto: fn_asignar_fefo también la usan los
-- traspasos, y cambiar su comportamiento ahí es otra decisión. La venta lo
-- enciende con
--     SELECT set_config('app.exigir_stock', 'on', true);
-- y el efecto muere con la transacción, así que una conexión reutilizada del pool
-- no lo hereda.
--
-- Además la función toma un candado por producto y sucursal durante la venta. Sin
-- él, dos cajas que venden la última unidad a la vez leen las dos "hay 1" y las
-- dos pasan: la comprobación y el descuento eran dos pasos separados. Con el
-- candado la segunda espera a que la primera termine, y entonces ve el saldo real.
-- Sirve también para los lotes, que tenían la misma carrera.
--
-- Idempotente.
-- =============================================================================

BEGIN;

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
    v_nombre     text;
BEGIN
    IF p_cantidad <= 0 THEN
        RAISE EXCEPTION 'La cantidad a asignar debe ser positiva.';
    END IF;

    SELECT p.tracking_mode, p.product_name INTO v_modo, v_nombre
      FROM public.products p WHERE p.id = p_product_id;

    IF v_modo IS NULL THEN
        RAISE EXCEPTION 'No existe el producto %.', p_product_id;
    END IF;

    -- Quien vende el mismo producto en la misma sucursal espera su turno: el saldo
    -- que se lee abajo tiene que ser el que dejó la venta anterior. Se libera solo
    -- al terminar la transacción.
    PERFORM pg_advisory_xact_lock(hashtextextended(p_product_id::text || v_branch::text, 0));

    -- Solo lo que hay en esta sucursal: el stock de otra no se puede entregar
    -- desde el mostrador de esta.
    SELECT COALESCE(sum(si.quantity), 0) INTO v_disponible
      FROM public.stock_items si
     WHERE si.product_id = p_product_id AND si.branch_id = v_branch
       AND si.state AND si.quantity > 0;

    IF v_disponible < p_cantidad THEN
        IF v_modo = 'none' THEN
            -- Vender más de lo registrado sigue siendo posible, pero cuando la
            -- venta lo pide (cajero sin autorización) se corta con un código que
            -- el punto de venta sabe leer.
            IF current_setting('app.exigir_stock', true) = 'on' THEN
                RAISE EXCEPTION 'Stock insuficiente de «%»: se piden % y hay %. Requiere la autorización de un supervisor.',
                    v_nombre, p_cantidad, v_disponible
                    USING ERRCODE = 'PS001';
            END IF;

            v_implicita := public.fn_existencia_implicita(p_product_id, v_branch);
            RETURN QUERY SELECT v_implicita, NULL::varchar, NULL::date, p_cantidad;
            RETURN;
        END IF;

        -- Con lotes sí se rechaza siempre: vender de un lote que no se recibió no
        -- significa nada, y rompería la trazabilidad que justifica todo esto. Ni
        -- un supervisor lo puede autorizar.
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
    'antes. Sin seguimiento admite vender por encima del saldo, salvo que la transacción '
    'active app.exigir_stock: entonces lo rechaza con el código PS001 (requiere supervisor).';

COMMIT;
