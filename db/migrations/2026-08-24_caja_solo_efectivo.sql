-- -----------------------------------------------------------------------------
-- Arqueo de caja: contar solo lo que entra al cajón
-- -----------------------------------------------------------------------------
-- CashSessionApplication.CloseSession calculaba el efectivo esperado como
-- "fondo inicial + ventas - gastos - retiros + ingresos", donde "ventas" era
-- SUM(sales.total) de la sesión: TODAS las ventas, sin importar el método de
-- pago. El comentario del código decía "ventas en efectivo", pero la consulta
-- no filtraba nada. Con una venta por QR o tarjeta el sistema esperaba
-- encontrar en el cajón plata que nunca entró, y el faltante se le imputaba al
-- cajero. En esta base ya hay 132.53 cobrados por QR, uno de ellos (90.31)
-- dentro de una sesión de caja.
--
-- payment_methods no tenía con qué distinguirlo. requires_changes (si el método
-- da vuelto) hoy coincide, pero significa otra cosa: alguien podría cambiarlo
-- sin pensar en el arqueo. Por eso se agrega affects_cash, que es la pregunta
-- que el arqueo necesita: ¿este cobro entra al cajón?
--
-- El valor inicial se siembra desde requires_changes, que es la mejor
-- aproximación disponible para los métodos ya cargados. Se hace una sola vez,
-- al crear la columna, para no pisar ajustes manuales si el script se vuelve a
-- correr.
--
-- Idempotente.

DO $do$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'public'
                      AND table_name   = 'payment_methods'
                      AND column_name  = 'affects_cash') THEN

        ALTER TABLE public.payment_methods
            ADD COLUMN affects_cash boolean NOT NULL DEFAULT true;

        UPDATE public.payment_methods
           SET affects_cash = COALESCE(requires_changes, true);
    END IF;
END $do$;

COMMENT ON COLUMN public.payment_methods.affects_cash IS
    'Si el cobro por este método entra físicamente al cajón. Es lo único que el '
    'arqueo debe sumar como efectivo esperado (ver CashSessionRepository). '
    'Distinto de requires_changes, que solo indica si el método da vuelto.';

-- La siembra de un tenant nuevo tiene que nacer con la bandera correcta: sin
-- esto, un QR recién sembrado quedaría marcado como efectivo por el DEFAULT.
-- Definición tomada de la base (pg_get_functiondef) y modificada solo en el
-- INSERT de payment_methods.
CREATE OR REPLACE FUNCTION sec.fn_seed_tenant_master_data(p_tenant integer)
 RETURNS void
 LANGUAGE plpgsql
 SECURITY DEFINER
 SET search_path TO 'sec', 'public', 'pg_temp'
AS $function$
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
            (id, name, requires_changes, affects_cash, state, created_by, created, modified_by, modified, icon_css, tenant_id)
        VALUES
            (gen_random_uuid(), 'Efectivo', true,  true,  true, 1, now(), 1, now(), 'fal fa-money-bill-wave', p_tenant),
            (gen_random_uuid(), 'Tarjeta',  false, false, true, 1, now(), 1, now(), 'fal fa-credit-card',     p_tenant),
            (gen_random_uuid(), 'QR',       false, false, true, 1, now(), 1, now(), 'fal fa-qrcode',          p_tenant);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM public.customers WHERE tenant_id = p_tenant AND is_generic) THEN
        INSERT INTO public.customers
            (id, full_name, document_number, email, cellphone, is_active, is_generic,
             state, created_by, created, modified_by, modified, tenant_id)
        VALUES (gen_random_uuid(), 'Cliente Genérico', '0', '', '', true, true,
                true, 1, now(), 1, now(), p_tenant);
    END IF;
END $function$

;
