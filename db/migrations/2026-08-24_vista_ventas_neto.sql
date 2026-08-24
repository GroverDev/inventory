-- -----------------------------------------------------------------------------
-- v_sales_net: el total de una venta ya neteado de devoluciones
-- -----------------------------------------------------------------------------
-- sales.total es el importe facturado y no se toca nunca: las devoluciones se
-- registran aparte, en sale_returns. El problema es que hasta ahora cada
-- pantalla que sumaba ventas lo hacía con SUM(sales.total) -- el listado de
-- ventas, el reporte de ventas, el dashboard -- así que ninguna descontaba lo
-- devuelto, y una venta con devolución total (is_active = false) seguía
-- aportando el 100% de su importe.
--
-- La definición de "neto" vive acá, en un solo lugar, para que las cuatro
-- pantallas no puedan divergir. No se desnormaliza total_returned dentro de
-- sales a propósito: sería duplicar el hecho que ya vive en sale_returns y
-- abrir la puerta a que se desincronicen.
--
-- security_invoker = true es obligatorio: sin eso la vista se evaluaría con los
-- permisos de su dueño (postgres) y saltearía las políticas RLS de aislamiento
-- por tenant de sales y sale_returns.
--
-- Idempotente.

CREATE INDEX IF NOT EXISTS ix_sale_returns_sale
    ON public.sale_returns (tenant_id, sale_id);

CREATE OR REPLACE VIEW public.v_sales_net WITH (security_invoker = true) AS
SELECT s.*,
       COALESCE(r.total_returned, 0)             AS total_returned,
       s.total - COALESCE(r.total_returned, 0)   AS net_total,
       CASE WHEN NOT s.is_active                    THEN 'anulada'
            WHEN COALESCE(r.total_returned, 0) > 0  THEN 'con_devolucion'
            ELSE 'activa'
       END                                       AS sale_status
  FROM public.sales s
       LEFT JOIN LATERAL (
            SELECT SUM(sr.total_returned) AS total_returned
              FROM public.sale_returns sr
             WHERE sr.sale_id = s.id
               AND sr.state
       ) r ON TRUE;

COMMENT ON VIEW public.v_sales_net IS
    'sales + lo devuelto por cada venta. net_total es el importe que quedó '
    'efectivamente cobrado; sale_status distingue activa / con_devolucion / anulada. '
    'Toda pantalla que sume ventas debe leer de acá, no de sales.';

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_pos') THEN
        GRANT SELECT ON public.v_sales_net TO app_pos;
    END IF;
END $$;
