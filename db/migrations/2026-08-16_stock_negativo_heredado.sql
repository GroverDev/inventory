-- -----------------------------------------------------------------------------
-- Corrección del stock negativo heredado
-- -----------------------------------------------------------------------------
-- Cuatro productos arrastraban saldo negativo desde antes del libro mayor
-- (DR BELL CALLOS -20, CLOBETASOL -2, BALLENA AZUL -1, POMADA X -1) y NINGUNO
-- tiene un solo movimiento que lo explique. Cuando se creó el modelo de
-- existencias se los dejó a propósito: rechazarlos habría abortado la migración
-- y forzarlos a cero en silencio habría escondido un dato real.
--
-- Ahora se corrigen, pero dejando rastro. Un saldo negativo es físicamente
-- imposible —no se puede tener menos veinte cajas en un estante— y además
-- envenena todo lo que se apoye en el stock: el punto de venta, los reportes y
-- el cálculo de reposición.
--
-- Se corrige con fn_mover_stock y no con un UPDATE directo, por dos razones: es
-- lo que mantiene alineadas la existencia y la caché de products (lo que vigila
-- v_stock_descuadrado), y deja el movimiento asentado para que dentro de un año
-- se pueda ver de dónde salió ese ajuste.
--
-- Idempotente: solo toca lo que siga en negativo.

DO $$
DECLARE
    r           RECORD;
    v_res       RECORD;
    v_corregidos integer := 0;
BEGIN
    FOR r IN
        SELECT p.id, p.product_name, p.current_stock, p.tenant_id
          FROM public.products p
         WHERE p.current_stock < 0
           AND p.state
         ORDER BY p.tenant_id, p.product_name
    LOOP
        -- fn_mover_stock y el INSERT resuelven el tenant por current_tenant(),
        -- así que hay que anunciarlo en cada vuelta: la corrección puede abarcar
        -- varias farmacias.
        PERFORM set_config('app.tenant_id', r.tenant_id::text, false);

        SELECT * INTO v_res
          FROM public.fn_mover_stock(r.id, (-r.current_stock)::numeric, 1);

        INSERT INTO public.stock_movements
            (id, product_id, stock_item_id, movement_type, quantity,
             stock_before, stock_after, reason, observation,
             reference_id, reference_type, state,
             created_by, created, modified_by, modified, tenant_id)
        VALUES (gen_random_uuid(), r.id, v_res.stock_item_id, 'AJUSTE',
                (-r.current_stock), v_res.stock_before, v_res.stock_after,
                'Corrección de saldo negativo heredado',
                'Saldo anterior al libro mayor, sin ningún movimiento que lo explicara.',
                NULL, 'CORRECCION', true,
                1, now(), 1, now(), r.tenant_id);

        v_corregidos := v_corregidos + 1;
        RAISE NOTICE '% : % -> 0', r.product_name, r.current_stock;
    END LOOP;

    IF v_corregidos = 0 THEN
        RAISE NOTICE 'No quedaba ningún producto con saldo negativo.';
    ELSE
        RAISE NOTICE 'Productos corregidos: %.', v_corregidos;
    END IF;
END $$;
