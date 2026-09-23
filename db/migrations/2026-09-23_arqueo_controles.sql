-- =============================================================================
-- Cierre de caja: autorización de supervisor, límite de intentos y conteo por
-- billetes
-- =============================================================================
-- Sobre el arqueo por medio de pago (2026-09-23_arqueo_por_medio.sql):
--
--   * supervisor_threshold: si alguna diferencia lo supera, además de la
--     observación hace falta la clave de un supervisor. Nulo = no se exige.
--   * max_attempts: después de tantos cierres rechazados por diferencia sin
--     observación, el cierre pide supervisor. Nulo = sin límite.
--   * require_denominations: el efectivo se declara contando billetes y
--     monedas; el detalle queda guardado. Si es false, la grilla es opcional.
--   * close_authorized_by: quién autorizó un cierre que lo necesitaba.
--
-- Requiere 2026-09-23_arqueo_por_medio.sql. Idempotente.
-- =============================================================================

BEGIN;

ALTER TABLE public.cash_close_settings
    ADD COLUMN IF NOT EXISTS supervisor_threshold  numeric(10,2) CHECK (supervisor_threshold >= 0),
    ADD COLUMN IF NOT EXISTS max_attempts          integer       CHECK (max_attempts > 0),
    ADD COLUMN IF NOT EXISTS require_denominations boolean       NOT NULL DEFAULT false;

COMMENT ON COLUMN public.cash_close_settings.supervisor_threshold IS
    'Diferencia por medio a partir de la cual el cierre necesita un supervisor. Nulo = nunca.';
COMMENT ON COLUMN public.cash_close_settings.max_attempts IS
    'Intentos rechazados a partir de los cuales el cierre necesita un supervisor. Nulo = sin límite.';

ALTER TABLE public.cash_sessions
    ADD COLUMN IF NOT EXISTS close_authorized_by integer;

COMMENT ON COLUMN public.cash_sessions.close_authorized_by IS
    'Supervisor que autorizó el cierre, cuando la diferencia o los intentos lo exigieron.';

-- Conteo del efectivo por billete y moneda.
CREATE TABLE IF NOT EXISTS public.cash_session_denominations (
    tenant_id       integer       NOT NULL DEFAULT public.current_tenant(),
    cash_session_id uuid          NOT NULL,
    value           numeric(10,2) NOT NULL CHECK (value > 0),
    quantity        integer       NOT NULL CHECK (quantity >= 0),
    PRIMARY KEY (cash_session_id, value),
    CONSTRAINT cash_session_denominations_turno_fk
        FOREIGN KEY (tenant_id, cash_session_id) REFERENCES public.cash_sessions (tenant_id, id)
);

COMMENT ON TABLE public.cash_session_denominations IS
    'Conteo del efectivo al cierre, por billete y moneda.';

GRANT SELECT, INSERT, UPDATE, DELETE ON public.cash_session_denominations TO app_pos;

ALTER TABLE public.cash_session_denominations ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.cash_session_denominations FORCE  ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_aislado ON public.cash_session_denominations;
CREATE POLICY tenant_aislado ON public.cash_session_denominations
    USING      (tenant_id = public.current_tenant())
    WITH CHECK (tenant_id = public.current_tenant());

COMMIT;
