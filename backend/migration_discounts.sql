-- ============================================================
-- MIGRATION: Discounts system
-- ============================================================

-- 1. Complete the discounts table (add audit + active columns)
ALTER TABLE public.discounts
    ADD COLUMN IF NOT EXISTS is_active   boolean                     NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS state       boolean                     NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS created_by  integer                     NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS created     timestamp with time zone    NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS modified_by integer                     NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS modified    timestamp with time zone    NOT NULL DEFAULT now();

-- Add PK if missing
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE table_name = 'discounts' AND constraint_type = 'PRIMARY KEY'
    ) THEN
        ALTER TABLE public.discounts ADD PRIMARY KEY (id);
    END IF;
END $$;

-- 2. Complete the sale_detail_discounts table
ALTER TABLE public.sale_detail_discounts
    ADD COLUMN IF NOT EXISTS applied_amount  numeric(10,2)               NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS state           boolean                     NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS created_by      integer                     NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS created         timestamp with time zone    NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS modified_by     integer                     NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS modified        timestamp with time zone    NOT NULL DEFAULT now();

-- Migrate existing applied_value (varchar) -> applied_amount (numeric)
UPDATE public.sale_detail_discounts
   SET applied_amount = CAST(applied_value AS numeric)
 WHERE applied_value IS NOT NULL AND applied_value ~ '^\d+(\.\d+)?$';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE table_name = 'sale_detail_discounts' AND constraint_type = 'PRIMARY KEY'
    ) THEN
        ALTER TABLE public.sale_detail_discounts ADD PRIMARY KEY (id);
    END IF;
END $$;

-- 3. Add header-level discount columns to sales
ALTER TABLE public.sales
    ADD COLUMN IF NOT EXISTS header_discount_id     uuid           NULL,
    ADD COLUMN IF NOT EXISTS header_discount_amount numeric(10,2)  NOT NULL DEFAULT 0;

-- 4. Add discount_id to sales_detail (tracks which predefined discount was applied)
ALTER TABLE public.sales_detail
    ADD COLUMN IF NOT EXISTS discount_id uuid NULL;
