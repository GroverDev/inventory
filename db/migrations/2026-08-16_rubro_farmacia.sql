-- -----------------------------------------------------------------------------
-- Rubro farmacia: datos del producto propios del negocio farmacéutico
-- -----------------------------------------------------------------------------
-- Todo lo de acá vive en tablas APARTE, no en columnas de `products`. Es
-- deliberado: el núcleo tiene que seguir sirviendo para una ferretería o un
-- minimarket, y una empresa de otro rubro simplemente no tiene filas en estas
-- tablas. Meter `dosis` o `via` como columnas de products sería el puente
-- quemado: el día que entre otro rubro, ya no hay vuelta.
--
-- La única excepción es `requires_prescription`, que va en products a propósito:
-- cambia el comportamiento de la venta, el código lo consulta seguido, y NO es
-- exclusivo de farmacia (una ferretería con químicos controlados tiene el mismo
-- problema). Por eso el nombre es genérico y no habla de recetas médicas.
--
-- Hoy la información farmacéutica está metida como texto dentro del nombre
-- ("IBUPROFENO 200 MG DELTA" = principio activo + concentración + marca), y por
-- eso no se puede buscar por principio activo ni ofrecer equivalentes. Esto lo
-- separa.
--
-- Idempotente.

-- -----------------------------------------------------------------------------
-- 1. Catálogo de sustancias
-- -----------------------------------------------------------------------------
-- Principios activos y excipientes en el mismo catálogo: son sustancias, y lo
-- que cambia es el PAPEL que cumplen en cada producto, no la sustancia en sí.
-- Por eso "es principio activo" se marca en la relación y no acá.
--
-- El grupo terapéutico habilita sugerir productos de la misma acción cuando no
-- hay equivalente exacto (paracetamol cuando falta ibuprofeno). Es sugerencia
-- comercial, no equivalencia clínica: la decisión sigue siendo del farmacéutico.
CREATE TABLE IF NOT EXISTS public.pharma_substances (
    id                uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         integer     NOT NULL DEFAULT public.current_tenant(),

    substance_name    varchar(150) NOT NULL,
    therapeutic_group varchar(100),

    state             boolean     NOT NULL DEFAULT true,
    created_by        integer     NOT NULL DEFAULT 0,
    created           timestamptz NOT NULL DEFAULT now(),
    modified_by       integer     NOT NULL DEFAULT 0,
    modified          timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT pharma_substances_tenant_id_uk UNIQUE (tenant_id, id)
);

-- Una sustancia no se repite dentro de una farmacia: si "IBUPROFENO" entra dos
-- veces, la búsqueda de equivalentes se parte en dos mitades que no se ven.
CREATE UNIQUE INDEX IF NOT EXISTS pharma_substances_nombre_uk
    ON public.pharma_substances (tenant_id, upper(trim(substance_name)))
    WHERE state;

-- -----------------------------------------------------------------------------
-- 2. Composición del producto
-- -----------------------------------------------------------------------------
-- La concentración vive ACÁ y no en la sustancia: dos cremas con ácido
-- salicílico al 2% y al 5% comparten la sustancia y difieren en la proporción.
-- Y un producto puede tener varias — los antigripales son casi siempre
-- combinaciones (paracetamol + clorfenamina + fenilefrina).
--
-- Los excipientes (lactosa, gluten, azúcar) van en la misma tabla marcados como
-- tales: no sirven para buscar equivalentes, pero sí importan clínicamente y son
-- justo lo que se pregunta en el mostrador.
CREATE TABLE IF NOT EXISTS public.product_components (
    id                  uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           integer     NOT NULL DEFAULT public.current_tenant(),

    product_id          uuid        NOT NULL,
    substance_id        uuid        NOT NULL,

    -- Separados en número y unidad para poder COMPARAR: "200 mg" contra
    -- "200 mg" como texto funciona por casualidad, y contra "200mg" ya no.
    -- La comparación es lo que hace posible detectar equivalentes solo.
    concentration_value numeric(12,4),
    concentration_unit  varchar(20),

    is_active_ingredient boolean    NOT NULL DEFAULT true,
    show_order          integer     NOT NULL DEFAULT 0,

    state               boolean     NOT NULL DEFAULT true,
    created_by          integer     NOT NULL DEFAULT 0,
    created             timestamptz NOT NULL DEFAULT now(),
    modified_by         integer     NOT NULL DEFAULT 0,
    modified            timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT product_components_tenant_id_uk UNIQUE (tenant_id, id),
    CONSTRAINT fk_product_components_product
        FOREIGN KEY (tenant_id, product_id) REFERENCES public.products (tenant_id, id),
    CONSTRAINT fk_product_components_substance
        FOREIGN KEY (tenant_id, substance_id) REFERENCES public.pharma_substances (tenant_id, id)
);

