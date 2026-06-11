-- ============================================================
-- Migración: sale_returns + sale_return_detail
-- ============================================================

CREATE TABLE IF NOT EXISTS sale_returns (
    id              UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    sale_id         UUID         NOT NULL REFERENCES sales(id),
    return_date     TIMESTAMP    NOT NULL DEFAULT NOW(),
    reason          VARCHAR(255),
    total_returned  DECIMAL(18,2) NOT NULL DEFAULT 0,
    is_full_return  BOOLEAN      NOT NULL DEFAULT FALSE,
    state           BOOLEAN      NOT NULL DEFAULT TRUE,
    created_by      INT          NOT NULL,
    created         TIMESTAMP    NOT NULL DEFAULT NOW(),
    modified_by     INT          NOT NULL,
    modified        TIMESTAMP    NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS sale_return_detail (
    id                UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    return_id         UUID         NOT NULL REFERENCES sale_returns(id),
    sale_detail_id    UUID         NOT NULL REFERENCES sales_detail(id),
    product_id        UUID         NOT NULL REFERENCES products(id),
    quantity_returned INT          NOT NULL,
    unit_price        DECIMAL(18,2) NOT NULL,
    line_total        DECIMAL(18,2) NOT NULL,
    state             BOOLEAN      NOT NULL DEFAULT TRUE,
    created_by        INT          NOT NULL,
    created           TIMESTAMP    NOT NULL DEFAULT NOW(),
    modified_by       INT          NOT NULL,
    modified          TIMESTAMP    NOT NULL DEFAULT NOW()
);
