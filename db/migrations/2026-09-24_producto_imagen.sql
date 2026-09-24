-- =============================================================================
-- Imagen del producto
-- =============================================================================
-- Una imagen por producto. La base guarda solo la ruta RELATIVA del archivo
-- ("{tenant}/products/{productId}/{guid}.webp"); el archivo vive en disco y lo
-- sirve Nginx desde un dominio aparte. La miniatura no tiene columna: se deriva
-- de la ruta ("{guid}_thumb.webp").
--
-- Ruta relativa y no URL para que cambiar de dominio, o pasar a S3/R2, no obligue
-- a migrar datos.
--
-- No hace falta tocar RLS: la columna es de `products`, que ya está aislada por
-- tenant.
--
-- Idempotente.
-- =============================================================================

BEGIN;

ALTER TABLE public.products
    ADD COLUMN IF NOT EXISTS image_path text;

COMMIT;
