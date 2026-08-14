-- =============================================================================
-- Integridad referencial por tenant
-- =============================================================================
-- Las claves foráneas validan que la fila referenciada exista, no que pertenezca
-- a la misma farmacia. Las comprobaciones de integridad referencial corren por
-- debajo de RLS, así que un INSERT con el UUID de un laboratorio ajeno tiene
-- éxito.
--
-- No es una fuga de datos: las lecturas siguen filtradas, y el JOIN a la tabla
-- ajena no devuelve nada. Son dos fallas de integridad, las dos difíciles de
-- diagnosticar:
--
--   1. La fila se vuelve invisible para su propio dueño. Un producto que apunta
--      al laboratorio de otra farmacia desaparece de las pantallas, porque el
--      JOIN interno no encuentra el laboratorio. El producto existe y no se ve.
--
--   2. Una farmacia puede clavar los maestros de otra. Si B referencia el
--      laboratorio de A, cuando A intente borrarlo recibe una violación de FK
--      causada por una fila que no puede ver.
--
-- La solución es que la clave foránea incluya tenant_id en ambos lados. Eso exige
-- un UNIQUE (tenant_id, id) en la tabla referenciada: redundante respecto de la
-- PK, pero PostgreSQL lo necesita como destino de una FK compuesta.
--
-- Ninguna de las FK afectadas tiene ON DELETE/UPDATE distinto de NO ACTION, así
-- que recrearlas no cambia comportamiento.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. UNIQUE (tenant_id, id) en las tablas referenciadas
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    t          text;
    esquema    text;
    solo_tabla text;
    padres     text[] := ARRAY[
        'public.cash_sessions',
        'public.categories',
        'public.customers',
        'public.laboratories',
        'public.products',
        'public.providers',
        'public.purchases',
        'public.purchases_delivery',
        'public.sale_returns',
        'public.sales',
        'public.sales_detail',
        'public.unit_of_measurement',
        'sec.users'
    ];
BEGIN
    FOREACH t IN ARRAY padres LOOP
        esquema    := split_part(t, '.', 1);
        solo_tabla := split_part(t, '.', 2);

        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = format('%s_tenant_id_uk', solo_tabla)
              AND conrelid = format('%I.%I', esquema, solo_tabla)::regclass
        ) THEN
            EXECUTE format('ALTER TABLE %I.%I ADD CONSTRAINT %I UNIQUE (tenant_id, id)',
                           esquema, solo_tabla, format('%s_tenant_id_uk', solo_tabla));
        END IF;
    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- 2. Reemplazar cada FK por su versión compuesta
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    fk      text[];
    -- {constraint, esquema_hija, tabla_hija, columna_hija, esquema_padre, tabla_padre}
    fks     text[][] := ARRAY[
        ARRAY['cash_movements_cash_session_id_fkey',    'public','cash_movements',           'cash_session_id',     'public','cash_sessions'],
        ARRAY['cash_sessions_user_id_fkey',             'public','cash_sessions',            'user_id',             'sec',   'users'],
        ARRAY['products_category_id_fkey',              'public','products',                 'category_id',         'public','categories'],
        ARRAY['products_laboratories_fk',               'public','products',                 'laboratory_id',       'public','laboratories'],
        ARRAY['products_unit_of_measurement_fk',        'public','products',                 'uom_id',              'public','unit_of_measurement'],
        ARRAY['fk_purchases_provider',                  'public','purchases',                'provider_id',         'public','providers'],
        ARRAY['fk_delivery_purchase',                   'public','purchases_delivery',       'purchase_id',         'public','purchases'],
        ARRAY['fk_deld_delivery',                       'public','purchases_delivery_detail','purchase_delivery_id','public','purchases_delivery'],
        ARRAY['fk_deld_product',                        'public','purchases_delivery_detail','product_id',          'public','products'],
        ARRAY['fk_pd_product',                          'public','purchases_detail',         'product_id',          'public','products'],
        ARRAY['fk_pd_purchase',                         'public','purchases_detail',         'purchase_id',         'public','purchases'],
        ARRAY['sale_return_detail_product_id_fkey',     'public','sale_return_detail',       'product_id',          'public','products'],
        ARRAY['sale_return_detail_return_id_fkey',      'public','sale_return_detail',       'return_id',           'public','sale_returns'],
        ARRAY['sale_return_detail_sale_detail_id_fkey', 'public','sale_return_detail',       'sale_detail_id',      'public','sales_detail'],
        ARRAY['sale_returns_sale_id_fkey',              'public','sale_returns',             'sale_id',             'public','sales'],
        ARRAY['sales_cash_session_id_fkey',             'public','sales',                    'cash_session_id',     'public','cash_sessions'],
        ARRAY['sales_customers_fk',                     'public','sales',                    'customer_id',         'public','customers'],
        ARRAY['sales_detail_products_fk',               'public','sales_detail',             'product_id',          'public','products'],
        ARRAY['sales_detail_sales_fk',                  'public','sales_detail',             'sale_id',             'public','sales'],
        ARRAY['stock_movements_product_id_fkey',        'public','stock_movements',          'product_id',          'public','products']
    ];
    i integer;
BEGIN
    FOR i IN 1 .. array_length(fks, 1) LOOP
        fk := fks[i:i][1:6];

        EXECUTE format('ALTER TABLE %I.%I DROP CONSTRAINT IF EXISTS %I',
                       fks[i][2], fks[i][3], fks[i][1]);

        EXECUTE format(
            'ALTER TABLE %I.%I ADD CONSTRAINT %I FOREIGN KEY (tenant_id, %I) REFERENCES %I.%I (tenant_id, id)',
            fks[i][2], fks[i][3], fks[i][1], fks[i][4], fks[i][5], fks[i][6]);
    END LOOP;
END $$;

COMMIT;

-- =============================================================================
-- Verificación
-- =============================================================================
--   Debe devolver 20: todas las FK entre tablas por tenant ya incluyen tenant_id.
--
--   SELECT count(*) FROM pg_constraint con
--    WHERE con.contype = 'f'
--      AND array_length(con.conkey, 1) = 2;
-- =============================================================================
