-- -----------------------------------------------------------------------------
-- Devoluciones: reembolsar el precio efectivamente cobrado, no el de lista
-- -----------------------------------------------------------------------------
-- Hasta ahora el importe de una devolución se calculaba como
-- quantity_returned * unit_price, donde unit_price es el precio de lista de
-- sales_detail: el precio ANTES de los descuentos. Con descuento por producto o
-- con descuento global de cabecera eso devuelve más plata de la que el cliente
-- pagó. El caso extremo ya existe en la base: una venta de subtotal 84.00 con
-- 55.00 de descuento global se cobró en 29.00, y una devolución total le habría
-- reembolsado 84.00.
--
-- Se agrega discount_share para dejar explícito cuánto descuento le corresponde
-- a las unidades devueltas (el de la propia línea más la porción prorrateada
-- del descuento global). El importe reembolsado pasa a ser:
--
--     line_total = quantity_returned * unit_price - discount_share
--
-- unit_price conserva su significado actual (precio de lista al momento de la
-- venta), así que las filas históricas siguen siendo coherentes: con
-- discount_share = 0 la igualdad de arriba se cumple exactamente para todas.
-- El cálculo pasa a hacerse en el servidor (SaleReturnApplication), que lee los
-- importes de sales_detail y sales en vez de confiar en los que manda el
-- cliente.
--
-- Nota: quedan 3 líneas históricas (devolución del 2026-05-28, venta con 1.00 de
-- descuento global) reembolsadas de más por 0.37 en total. Este script NO las
-- corrige: el ajuste del histórico es una decisión aparte.
--
-- Idempotente.

ALTER TABLE public.sale_return_detail
    ADD COLUMN IF NOT EXISTS discount_share numeric(18,2) NOT NULL DEFAULT 0;

COMMENT ON COLUMN public.sale_return_detail.unit_price IS
    'Precio de lista de la unidad al momento de la venta (sales_detail.unit_price), '
    'sin descuentos. Se conserva para poder mostrar el desglose en el comprobante.';

COMMENT ON COLUMN public.sale_return_detail.discount_share IS
    'Descuentos que corresponden a las unidades devueltas: el descuento de la línea '
    'más la porción prorrateada del descuento global de la venta. Lo calcula '
    'SaleReturnApplication al registrar la devolución.';

COMMENT ON COLUMN public.sale_return_detail.line_total IS
    'Importe efectivamente reembolsado = quantity_returned * unit_price - discount_share.';
