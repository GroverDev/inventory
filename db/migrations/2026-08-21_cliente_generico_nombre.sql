-- El cliente genérico se llama "Cliente Genérico", no "Consumidor Final".
--
-- La migración 2026-08-20_default_customer.sql sembraba "Consumidor Final",
-- pero el nombre elegido para el producto es "Cliente Genérico". Nadie lo notó
-- porque el bloque de siembra es `IF NOT EXISTS`: el tenant 1 ya tenía su
-- cliente genérico con el nombre correcto y el backfill lo saltó, así que la
-- discrepancia quedó latente y solo habría aparecido al dar de alta una empresa
-- nueva, que habría nacido con el nombre equivocado.
--
-- La función se reemplaza ENTERA y su cuerpo se copió literal de la migración
-- 2026-08-20 (única definición vigente), cambiando solo el literal del nombre.
-- Ojo al repetir esto: la función también siembra unit_of_measurement,
-- laboratories, categories y payment_methods, y valida que la farmacia exista.
-- Reescribirla de memoria deja tenants nuevos sin unidad de medida y hace fallar
-- el alta de productos con "null value in column uom_id".

CREATE OR REPLACE FUNCTION sec.fn_seed_tenant_master_data(p_tenant integer)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = sec, public, pg_temp
AS $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sec.tenants WHERE id = p_tenant) THEN
        RAISE EXCEPTION 'No existe la farmacia %.', p_tenant;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.unit_of_measurement WHERE tenant_id = p_tenant) THEN
        INSERT INTO public.unit_of_measurement
            (id, unit_name, proportion, precision_rounding, is_large_than_default,
             is_default, is_active, state, created_by, created, modified_by, modified, tenant_id)
        VALUES (gen_random_uuid(), 'UNIDAD', 100, 1, false, true, true, true,
                1, now(), 1, now(), p_tenant);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.laboratories WHERE tenant_id = p_tenant) THEN
        INSERT INTO public.laboratories
            (id, laboratory_name, description, direction, celular,
             is_active, state, created_by, created, modified_by, modified, tenant_id)
        VALUES (gen_random_uuid(), 'SIN LABORATORIO', 'Valor por defecto, editable', '', '',
                true, true, 1, now(), 1, now(), p_tenant);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.categories WHERE tenant_id = p_tenant) THEN
        INSERT INTO public.categories
            (id, category_name, description, is_active, state,
             created_by, created, modified_by, modified, tenant_id)
        VALUES (gen_random_uuid(), 'GENERAL', 'Categoría por defecto, editable',
                true, true, 1, now(), 1, now(), p_tenant);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.payment_methods WHERE tenant_id = p_tenant) THEN
        INSERT INTO public.payment_methods
            (id, name, requires_changes, state, created_by, created, modified_by, modified, icon_css, tenant_id)
        VALUES
            (gen_random_uuid(), 'Efectivo', true,  true, 1, now(), 1, now(), 'fal fa-money-bill-wave', p_tenant),
            (gen_random_uuid(), 'Tarjeta',  false, true, 1, now(), 1, now(), 'fal fa-credit-card',     p_tenant),
            (gen_random_uuid(), 'QR',       false, true, 1, now(), 1, now(), 'fal fa-qrcode',          p_tenant);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.customers WHERE tenant_id = p_tenant AND is_generic) THEN
        INSERT INTO public.customers
            (id, full_name, document_number, email, cellphone, is_active, is_generic,
             state, created_by, created, modified_by, modified, tenant_id)
        VALUES (gen_random_uuid(), 'Cliente Genérico', '0', '', '', true, true,
                true, 1, now(), 1, now(), p_tenant);
    END IF;
END $$;

REVOKE ALL  ON FUNCTION sec.fn_seed_tenant_master_data(integer) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION sec.fn_seed_tenant_master_data(integer) TO app_pos;

-- Normaliza a los tenants que alcanzaron a nacer con el nombre viejo. Toca solo
-- el cliente genérico, y solo si conserva el literal sembrado: si alguien le
-- puso otro nombre a propósito, se respeta.
UPDATE public.customers
   SET full_name = 'Cliente Genérico'
 WHERE is_generic
   AND full_name = 'Consumidor Final';

-- El comentario de la columna también nombraba al cliente viejo. Se deja sin el
-- nombre: es editable por el usuario, así que repetirlo acá vuelve a envejecer.
COMMENT ON COLUMN public.customers.is_generic IS
    'Cliente genérico que el POS precarga por defecto para no bloquear '
    'una venta sin cliente identificado. Uno por tenant, sembrado por '
    'sec.fn_seed_tenant_master_data. No se puede borrar (ver DeleteCustomer).';
