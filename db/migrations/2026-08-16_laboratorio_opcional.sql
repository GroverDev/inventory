-- -----------------------------------------------------------------------------
-- El laboratorio deja de ser obligatorio en el producto
-- -----------------------------------------------------------------------------
-- Hasta acá `products.laboratory_id` era NOT NULL y las consultas de producto
-- hacían INNER JOIN contra laboratories: un producto sin laboratorio no solo no
-- se podía crear, sino que además habría DESAPARECIDO de todas las listas.
--
-- No todo lo que vende una farmacia tiene laboratorio: los accesorios, los
-- productos de limpieza y la mercadería genérica no lo tienen, y hoy obligan a
-- inventar uno. La categoría ya era opcional (nullable + LEFT JOIN); esto le da
-- al laboratorio el mismo tratamiento.
--
-- La FK compuesta (tenant_id, laboratory_id) sigue vigente: con MATCH SIMPLE,
-- una fila con laboratory_id NULL la satisface sin más. Idempotente.

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
         WHERE table_schema = 'public'
           AND table_name   = 'products'
           AND column_name  = 'laboratory_id'
           AND is_nullable  = 'NO'
    ) THEN
        ALTER TABLE public.products ALTER COLUMN laboratory_id DROP NOT NULL;
        RAISE NOTICE 'products.laboratory_id ahora admite NULL.';
    ELSE
        RAISE NOTICE 'products.laboratory_id ya admitía NULL.';
    END IF;
END $$;

COMMENT ON COLUMN public.products.laboratory_id IS
    'Laboratorio o proveedor del producto. Opcional: no toda la mercadería que '
    'vende una farmacia tiene laboratorio.';
