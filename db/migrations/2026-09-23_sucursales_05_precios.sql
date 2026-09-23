-- =============================================================================
-- Sucursales, paso 5: precio y mínimo de reposición por sucursal
-- =============================================================================
-- El catálogo es uno solo por farmacia: products.sale_price y
-- products.min_reorder_quantity son los valores base. Una sucursal puede tener
-- una excepción para cualquiera de los dos; si no la tiene, usa el base.
--
-- Una fila con los dos valores nulos no significa nada: el backend la borra en
-- vez de guardarla, y la CHECK lo exige.
--
-- Mantener un precio único cuesta cero filas; subir un precio se hace en un
-- solo lugar; y una sucursal más cara (aeropuerto, zona turística) no obliga a
-- duplicar el catálogo.
--
-- Requiere el paso 1. Idempotente.
-- =============================================================================

BEGIN;

CREATE TABLE IF NOT EXISTS public.product_branch_settings (
    tenant_id            integer       NOT NULL DEFAULT public.current_tenant(),
    branch_id            uuid          NOT NULL,
    product_id           uuid          NOT NULL,
    sale_price           numeric(10,2) CHECK (sale_price >= 0),
    min_reorder_quantity integer       CHECK (min_reorder_quantity >= 0),
    created_by           integer       NOT NULL DEFAULT 0,
    created              timestamptz   NOT NULL DEFAULT now(),
    modified_by          integer       NOT NULL DEFAULT 0,
    modified             timestamptz   NOT NULL DEFAULT now(),
    PRIMARY KEY (branch_id, product_id),
    CONSTRAINT product_branch_settings_algo_propio
        CHECK (sale_price IS NOT NULL OR min_reorder_quantity IS NOT NULL),
    CONSTRAINT product_branch_settings_sucursal_fk
        FOREIGN KEY (tenant_id, branch_id)  REFERENCES public.branches (tenant_id, id),
    CONSTRAINT product_branch_settings_producto_fk
        FOREIGN KEY (tenant_id, product_id) REFERENCES public.products (tenant_id, id)
);

COMMENT ON TABLE public.product_branch_settings IS
    'Excepciones de precio y de mínimo de reposición por sucursal. Sin fila, la '
    'sucursal usa los valores base de products.';

CREATE INDEX IF NOT EXISTS idx_product_branch_settings_producto
    ON public.product_branch_settings (tenant_id, product_id);

GRANT SELECT, INSERT, UPDATE, DELETE ON public.product_branch_settings TO app_pos;

ALTER TABLE public.product_branch_settings ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.product_branch_settings FORCE  ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_aislado ON public.product_branch_settings;
CREATE POLICY tenant_aislado ON public.product_branch_settings
    USING      (tenant_id = public.current_tenant())
    WITH CHECK (tenant_id = public.current_tenant());

-- El precio de venta de un producto en una sucursal. Es la regla en un solo
-- lugar, para SQL que la necesite (reportes, funciones). Los listados del
-- backend hacen el mismo COALESCE con un LEFT JOIN, que rinde mejor sobre
-- miles de filas que una llamada por fila.
CREATE OR REPLACE FUNCTION public.fn_precio_venta(p_product_id uuid, p_branch_id uuid DEFAULT NULL)
RETURNS numeric
LANGUAGE sql STABLE
AS $$
    SELECT COALESCE(s.sale_price, p.sale_price)
      FROM public.products p
      LEFT JOIN public.product_branch_settings s
             ON s.product_id = p.id
            AND s.branch_id = COALESCE(p_branch_id, public.current_branch())
     WHERE p.id = p_product_id;
$$;

COMMENT ON FUNCTION public.fn_precio_venta(uuid, uuid) IS
    'Precio de venta en la sucursal indicada (o la de la sesión): su excepción si la '
    'tiene, si no el precio base del producto.';

GRANT EXECUTE ON FUNCTION public.fn_precio_venta(uuid, uuid) TO app_pos;

COMMIT;
