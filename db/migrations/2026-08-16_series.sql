-- -----------------------------------------------------------------------------
-- Seguimiento por número de serie
-- -----------------------------------------------------------------------------
-- El modo 'serial' existía a medias desde el primer incremento: la columna
-- `serial_number`, su índice único por farmacia y el CHECK que lo admite como
-- valor válido. Lo que no había era una sola función que lo activara o lo
-- recibiera, así que era un modo declarado e inalcanzable.
--
-- Sirve para lo que una farmacia vende con garantía o registro sanitario por
-- unidad: tensiómetros, nebulizadores, glucómetros. Ahí no interesa el lote
-- sino CUÁL unidad se entregó.
--
-- La asignación al vender NO necesita nada nuevo: fn_asignar_fefo ya reparte
-- sobre existencias con cantidad 1, así que vender N devuelve N filas, una por
-- serie, y rechaza la sobreventa igual que con lotes.
--
-- Idempotente: solo crea o reemplaza funciones.

-- -----------------------------------------------------------------------------
-- 1. Activar el seguimiento por series
-- -----------------------------------------------------------------------------
-- Mismo criterio que fn_activar_lotes y su espejo: el stock que el producto ya
-- tenía queda como existencia SIN serie, y FEFO la consume primero. Es
-- mercadería de la que no se registró el número; conviene sacarla mientras
-- todavía se la puede mirar, no dejarla detrás de las unidades identificadas.
CREATE OR REPLACE FUNCTION public.fn_activar_series(p_product_id uuid)
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

    -- Simétrico a fn_activar_lotes, que rechaza pasar de 'serial' a 'lot'. Las
    -- existencias de lote agrupan varias unidades bajo un código; repartirlas en
    -- una fila por unidad exigiría inventar números de serie que nadie anotó.
    IF v_modo = 'lot' THEN
        RAISE EXCEPTION 'El producto ya usa lotes; no se puede pasar a números de serie.';
    END IF;

    UPDATE public.products SET tracking_mode = 'serial', modified = now()
     WHERE id = p_product_id AND tracking_mode <> 'serial';
END $$;

COMMENT ON FUNCTION public.fn_activar_series(uuid) IS
    'Pasa un producto a seguimiento por número de serie. El stock que ya tenía '
    'queda como existencia sin serie, y FEFO la consume primero.';

GRANT EXECUTE ON FUNCTION public.fn_activar_series(uuid) TO app_pos;

-- -----------------------------------------------------------------------------
-- 2. Recibir una unidad con su número de serie
-- -----------------------------------------------------------------------------
-- Una unidad, una existencia, cantidad 1. A diferencia de fn_recibir_lote, acá
-- recibir dos veces el mismo número NO suma: es la misma unidad física, y que
-- aparezca dos veces significa que alguien se equivocó al teclear o que le
-- pusieron la etiqueta a dos cajas.
CREATE OR REPLACE FUNCTION public.fn_recibir_serie(
    p_product_id    uuid,
    p_serial_number varchar,
    p_expiry_date   date    DEFAULT NULL,
    p_user_id       integer DEFAULT 0
)
RETURNS TABLE (stock_item_id uuid, stock_before numeric, stock_after numeric)
LANGUAGE plpgsql
AS $$
DECLARE
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

    -- El índice único lo impediría igual, pero su mensaje no le dice nada a
    -- quien está recibiendo mercadería con el lector en la mano.
    SELECT si.id INTO v_item
      FROM public.stock_items si
     WHERE si.serial_number = v_serie;

    IF v_item IS NOT NULL THEN
        RAISE EXCEPTION 'El número de serie % ya está registrado.', v_serie;
    END IF;

    INSERT INTO public.stock_items
        (tenant_id, product_id, serial_number, expiry_date, quantity, created_by, modified_by)
    VALUES (v_tenant, p_product_id, v_serie, p_expiry_date, 0, p_user_id, p_user_id)
    RETURNING id INTO v_item;

    RETURN QUERY SELECT * FROM public.fn_mover_stock(p_product_id, 1, p_user_id, v_item);
END $$;

COMMENT ON FUNCTION public.fn_recibir_serie(uuid, varchar, date, integer) IS
    'Da entrada a UNA unidad identificada por su número de serie. Rechaza un '
    'número repetido: sería la misma unidad física dos veces.';

GRANT EXECUTE ON FUNCTION public.fn_recibir_serie(uuid, varchar, date, integer) TO app_pos;

-- -----------------------------------------------------------------------------
-- 3. Trazabilidad por número de serie
-- -----------------------------------------------------------------------------
-- El equivalente de v_trazabilidad_lote para las unidades identificadas: a quién
-- se le entregó cada número. Es la consulta de una garantía o de un retiro.
CREATE OR REPLACE VIEW public.v_trazabilidad_serie
    WITH (security_invoker = true) AS
SELECT si.tenant_id,
       si.serial_number,
       si.expiry_date,
       p.product_code,
       p.product_name,
       s.id            AS sale_id,
       s.sale_date,
       c.full_name     AS cliente,
       c.document_number,
       c.cellphone
  FROM public.stock_items si
  JOIN public.products     p  ON p.id  = si.product_id
  JOIN public.sales_detail sd ON sd.stock_item_id = si.id
  JOIN public.sales        s  ON s.id  = sd.sale_id
  JOIN public.customers    c  ON c.id  = s.customer_id
 WHERE si.serial_number IS NOT NULL AND sd.state AND s.state;

COMMENT ON VIEW public.v_trazabilidad_serie IS
    'A quién se le entregó cada número de serie. Es la consulta de una garantía.';

GRANT SELECT ON public.v_trazabilidad_serie TO app_pos;
