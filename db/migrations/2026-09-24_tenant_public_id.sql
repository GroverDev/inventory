-- =============================================================================
-- Identificador público del tenant
-- =============================================================================
-- El id numérico de sec.tenants es secuencial y aparecía en las URLs de las
-- imágenes ("/1/products/..."): dice cuántas empresas hay y cuál es cada una.
-- public_id es un UUID aleatorio para todo lo que sale hacia afuera (carpetas y
-- URLs de medios). La clave interna no cambia: RLS, claves foráneas compuestas y
-- el JWT siguen usando el entero.
--
--   * NO es un secreto ni sirve para autorizar: es un nombre opaco.
--   * Es INMUTABLE. Las URLs de las imágenes se cachean por un año; regenerarlo
--     dejaría a todas las fotos de la empresa sin encontrarse. Un trigger lo
--     impide, para que ni un UPDATE a mano lo cambie por descuido.
--   * Las empresas nuevas lo reciben solas (DEFAULT), así que la provisión
--     (fn_provision_tenant) no necesita cambios.
--
-- También reescribe products.image_path, que guardaba "{id}/products/..." y pasa a
-- "{public_id}/products/...". Los ARCHIVOS hay que moverlos aparte: la carpeta
-- "{id}" del directorio de medios pasa a llamarse "{public_id}" (ver DEPLOY).
--
-- Idempotente: una segunda corrida no encuentra rutas con el prefijo numérico.
-- =============================================================================

BEGIN;

-- Al agregar la columna con DEFAULT, Postgres le da su propio UUID a cada empresa
-- que ya existe.
ALTER TABLE sec.tenants
    ADD COLUMN IF NOT EXISTS public_id uuid NOT NULL DEFAULT gen_random_uuid();

CREATE UNIQUE INDEX IF NOT EXISTS tenants_public_id_key ON sec.tenants (public_id);

COMMENT ON COLUMN sec.tenants.public_id IS
    'Identificador opaco para URLs y carpetas de medios. Inmutable; no es un secreto ni sirve para autorizar.';

CREATE OR REPLACE FUNCTION sec.fn_tenants_public_id_inmutable()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF NEW.public_id IS DISTINCT FROM OLD.public_id THEN
        RAISE EXCEPTION 'El public_id de un tenant no puede cambiar: rompería las URLs de sus imágenes.';
    END IF;
    RETURN NEW;
END $$;

DROP TRIGGER IF EXISTS tenants_public_id_inmutable ON sec.tenants;
CREATE TRIGGER tenants_public_id_inmutable
    BEFORE UPDATE ON sec.tenants
    FOR EACH ROW EXECUTE FUNCTION sec.fn_tenants_public_id_inmutable();

-- "{id}/products/x.webp" -> "{public_id}/products/x.webp"
UPDATE public.products p
   SET image_path = t.public_id::text || substr(p.image_path, length(p.tenant_id::text) + 1)
  FROM sec.tenants t
 WHERE t.id = p.tenant_id
   AND p.image_path LIKE p.tenant_id::text || '/%';

COMMIT;