-- La misma sustancia no se carga dos veces en el mismo producto.
CREATE UNIQUE INDEX IF NOT EXISTS product_components_uk
    ON public.product_components (tenant_id, product_id, substance_id)
    WHERE state;

-- Ruta caliente: "¿qué productos tienen ibuprofeno?"
CREATE INDEX IF NOT EXISTS product_components_por_sustancia
    ON public.product_components (tenant_id, substance_id)
    WHERE state AND is_active_ingredient;

-- -----------------------------------------------------------------------------
-- 3. Catálogos de forma y vía
-- -----------------------------------------------------------------------------
-- Listas cerradas y cortas. Como catálogo se puede filtrar de verdad; como texto
-- libre terminarían conviviendo "jarabe", "Jarabe" y "JBE".
CREATE TABLE IF NOT EXISTS public.pharma_forms (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   integer     NOT NULL DEFAULT public.current_tenant(),
    form_name   varchar(80) NOT NULL,
    state       boolean     NOT NULL DEFAULT true,
    created_by  integer     NOT NULL DEFAULT 0,
    created     timestamptz NOT NULL DEFAULT now(),
    modified_by integer     NOT NULL DEFAULT 0,
    modified    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pharma_forms_tenant_id_uk UNIQUE (tenant_id, id)
);

CREATE TABLE IF NOT EXISTS public.pharma_routes (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   integer     NOT NULL DEFAULT public.current_tenant(),
    route_name  varchar(80) NOT NULL,
    state       boolean     NOT NULL DEFAULT true,
    created_by  integer     NOT NULL DEFAULT 0,
    created     timestamptz NOT NULL DEFAULT now(),
    modified_by integer     NOT NULL DEFAULT 0,
    modified    timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pharma_routes_tenant_id_uk UNIQUE (tenant_id, id)
);

-- -----------------------------------------------------------------------------
-- 4. Datos farmacéuticos del producto
-- -----------------------------------------------------------------------------
-- Uno a uno con el producto, y solo para los que lo necesiten: un minimarket no
-- tiene ni una fila acá.
CREATE TABLE IF NOT EXISTS public.product_pharma (
    product_id          uuid        PRIMARY KEY,
    tenant_id           integer     NOT NULL DEFAULT public.current_tenant(),

    form_id             uuid,
    route_id            uuid,

    -- "caja x 20 comprimidos", "frasco 120 ml". Es lo que distingue dos
    -- productos con el mismo principio activo y concentración.
    presentation        varchar(150),

    -- Del prospecto, para consulta del mostrador. NO es una recomendación del
    -- sistema: la dosis real depende del paciente, y el sistema no la calcula.
    dosage_reference    varchar(300),

    -- 'generico' | 'marca' | 'similar'. Lista corta y no booleano: en la
    -- práctica no siempre es binario.
    product_type        varchar(20),

    -- Registro AGEMED. Indexado para buscarlo, NO único: el mismo número puede
    -- aparecer legítimamente en dos presentaciones, y un constraint acá
    -- bloquearía una carga en el peor momento.
    sanitary_registry        varchar(60),
    -- El registro también vence, y un producto con registro vencido no debería
    -- venderse. Guardar solo el número perdería esa mitad del dato.
    sanitary_registry_expiry date,

    state               boolean     NOT NULL DEFAULT true,
    created_by          integer     NOT NULL DEFAULT 0,
    created             timestamptz NOT NULL DEFAULT now(),
    modified_by         integer     NOT NULL DEFAULT 0,
    modified            timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT product_pharma_tenant_id_uk UNIQUE (tenant_id, product_id),
    CONSTRAINT fk_product_pharma_product
        FOREIGN KEY (tenant_id, product_id) REFERENCES public.products (tenant_id, id),
    CONSTRAINT fk_product_pharma_form
        FOREIGN KEY (tenant_id, form_id)  REFERENCES public.pharma_forms  (tenant_id, id),
    CONSTRAINT fk_product_pharma_route
        FOREIGN KEY (tenant_id, route_id) REFERENCES public.pharma_routes (tenant_id, id),
    CONSTRAINT product_pharma_tipo_check
        CHECK (product_type IS NULL OR product_type IN ('generico', 'marca', 'similar'))
);

