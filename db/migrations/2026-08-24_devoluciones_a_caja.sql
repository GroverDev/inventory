-- -----------------------------------------------------------------------------
-- Las devoluciones llegan a la caja
-- -----------------------------------------------------------------------------
-- Registrar una devolución reponía stock y creaba el sale_return, pero no dejaba
-- ninguna huella en la caja: ni de qué sesión salió la plata, ni por qué medio
-- se reintegró. Si el reintegro era en efectivo, el cajón quedaba con menos
-- dinero del que el arqueo esperaba y el faltante se le imputaba al cajero.
--
-- Decisiones tomadas (2026-08-24):
--   * El movimiento afecta a la sesión de caja ABIERTA al momento de devolver,
--     no a la de la venta original: la plata sale del cajón de hoy.
--   * El método de reintegro se elige en el POS, precargado con el que usó la
--     venta. Solo los métodos con affects_cash generan movimiento de caja.
--   * Sin caja abierta no se puede devolver en efectivo (lo valida
--     SaleReturnApplication): si sale plata del cajón, tiene que haber cajón.
--   * La devolución no pide autorización de supervisor; queda auditada por
--     created_by y visible en el arqueo.
--
-- payment_method_id va sin FK a propósito: payment_methods no tiene primary key
-- (sale_payments.payment_method_id tampoco la referencia). Cuando se le agregue
-- la PK conviene crear las dos FKs juntas.
--
-- Idempotente.

ALTER TABLE public.sale_returns
    ADD COLUMN IF NOT EXISTS cash_session_id   uuid,
    ADD COLUMN IF NOT EXISTS payment_method_id uuid;

DO $do$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                    WHERE conname = 'sale_returns_cash_session_id_fkey') THEN
        ALTER TABLE public.sale_returns
            ADD CONSTRAINT sale_returns_cash_session_id_fkey
            FOREIGN KEY (tenant_id, cash_session_id)
            REFERENCES public.cash_sessions (tenant_id, id);
    END IF;
END $do$;

CREATE INDEX IF NOT EXISTS ix_sale_returns_cash_session
    ON public.sale_returns (tenant_id, cash_session_id);

COMMENT ON COLUMN public.sale_returns.cash_session_id IS
    'Sesión de caja de la que salió el efectivo reintegrado. NULL cuando el '
    'reintegro no fue en efectivo, o en las devoluciones anteriores a 2026-08-24, '
    'donde el dato no existe y no se puede reconstruir.';

COMMENT ON COLUMN public.sale_returns.payment_method_id IS
    'Medio por el que se reintegró al cliente. NULL en las devoluciones '
    'anteriores a 2026-08-24.';

-- 'return' como tipo propio y no reusando 'expense': el arqueo tiene que poder
-- mostrar las devoluciones separadas de los gastos operativos.
ALTER TABLE public.cash_movements
    DROP CONSTRAINT IF EXISTS cash_movements_movement_type_check;

ALTER TABLE public.cash_movements
    ADD CONSTRAINT cash_movements_movement_type_check
    CHECK (movement_type::text = ANY (ARRAY['expense', 'withdrawal', 'income', 'return']::text[]));
