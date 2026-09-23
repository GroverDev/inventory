-- =============================================================================
-- Ventas en espera
-- =============================================================================
-- El cajero deja una venta a medio armar (el cliente fue a buscar dinero),
-- atiende a otro y después la retoma. Es lo que otros POS llaman "open
-- tickets", "carritos guardados" o "parked sales".
--
--   * Son de la sucursal, no de la caja ni del cajero: cualquier caja de la
--     sucursal la puede retomar.
--   * No reservan stock ni fijan precios: el carrito se guarda tal cual y al
--     cobrar la venta pasa por las mismas validaciones que cualquier otra
--     (precio vigente, topes de descuento, stock).
--   * Retomarla la saca de la espera (DELETE ... RETURNING): dos cajas no pueden
--     cobrar la misma.
--
-- payload es el carrito como lo arma el punto de venta. El servidor no lo
-- interpreta: al cobrar recibe una venta normal y la valida entera.
--
-- Idempotente.
-- =============================================================================

BEGIN;

CREATE TABLE IF NOT EXISTS public.held_sales (
    id          uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   integer       NOT NULL DEFAULT public.current_tenant() REFERENCES sec.tenants(id),
    branch_id   uuid          NOT NULL DEFAULT public.current_branch(),
    user_id     integer       NOT NULL,
    customer_id uuid,
    -- Para reconocerla en la lista: "señor de camisa azul", "mesa 3".
    label       varchar(100)  NOT NULL DEFAULT '',
    items_count integer       NOT NULL DEFAULT 0,
    total       numeric(12,2) NOT NULL DEFAULT 0,
    payload     jsonb         NOT NULL,
    created     timestamptz   NOT NULL DEFAULT now(),
    CONSTRAINT held_sales_sucursal_fk
        FOREIGN KEY (tenant_id, branch_id) REFERENCES public.branches (tenant_id, id),
    CONSTRAINT held_sales_usuario_fk
        FOREIGN KEY (tenant_id, user_id) REFERENCES sec.users (tenant_id, id),
    CONSTRAINT held_sales_cliente_fk
        FOREIGN KEY (tenant_id, customer_id) REFERENCES public.customers (tenant_id, id)
);

COMMENT ON TABLE public.held_sales IS
    'Ventas en espera de una sucursal. No reservan stock ni precios: al cobrarlas se '
    'validan como cualquier venta.';

CREATE INDEX IF NOT EXISTS idx_held_sales_sucursal
    ON public.held_sales (tenant_id, branch_id, created);

GRANT SELECT, INSERT, UPDATE, DELETE ON public.held_sales TO app_pos;

ALTER TABLE public.held_sales ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.held_sales FORCE  ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_aislado ON public.held_sales;
CREATE POLICY tenant_aislado ON public.held_sales
    USING      (tenant_id = public.current_tenant())
    WITH CHECK (tenant_id = public.current_tenant());

COMMIT;
