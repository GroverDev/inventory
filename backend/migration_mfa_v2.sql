-- Migración v2: MFA en tablas propias (separado de sec.users)

CREATE TABLE IF NOT EXISTS sec.user_mfa (
    id               SERIAL PRIMARY KEY,
    user_id          INT NOT NULL REFERENCES sec.users(id) ON DELETE CASCADE,
    mfa_type         VARCHAR(20) NOT NULL DEFAULT 'totp',
    secret_encrypted TEXT NULL,          -- AES-256-GCM encrypted base64
    is_enabled       BOOLEAN NOT NULL DEFAULT FALSE,
    is_required      BOOLEAN NOT NULL DEFAULT FALSE,
    failed_attempts  SMALLINT NOT NULL DEFAULT 0,
    locked_until     TIMESTAMPTZ NULL,
    enabled_at       TIMESTAMPTZ NULL,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ NULL,
    UNIQUE(user_id, mfa_type)
);

CREATE TABLE IF NOT EXISTS sec.user_mfa_recovery_codes (
    id          SERIAL PRIMARY KEY,
    user_mfa_id INT NOT NULL REFERENCES sec.user_mfa(id) ON DELETE CASCADE,
    code_hash   VARCHAR(200) NOT NULL,
    used_at     TIMESTAMPTZ NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- NOTE: Old totp_secret was stored as plain text.
-- The new secret_encrypted column expects AES-256-GCM ciphertext.
-- Migration preserves the fact that a user had TOTP enabled (is_required=true)
-- but resets is_enabled=false so users must go through setup again with encryption.
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns
             WHERE table_schema='sec' AND table_name='users' AND column_name='totp_enabled') THEN
    INSERT INTO sec.user_mfa (user_id, mfa_type, is_enabled, is_required, secret_encrypted)
    SELECT id,
           'totp',
           false,       -- force re-setup (old secrets were unencrypted)
           COALESCE(totp_enabled, false),   -- preserve required flag
           NULL         -- discard plain-text secret
    FROM sec.users
    WHERE COALESCE(totp_enabled, false) = true
    ON CONFLICT (user_id, mfa_type) DO NOTHING;
  END IF;
END $$;

-- Run manually after validating the migration:
-- ALTER TABLE sec.users DROP COLUMN IF EXISTS totp_secret;
-- ALTER TABLE sec.users DROP COLUMN IF EXISTS totp_enabled;
-- ALTER TABLE sec.users DROP COLUMN IF EXISTS totp_failed_attempts;
-- ALTER TABLE sec.users DROP COLUMN IF EXISTS totp_locked_until;
