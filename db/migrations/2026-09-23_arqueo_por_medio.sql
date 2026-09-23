-- =============================================================================
-- Arqueo por medio de pago y conteo a ciegas en el cierre de caja
-- =============================================================================
-- Hasta acá el cierre declaraba solo el efectivo, viendo antes el esperado.
-- Ahora:
--   * Cada medio de pago puede arquearse al cierre (requires_count). El
--     efectivo se arquea siempre; tarjeta y QR, si se activa: se declaran con el
--     cierre de lote del datáfono y lo recibido en la cuenta.
--   * El cajero declara sin ver lo esperado (conteo a ciegas). El servidor
--     calcula esperado, declarado y diferencia por medio, y los guarda en
--     cash_session_counts.
--   * Si una diferencia supera note_threshold, el cierre exige una observación.
--     Cada intento rechazado suma en cash_sessions.close_attempts: un supervisor
--     ve si se probaron cifras hasta que cerró.
--
-- Idempotente.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Qué medios se arquean
-- -----------------------------------------------------------------------------
ALTER TABLE public.payment_methods
    ADD COLUMN IF NOT EXISTS requires_count boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.payment_methods.requires_count IS
    'Se declara al cerrar la caja. El efectivo (affects_cash) se arquea siempre, '
    'tenga o no esta marca.';

-- El efectivo, marcado desde ya para que la configuración lo muestre como está.
UPDATE public.payment_methods SET requires_count = true
 WHERE affects_cash AND NOT requires_count;

-- -----------------------------------------------------------------------------
-- 2. Lo declarado en cada cierre, por medio
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.cash_session_counts (
    tenant_id         integer       NOT NULL DEFAULT public.current_tenant(),
    cash_session_id   uuid          NOT NULL,
    payment_method_id uuid          NOT NULL,
    expected          numeric(12,2) NOT NULL,
    declared          numeric(12,2) NOT NULL CHECK (declared >= 0),
    difference        numeric(12,2) NOT NULL,
    PRIMARY KEY (cash_session_id, payment_method_id),
    -- Sin FK a payment_methods: esa tabla no tiene clave primaria (tampoco la
    -- referencia sale_payments). Agregársela es otro cambio de esquema.
    CONSTRAINT cash_session_counts_turno_fk
        FOREIGN KEY (tenant_id, cash_session_id) REFERENCES public.cash_sessions (tenant_id, id)
);

COMMENT ON TABLE public.cash_session_counts IS
    'Arqueo del cierre por medio de pago: esperado según el sistema, declarado por '
    'el cajero sin verlo, y la diferencia.';

GRANT SELECT, INSERT, UPDATE, DELETE ON public.cash_session_counts TO app_pos;

ALTER TABLE public.cash_session_counts ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.cash_session_counts FORCE  ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_aislado ON public.cash_session_counts;
CREATE POLICY tenant_aislado ON public.cash_session_counts
    USING      (tenant_id = public.current_tenant())
    WITH CHECK (tenant_id = public.current_tenant());

-- -----------------------------------------------------------------------------
-- 3. Intentos de cierre rechazados
-- -----------------------------------------------------------------------------
ALTER TABLE public.cash_sessions
    ADD COLUMN IF NOT EXISTS close_attempts integer NOT NULL DEFAULT 0;

COMMENT ON COLUMN public.cash_sessions.close_attempts IS
    'Cierres rechazados por diferencia sin observación. Con el conteo a ciegas, '
    'varios intentos pueden indicar que se ajustaron las cifras hasta que cerró.';

-- -----------------------------------------------------------------------------
-- 4. Configuración del cierre, por empresa
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.cash_close_settings (
    tenant_id      integer       PRIMARY KEY DEFAULT public.current_tenant() REFERENCES sec.tenants(id),
    -- Diferencia (en valor absoluto, por medio) a partir de la cual el cierre
    -- exige una observación.
    note_threshold numeric(10,2) NOT NULL DEFAULT 10 CHECK (note_threshold >= 0),
    modified_by    integer       NOT NULL DEFAULT 0,
    modified       timestamptz   NOT NULL DEFAULT now()
);

GRANT SELECT, INSERT, UPDATE, DELETE ON public.cash_close_settings TO app_pos;

ALTER TABLE public.cash_close_settings ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.cash_close_settings FORCE  ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_aislado ON public.cash_close_settings;
CREATE POLICY tenant_aislado ON public.cash_close_settings
    USING      (tenant_id = public.current_tenant())
    WITH CHECK (tenant_id = public.current_tenant());

-- -----------------------------------------------------------------------------
-- 5. Menú: "Configuración de Cierre", junto a Turnos de Caja
-- -----------------------------------------------------------------------------
-- Los permisos se copian de discounts-admin (configuración de ventas, de
-- administración); la pantalla va en el menú junto a cash-sessions.
DO $$
DECLARE
    v_form   integer;
    v_padre  integer;
    v_source integer;
BEGIN
    SELECT f.form_id INTO v_padre  FROM sec.forms f WHERE f.route = 'cash-sessions'   AND f.state;
    SELECT f.id      INTO v_source FROM sec.forms f WHERE f.route = 'discounts-admin' AND f.state;

    IF v_padre IS NULL OR v_source IS NULL THEN
        RAISE EXCEPTION 'No se encontraron "cash-sessions" y "discounts-admin"; el menú no tiene la forma esperada.';
    END IF;

    SELECT id INTO v_form FROM sec.forms WHERE route = 'cash-close-settings';

    IF v_form IS NULL THEN
        v_form := set_sequences_key('sec.forms');

        INSERT INTO sec.forms
            (id, form_id, name_form, description, icon_css, show_order, route,
             show_menu, is_form_register, module_id, state,
             created_by, created, modified_by, modified, controller)
        SELECT v_form, v_padre, 'Configuración de Cierre',
               'Qué medios se arquean y cuándo se exige observación', 'fal fa-cogs', 3,
               'cash-close-settings', true, true, f.module_id, true,
               1, now(), 1, now(), 'ninguno'
          FROM sec.forms f WHERE f.route = 'cash-sessions';
    END IF;

    INSERT INTO sec.roles_forms
        (rol_id, form_id, can_create, can_read, can_update, can_delete,
         state, created_by, created, modified_by, modified, tenant_id)
    SELECT rf.rol_id, v_form, rf.can_create, rf.can_read, rf.can_update, rf.can_delete,
           true, 1, now(), 1, now(), rf.tenant_id
      FROM sec.roles_forms rf
     -- NULL cuenta como permitido: así lo lee el menú (COALESCE(can_read, true)).
     WHERE rf.form_id = v_source AND rf.state AND COALESCE(rf.can_read, true)
       AND NOT EXISTS (SELECT 1 FROM sec.roles_forms x
                        WHERE x.rol_id = rf.rol_id AND x.form_id = v_form);
END $$;

COMMIT;
