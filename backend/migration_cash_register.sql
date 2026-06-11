-- ============================================================
-- MIGRACIÓN: Gestión de Caja por Turno
-- Fecha: 2026-05-26
-- ============================================================

-- 1. Tabla de turnos de caja (una por cajero por turno)
CREATE TABLE IF NOT EXISTS cash_sessions (
    id              UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         INTEGER      NOT NULL REFERENCES sec.users(id),
    opened_at       TIMESTAMP    NOT NULL DEFAULT NOW(),
    closed_at       TIMESTAMP,
    opening_amount  DECIMAL(12,2) NOT NULL DEFAULT 0,
    declared_amount DECIMAL(12,2),
    expected_amount DECIMAL(12,2),
    difference      DECIMAL(12,2),
    notes           TEXT,
    state           BOOLEAN      NOT NULL DEFAULT TRUE,
    created_by      INTEGER      NOT NULL,
    created         TIMESTAMP    NOT NULL DEFAULT NOW(),
    modified_by     INTEGER      NOT NULL,
    modified        TIMESTAMP    NOT NULL DEFAULT NOW()
);

-- Garantiza que un cajero no tenga dos sesiones abiertas simultáneamente
CREATE UNIQUE INDEX IF NOT EXISTS uix_cash_sessions_user_open
    ON cash_sessions (user_id)
    WHERE closed_at IS NULL AND state = TRUE;

-- 2. Tabla de movimientos de caja (gastos, retiros, ingresos extra)
CREATE TABLE IF NOT EXISTS cash_movements (
    id               UUID          PRIMARY KEY DEFAULT gen_random_uuid(),
    cash_session_id  UUID          NOT NULL REFERENCES cash_sessions(id),
    movement_type    VARCHAR(20)   NOT NULL CHECK (movement_type IN ('expense','withdrawal','income')),
    amount           DECIMAL(12,2) NOT NULL CHECK (amount > 0),
    description      VARCHAR(255)  NOT NULL,
    state            BOOLEAN       NOT NULL DEFAULT TRUE,
    created_by       INTEGER       NOT NULL,
    created          TIMESTAMP     NOT NULL DEFAULT NOW(),
    modified_by      INTEGER       NOT NULL,
    modified         TIMESTAMP     NOT NULL DEFAULT NOW()
);

-- 3. Vincular ventas al turno de caja (nullable: ventas previas no tienen sesión)
ALTER TABLE sales
    ADD COLUMN IF NOT EXISTS cash_session_id UUID REFERENCES cash_sessions(id);

CREATE INDEX IF NOT EXISTS ix_sales_cash_session ON sales(cash_session_id);
CREATE INDEX IF NOT EXISTS ix_cash_movements_session ON cash_movements(cash_session_id);
CREATE INDEX IF NOT EXISTS ix_cash_sessions_user ON cash_sessions(user_id);
