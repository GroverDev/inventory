-- ============================================================
-- Migración: payment_methods + sale_payments
-- ============================================================

-- 1. Agregar columna icon_css si no existe
ALTER TABLE payment_methods
    ADD COLUMN IF NOT EXISTS icon_css VARCHAR(60) NOT NULL DEFAULT 'fal fa-money-bill';

-- 2. Limpiar filas sin ID (seed anterior fallido) y volver a insertar con UUIDs fijos
DELETE FROM payment_methods WHERE id IS NULL;

INSERT INTO payment_methods (id, name, icon_css, requires_changes, state, created_by, created, modified_by, modified)
SELECT 'a1000000-0000-0000-0000-000000000001', 'Efectivo', 'fal fa-money-bill-wave', TRUE,  TRUE, 1, NOW(), 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM payment_methods WHERE name = 'Efectivo');

INSERT INTO payment_methods (id, name, icon_css, requires_changes, state, created_by, created, modified_by, modified)
SELECT 'a1000000-0000-0000-0000-000000000002', 'Tarjeta',  'fal fa-credit-card',     FALSE, TRUE, 1, NOW(), 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM payment_methods WHERE name = 'Tarjeta');

INSERT INTO payment_methods (id, name, icon_css, requires_changes, state, created_by, created, modified_by, modified)
SELECT 'a1000000-0000-0000-0000-000000000003', 'QR',       'fal fa-qrcode',          FALSE, TRUE, 1, NOW(), 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM payment_methods WHERE name = 'QR');