CREATE INDEX IF NOT EXISTS product_pharma_registro
    ON public.product_pharma (tenant_id, sanitary_registry)
    WHERE sanitary_registry IS NOT NULL;

-- -----------------------------------------------------------------------------
-- 5. Prospecto
-- -----------------------------------------------------------------------------
-- Tabla aparte y no una columna de products por peso: un prospecto son varios KB
-- y la consulta del punto de venta trae cientos de productos. Acá el texto se
-- carga solo cuando alguien lo abre, y la mayoría de los productos no tiene fila.
--
-- `text` y no varchar(N): PostgreSQL comprime los valores grandes solo, y un
-- límite arbitrario molesta el día que llega un prospecto largo.
CREATE TABLE IF NOT EXISTS public.product_leaflet (
    product_id  uuid        PRIMARY KEY,
    tenant_id   integer     NOT NULL DEFAULT public.current_tenant(),

    -- Markdown. Se renderiza con sanitizado: el texto suele venir copiado de una
    -- web y puede traer HTML pegado.
    content     text        NOT NULL,

    state       boolean     NOT NULL DEFAULT true,
    created_by  integer     NOT NULL DEFAULT 0,
    created     timestamptz NOT NULL DEFAULT now(),
    modified_by integer     NOT NULL DEFAULT 0,
    modified    timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT product_leaflet_tenant_id_uk UNIQUE (tenant_id, product_id),
    CONSTRAINT fk_product_leaflet_product
        FOREIGN KEY (tenant_id, product_id) REFERENCES public.products (tenant_id, id)
);

