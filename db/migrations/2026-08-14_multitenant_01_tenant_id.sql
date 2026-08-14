-- =============================================================================
-- Multi-tenant, paso 1 de 2: columna tenant_id
-- =============================================================================
-- Este script NO activa RLS. Solo agrega la estructura y deja la aplicación
-- funcionando exactamente igual que hoy (todo pertenece al tenant 1).
--
-- El RLS va en el paso 2, DESPUÉS de que el backend ya esté enviando
-- app.tenant_id en cada conexión. Si se activa antes, toda consulta devuelve
-- cero filas y la aplicación queda inutilizable.
--
-- Idempotente: se puede correr más de una vez sin efectos secundarios.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Catálogo de tenants
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS sec.tenants (
    id        serial       PRIMARY KEY,
    name      varchar(150) NOT NULL,
    slug      varchar(60)  NOT NULL UNIQUE,
    is_active boolean      NOT NULL DEFAULT true,
    created   timestamptz  NOT NULL DEFAULT now(),
    modified  timestamptz  NOT NULL DEFAULT now()
);

COMMENT ON TABLE  sec.tenants      IS 'Empresas cliente. Cada farmacia/tienda es un tenant.';
COMMENT ON COLUMN sec.tenants.slug IS 'Identificador corto y estable, usable en URL o subdominio.';

-- El cliente que ya existe en producción pasa a ser el tenant 1.
INSERT INTO sec.tenants (id, name, slug)
VALUES (1, 'Cliente inicial', 'default')
ON CONFLICT (id) DO NOTHING;

SELECT setval(
    pg_get_serial_sequence('sec.tenants', 'id'),
    GREATEST((SELECT max(id) FROM sec.tenants), 1)
);

-- -----------------------------------------------------------------------------
-- 2. tenant_id NOT NULL en las tablas de negocio
-- -----------------------------------------------------------------------------
-- DEFAULT 1 es deliberado y temporal: mantiene funcionando los INSERT actuales,
-- que todavía no envían tenant_id. El paso 2 lo reemplaza por el valor de la
-- variable de sesión.
DO $$
DECLARE
    t         text;
    tabla     text;
    esquema   text;
    solo_tabla text;
    tablas    text[] := ARRAY[
        -- public: datos de negocio
        'public.cash_movements',
        'public.cash_sessions',
        'public.categories',
        'public.customers',
        'public.discounts',
        'public.laboratories',
        'public.payment_methods',
        'public.products',
        'public.products_providers',
        'public.providers',
        'public.purchases',
        'public.purchases_delivery',
        'public.purchases_delivery_detail',
        'public.purchases_detail',
        'public.sale_detail_discounts',
        'public.sale_payments',
        'public.sale_return_detail',
        'public.sale_returns',
        'public.sales',
        'public.sales_detail',
        'public.sequences_key',
        'public.stock_movements',
        'public.unit_of_measurement',
        -- sec: seguridad por tenant
        'sec.users',
        'sec.roles',
        'sec.roles_forms',
        'sec.users_roles',
        'sec.refresh_tokens',
        'sec.user_mfa',
        'sec.user_mfa_recovery_codes',
        'sec.users_changepass',
        'sec.users_resetpass'
    ];
BEGIN
    FOREACH t IN ARRAY tablas LOOP
        esquema    := split_part(t, '.', 1);
        solo_tabla := split_part(t, '.', 2);
        tabla      := format('%I.%I', esquema, solo_tabla);

        EXECUTE format('ALTER TABLE %s ADD COLUMN IF NOT EXISTS tenant_id integer', tabla);
        EXECUTE format('UPDATE %s SET tenant_id = 1 WHERE tenant_id IS NULL', tabla);
        EXECUTE format('ALTER TABLE %s ALTER COLUMN tenant_id SET DEFAULT 1', tabla);
        EXECUTE format('ALTER TABLE %s ALTER COLUMN tenant_id SET NOT NULL', tabla);

        -- FK al catálogo de tenants
        IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = format('fk_%s_tenant', solo_tabla)
              AND conrelid = tabla::regclass
        ) THEN
            EXECUTE format(
                'ALTER TABLE %s ADD CONSTRAINT %I FOREIGN KEY (tenant_id) REFERENCES sec.tenants(id)',
                tabla, format('fk_%s_tenant', solo_tabla)
            );
        END IF;

        EXECUTE format(
            'CREATE INDEX IF NOT EXISTS %I ON %s (tenant_id)',
            format('idx_%s_tenant', solo_tabla), tabla
        );
    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- 3. Tablas con tenant opcional
-- -----------------------------------------------------------------------------
-- sec.users_login registra intentos fallidos, incluidos los de correos que no
-- existen en ningún tenant (user_id = 0). Ahí el tenant es genuinamente
-- desconocido, así que la columna admite NULL.
ALTER TABLE sec.users_login  ADD COLUMN IF NOT EXISTS tenant_id integer
    REFERENCES sec.tenants(id);
UPDATE sec.users_login SET tenant_id = 1 WHERE tenant_id IS NULL AND user_id <> 0;
CREATE INDEX IF NOT EXISTS idx_users_login_tenant ON sec.users_login (tenant_id);

-- Logs de aplicación: tenant_id sirve para diagnosticar, no para aislar.
ALTER TABLE public.zlogs_app ADD COLUMN IF NOT EXISTS tenant_id integer;

-- -----------------------------------------------------------------------------
-- 4. Índices compuestos en las rutas calientes
-- -----------------------------------------------------------------------------
-- Con RLS, tenant_id entra como predicado en TODA consulta. Los índices que hoy
-- empiezan por otra columna dejan de servir; estos los reemplazan.
CREATE INDEX IF NOT EXISTS idx_sales_tenant_fecha
    ON public.sales (tenant_id, sale_date DESC);

CREATE INDEX IF NOT EXISTS idx_sales_tenant_sesion
    ON public.sales (tenant_id, cash_session_id);

CREATE INDEX IF NOT EXISTS idx_sales_detail_tenant_venta
    ON public.sales_detail (tenant_id, sale_id);

CREATE INDEX IF NOT EXISTS idx_products_tenant_activo
    ON public.products (tenant_id, is_active);

CREATE INDEX IF NOT EXISTS idx_stock_movements_tenant_producto
    ON public.stock_movements (tenant_id, product_id, created DESC);

CREATE INDEX IF NOT EXISTS idx_purchases_detail_tenant_compra
    ON public.purchases_detail (tenant_id, purchase_id);

CREATE INDEX IF NOT EXISTS idx_users_tenant_activo
    ON sec.users (tenant_id, is_active);

-- -----------------------------------------------------------------------------
-- 5. Unicidad que pasa a ser por tenant
-- -----------------------------------------------------------------------------
-- product_code hoy es único global. Con varios tenants, dos farmacias distintas
-- pueden usar el mismo código sin que eso sea un conflicto.
ALTER TABLE public.products DROP CONSTRAINT IF EXISTS product_code_unique;
CREATE UNIQUE INDEX IF NOT EXISTS product_code_unique_tenant
    ON public.products (tenant_id, product_code)
    WHERE product_code IS NOT NULL;

-- sec.users.email y sec.users.user_name se mantienen ÚNICOS GLOBALMENTE a
-- propósito: el login no lleva selector de farmacia, así que el correo es lo
-- que permite resolver a qué tenant pertenece quien entra.

COMMIT;
