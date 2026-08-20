-- -----------------------------------------------------------------------------
-- Cliente genérico ("Consumidor Final") por tenant
-- -----------------------------------------------------------------------------
-- Hasta ahora el POS (web y móvil) exigía elegir un cliente ya existente antes
-- de poder cobrar, sin forma de dar de alta uno nuevo desde el cobro. Eso no es
-- la práctica habitual en un POS: la venta no debería depender de tener el
-- cliente cargado de antemano.
--
-- Se agrega is_generic para marcar, por tenant, al cliente que el POS
-- precarga por defecto (evita apoyarse en una convención de texto como
-- document_number = '0', que cualquiera podría reutilizar sin querer).
--
-- Se siembra dentro de sec.fn_seed_tenant_master_data, la misma función que ya
-- deja a un tenant operativo (unidad de medida, laboratorio, categoría,
-- métodos de pago). La llaman tanto fn_provision_tenant (alta de una farmacia
-- nueva) como ResetCompany (reinicio de empresa), así que ambos flujos quedan
-- cubiertos sin tocarlos.
--
-- Idempotente.

ALTER TABLE public.customers
    ADD COLUMN IF NOT EXISTS is_generic boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.customers.is_generic IS
    'Cliente "Consumidor Final" que el POS precarga por defecto para no bloquear '
    'una venta sin cliente identificado. Uno por tenant, sembrado por '
    'sec.fn_seed_tenant_master_data. No se puede borrar (ver DeleteCustomer).';

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
        VALUES (gen_random_uuid(), 'Consumidor Final', '0', '', '', true, true,
                true, 1, now(), 1, now(), p_tenant);
    END IF;
END $$;

REVOKE ALL  ON FUNCTION sec.fn_seed_tenant_master_data(integer) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION sec.fn_seed_tenant_master_data(integer) TO app_pos;

-- Backfill: deja con su cliente genérico a cualquier tenant que ya existiera
-- antes de esta migración. Segura de repetir: cada bloque de la función es
-- IF NOT EXISTS, así que no duplica nada de lo que ya está sembrado.
SELECT sec.fn_seed_tenant_master_data(id) FROM sec.tenants;