-- -----------------------------------------------------------------------------
-- 6. Alternativas definidas a mano
-- -----------------------------------------------------------------------------
-- Los equivalentes por composición NO se cargan acá: se deducen solos de
-- product_components. Esta tabla es para lo que no se puede deducir — la
-- alternativa comercial, la más económica, la que el cliente suele preferir.
--
-- Dirigida a propósito: "cuando pidan la marca cara, ofrecé la barata" no vale
-- al revés. Si se quiere en ambos sentidos, se cargan las dos filas.
CREATE TABLE IF NOT EXISTS public.product_alternatives (
    id             uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id      integer     NOT NULL DEFAULT public.current_tenant(),

    product_id     uuid        NOT NULL,
    alternative_id uuid        NOT NULL,

    -- "más económico", "misma acción", "el cliente lo prefiere". Quien venda
    -- dentro de seis meses no va a saber por qué está ahí.
    reason         varchar(150),
    show_order     integer     NOT NULL DEFAULT 0,

    state          boolean     NOT NULL DEFAULT true,
    created_by     integer     NOT NULL DEFAULT 0,
    created        timestamptz NOT NULL DEFAULT now(),
    modified_by    integer     NOT NULL DEFAULT 0,
    modified       timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT product_alternatives_tenant_id_uk UNIQUE (tenant_id, id),
    CONSTRAINT fk_product_alternatives_product
        FOREIGN KEY (tenant_id, product_id)     REFERENCES public.products (tenant_id, id),
    CONSTRAINT fk_product_alternatives_alt
        FOREIGN KEY (tenant_id, alternative_id) REFERENCES public.products (tenant_id, id),
    -- Un producto no es alternativa de sí mismo.
    CONSTRAINT product_alternatives_distintos CHECK (product_id <> alternative_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS product_alternatives_uk
    ON public.product_alternatives (tenant_id, product_id, alternative_id)
    WHERE state;

-- -----------------------------------------------------------------------------
-- 7. Requiere autorización para vender (NÚCLEO, no farmacia)
-- -----------------------------------------------------------------------------
-- Nombre genérico a propósito: en farmacia es la receta, pero una ferretería con
-- químicos controlados o una tienda con alcohol tienen la misma necesidad. El
-- comportamiento queda en el núcleo y solo el vocabulario es del rubro.
ALTER TABLE public.products
    ADD COLUMN IF NOT EXISTS requires_authorization boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN public.products.requires_authorization IS
    'La venta exige respaldo (receta médica, permiso). Genérico a propósito: no '
    'es exclusivo de farmacia.';

-- -----------------------------------------------------------------------------
-- 8. RLS en todas las tablas nuevas
-- -----------------------------------------------------------------------------
-- Sin esto el aislamiento entre farmacias se rompe, y PoliticasTests lo detecta:
-- tiene un guardarraíl que falla ante cualquier tabla con tenant_id sin política.
DO $$
DECLARE
    t text;
BEGIN
    FOREACH t IN ARRAY ARRAY[
        'public.pharma_substances',
        'public.product_components',
        'public.pharma_forms',
        'public.pharma_routes',
        'public.product_pharma',
        'public.product_leaflet',
        'public.product_alternatives'
    ]
    LOOP
        EXECUTE format('ALTER TABLE %s ENABLE ROW LEVEL SECURITY', t);
        EXECUTE format('ALTER TABLE %s FORCE ROW LEVEL SECURITY', t);
        EXECUTE format('DROP POLICY IF EXISTS tenant_aislado ON %s', t);
        EXECUTE format($f$
            CREATE POLICY tenant_aislado ON %s
                USING      (tenant_id = public.current_tenant())
                WITH CHECK (tenant_id = public.current_tenant())
        $f$, t);
        EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON %s TO app_pos', t);
    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- 9. Catálogos iniciales para la farmacia que ya existe
-- -----------------------------------------------------------------------------
-- Solo el tenant 1, que es el único hoy. Las empresas nuevas los reciben desde
-- fn_seed_tenant_master_data cuando se implemente la instalación del rubro.
INSERT INTO public.pharma_forms (tenant_id, form_name)
SELECT 1, f FROM unnest(ARRAY[
    'Comprimido', 'Cápsula', 'Jarabe', 'Suspensión', 'Gotas', 'Crema',
    'Ungüento', 'Gel', 'Inyectable', 'Supositorio', 'Óvulo', 'Parche',
    'Spray', 'Colirio', 'Polvo'
]) AS f
WHERE NOT EXISTS (SELECT 1 FROM public.pharma_forms WHERE tenant_id = 1);

INSERT INTO public.pharma_routes (tenant_id, route_name)
SELECT 1, r FROM unnest(ARRAY[
    'Oral', 'Tópica', 'Oftálmica', 'Ótica', 'Nasal', 'Rectal', 'Vaginal',
    'Intramuscular', 'Intravenosa', 'Subcutánea', 'Inhalatoria'
]) AS r
WHERE NOT EXISTS (SELECT 1 FROM public.pharma_routes WHERE tenant_id = 1);
