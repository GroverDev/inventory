-- =============================================================================
-- Sucursales, paso 4: traspasos de stock entre sucursales
-- =============================================================================
-- Un traspaso nace como borrador en la sucursal de origen, se envía (el stock
-- sale del origen y queda "en tránsito") y se recibe en la de destino (entra,
-- con el mismo lote y vencimiento). Si llega menos de lo enviado, la diferencia
-- queda registrada como merma del origen.
--
--   borrador ──enviar──▶ enviado ──recibir──▶ recibido
--      └──anular──▶ anulado
--
-- Enviar y recibir son funciones de la base, cada una en una sola
-- transacción: es el mismo criterio de "único camino" que fn_mover_stock. El
-- backend no mueve stock de traspasos por su cuenta.
--
-- Quién puede hacer qué lo decide la sucursal de la sesión: se envía desde el
-- origen y se recibe en el destino. La sesión solo puede estar en una sucursal
-- en la que el usuario esté habilitado (lo valida el login y el cambio de
-- sucursal), así que eso ya es "solo un usuario habilitado en el destino
-- puede recibir".
--
-- Requiere los pasos 1 a 3. Idempotente.
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Tablas
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.stock_transfers (
    id               uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id        integer     NOT NULL DEFAULT public.current_tenant() REFERENCES sec.tenants(id),
    -- Correlativo por farmacia, para nombrarlo de palabra ("el traspaso 12").
    number           integer     NOT NULL,
    origin_branch_id uuid        NOT NULL,
    dest_branch_id   uuid        NOT NULL,
    status           varchar(10) NOT NULL DEFAULT 'borrador',
    notes            varchar(500) NOT NULL DEFAULT '',
    sent_at          timestamptz,
    sent_by          integer,
    received_at      timestamptz,
    received_by      integer,
    state            boolean     NOT NULL DEFAULT true,
    created_by       integer     NOT NULL DEFAULT 0,
    created          timestamptz NOT NULL DEFAULT now(),
    modified_by      integer     NOT NULL DEFAULT 0,
    modified         timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT stock_transfers_status_check
        CHECK (status IN ('borrador', 'enviado', 'recibido', 'anulado')),
    CONSTRAINT stock_transfers_sucursales_distintas
        CHECK (origin_branch_id <> dest_branch_id),
    CONSTRAINT stock_transfers_numero_uk UNIQUE (tenant_id, number),
    CONSTRAINT stock_transfers_tenant_id_uk UNIQUE (tenant_id, id),
    CONSTRAINT stock_transfers_origen_fk
        FOREIGN KEY (tenant_id, origin_branch_id) REFERENCES public.branches (tenant_id, id),
    CONSTRAINT stock_transfers_destino_fk
        FOREIGN KEY (tenant_id, dest_branch_id)   REFERENCES public.branches (tenant_id, id)
);

COMMENT ON TABLE public.stock_transfers IS
    'Traspasos de stock entre sucursales de una farmacia. El stock lo mueven '
    'fn_enviar_traspaso y fn_recibir_traspaso.';

-- Lo que se pide traspasar: producto y cantidad. Es lo único editable del
-- borrador.
CREATE TABLE IF NOT EXISTS public.stock_transfer_detail (
    id          uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   integer       NOT NULL DEFAULT public.current_tenant(),
    transfer_id uuid          NOT NULL,
    product_id  uuid          NOT NULL,
    quantity    numeric(14,4) NOT NULL CHECK (quantity > 0),
    CONSTRAINT stock_transfer_detail_producto_uk UNIQUE (transfer_id, product_id),
    CONSTRAINT stock_transfer_detail_tenant_id_uk UNIQUE (tenant_id, id),
    CONSTRAINT stock_transfer_detail_traspaso_fk
        FOREIGN KEY (tenant_id, transfer_id) REFERENCES public.stock_transfers (tenant_id, id) ON DELETE CASCADE,
    CONSTRAINT stock_transfer_detail_producto_fk
        FOREIGN KEY (tenant_id, product_id)  REFERENCES public.products (tenant_id, id)
);

-- Lo que efectivamente viajó: al enviar, cada línea se reparte por FEFO entre
-- las existencias del origen, y cada existencia usada es una fila. Guarda lote,
-- vencimiento y serie porque con eso se crea la existencia en el destino.
CREATE TABLE IF NOT EXISTS public.stock_transfer_lots (
    id                   uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id            integer       NOT NULL DEFAULT public.current_tenant(),
    detail_id            uuid          NOT NULL,
    origin_stock_item_id uuid          NOT NULL,
    lot_code             varchar(50),
    expiry_date          date,
    serial_number        varchar(80),
    quantity_sent        numeric(14,4) NOT NULL CHECK (quantity_sent > 0),
    quantity_received    numeric(14,4) CHECK (quantity_received >= 0),
    dest_stock_item_id   uuid,
    CONSTRAINT stock_transfer_lots_recibido_tope CHECK (quantity_received <= quantity_sent),
    CONSTRAINT stock_transfer_lots_detalle_fk
        FOREIGN KEY (tenant_id, detail_id) REFERENCES public.stock_transfer_detail (tenant_id, id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_stock_transfers_origen  ON public.stock_transfers (tenant_id, origin_branch_id, status);
CREATE INDEX IF NOT EXISTS idx_stock_transfers_destino ON public.stock_transfers (tenant_id, dest_branch_id, status);
CREATE INDEX IF NOT EXISTS idx_stock_transfer_detail_traspaso ON public.stock_transfer_detail (transfer_id);
CREATE INDEX IF NOT EXISTS idx_stock_transfer_lots_detalle ON public.stock_transfer_lots (detail_id);

GRANT SELECT, INSERT, UPDATE, DELETE
   ON public.stock_transfers, public.stock_transfer_detail, public.stock_transfer_lots
   TO app_pos;

DO $$
DECLARE
    tabla text;
BEGIN
    FOREACH tabla IN ARRAY ARRAY['public.stock_transfers', 'public.stock_transfer_detail', 'public.stock_transfer_lots'] LOOP
        EXECUTE format('ALTER TABLE %s ENABLE ROW LEVEL SECURITY', tabla);
        EXECUTE format('ALTER TABLE %s FORCE  ROW LEVEL SECURITY', tabla);
        EXECUTE format('DROP POLICY IF EXISTS tenant_aislado ON %s', tabla);
        EXECUTE format($f$
            CREATE POLICY tenant_aislado ON %s
                USING      (tenant_id = public.current_tenant())
                WITH CHECK (tenant_id = public.current_tenant())
        $f$, tabla);
    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- 2. Series: únicas entre las existencias activas
-- -----------------------------------------------------------------------------
-- Al traspasar una unidad con serie, su existencia en el origen queda inactiva
-- y nace otra en el destino. No se mueve la misma fila de sucursal: los
-- movimientos anteriores la referencian con la sucursal de origen (FK
-- stock_movements_existencia_misma_sucursal). La serie sigue siendo única en
-- la farmacia, ahora entre las existencias activas.
DROP INDEX IF EXISTS public.stock_items_serie_uk;
CREATE UNIQUE INDEX stock_items_serie_uk
    ON public.stock_items (tenant_id, serial_number)
    WHERE serial_number IS NOT NULL AND state;

-- -----------------------------------------------------------------------------
-- 3. Registro de un movimiento de stock desde una función
-- -----------------------------------------------------------------------------
-- Los demás caminos insertan el movimiento desde el backend. Los traspasos lo
-- hacen acá porque mueven dos sucursales en la misma transacción: la sucursal
-- del movimiento es la de la existencia, no la de la sesión.
CREATE OR REPLACE FUNCTION public.fn_registrar_movimiento(
    p_product_id  uuid,
    p_item_id     uuid,
    p_branch_id   uuid,
    p_tipo        varchar,
    p_cantidad    numeric,
    p_antes       numeric,
    p_despues     numeric,
    p_transfer_id uuid,
    p_user_id     integer,
    p_reason      varchar DEFAULT NULL,
    p_observation varchar DEFAULT NULL
) RETURNS void
LANGUAGE sql
AS $$
    INSERT INTO public.stock_movements
        (product_id, stock_item_id, branch_id, movement_type, quantity, stock_before, stock_after,
         reason, observation, reference_id, reference_type, created_by, modified_by)
    VALUES (p_product_id, p_item_id, p_branch_id, p_tipo, p_cantidad::integer, p_antes::integer, p_despues::integer,
            p_reason, p_observation, p_transfer_id, 'TRASPASO', p_user_id, p_user_id);
$$;

-- -----------------------------------------------------------------------------
-- 4. Enviar
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION public.fn_enviar_traspaso(p_transfer_id uuid, p_user_id integer)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    t       record;
    d       record;
    a       record;
    v_disp  numeric;
    v_mov   record;
    v_modo  text;
BEGIN
    SELECT * INTO t FROM public.stock_transfers WHERE id = p_transfer_id AND state FOR UPDATE;

    IF t.id IS NULL THEN
        RAISE EXCEPTION 'No existe el traspaso.';
    END IF;
    IF t.status <> 'borrador' THEN
        RAISE EXCEPTION 'El traspaso % ya fue %; solo se envía un borrador.', t.number, t.status;
    END IF;
    IF t.origin_branch_id IS DISTINCT FROM public.current_branch() THEN
        RAISE EXCEPTION 'El traspaso % se envía desde su sucursal de origen.', t.number;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM public.stock_transfer_detail WHERE transfer_id = t.id) THEN
        RAISE EXCEPTION 'El traspaso % no tiene productos.', t.number;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM public.branches WHERE id = t.dest_branch_id AND state AND is_active) THEN
        RAISE EXCEPTION 'La sucursal de destino está inactiva.';
    END IF;

    FOR d IN
        SELECT sd.id, sd.product_id, sd.quantity, p.product_name, p.tracking_mode
          FROM public.stock_transfer_detail sd
          JOIN public.products p ON p.id = sd.product_id
         WHERE sd.transfer_id = t.id
         ORDER BY p.product_name
    LOOP
        -- A diferencia de la venta, un traspaso nunca deja el origen en
        -- negativo, ni siquiera sin seguimiento: mandar lo que no hay crearía
        -- stock de la nada en el destino.
        SELECT COALESCE(sum(si.quantity), 0) INTO v_disp
          FROM public.stock_items si
         WHERE si.product_id = d.product_id AND si.branch_id = t.origin_branch_id
           AND si.state AND si.quantity > 0;

        IF v_disp < d.quantity THEN
            RAISE EXCEPTION 'No hay stock suficiente de "%" para enviar: se piden % y hay %.',
                            d.product_name, d.quantity, v_disp;
        END IF;

        -- Mismo reparto que la venta: primero lo que vence antes.
        FOR a IN SELECT * FROM public.fn_asignar_fefo(d.product_id, d.quantity, t.origin_branch_id) LOOP
            SELECT * INTO v_mov
              FROM public.fn_mover_stock(d.product_id, -a.cantidad, p_user_id, a.stock_item_id, t.origin_branch_id);

            PERFORM public.fn_registrar_movimiento(
                d.product_id, a.stock_item_id, t.origin_branch_id, 'TRASPASO_SALIDA',
                -a.cantidad, v_mov.stock_before, v_mov.stock_after, t.id, p_user_id);

            INSERT INTO public.stock_transfer_lots
                (tenant_id, detail_id, origin_stock_item_id, lot_code, expiry_date, serial_number, quantity_sent)
            SELECT t.tenant_id, d.id, si.id, si.lot_code, si.expiry_date, si.serial_number, a.cantidad
              FROM public.stock_items si WHERE si.id = a.stock_item_id;

            -- Una unidad con serie deja de existir en el origen: la existencia
            -- nueva nace en el destino al recibirla (ver punto 2).
            UPDATE public.stock_items si
               SET state = false, modified_by = p_user_id, modified = now()
             WHERE si.id = a.stock_item_id AND si.serial_number IS NOT NULL;
        END LOOP;
    END LOOP;

    UPDATE public.stock_transfers
       SET status = 'enviado', sent_at = now(), sent_by = p_user_id,
           modified_by = p_user_id, modified = now()
     WHERE id = t.id;
END $$;

COMMENT ON FUNCTION public.fn_enviar_traspaso(uuid, integer) IS
    'Envía un traspaso en borrador desde su sucursal de origen: descuenta el stock '
    'por FEFO, registra las salidas y deja el traspaso en tránsito.';

-- -----------------------------------------------------------------------------
-- 5. Recibir
-- -----------------------------------------------------------------------------
-- p_recibido: {"<id de stock_transfer_lots>": cantidad, ...}. Lo que no figure
-- se toma como recibido completo, así que '{}' recibe todo lo enviado.
CREATE OR REPLACE FUNCTION public.fn_recibir_traspaso(p_transfer_id uuid, p_user_id integer, p_recibido jsonb DEFAULT '{}')
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    t        record;
    l        record;
    v_cant   numeric;
    v_item   uuid;
    v_mov    record;
BEGIN
    SELECT * INTO t FROM public.stock_transfers WHERE id = p_transfer_id AND state FOR UPDATE;

    IF t.id IS NULL THEN
        RAISE EXCEPTION 'No existe el traspaso.';
    END IF;
    IF t.status <> 'enviado' THEN
        RAISE EXCEPTION 'El traspaso % está %; solo se recibe uno enviado.', t.number, t.status;
    END IF;
    IF t.dest_branch_id IS DISTINCT FROM public.current_branch() THEN
        RAISE EXCEPTION 'El traspaso % se recibe en su sucursal de destino.', t.number;
    END IF;

    FOR l IN
        SELECT stl.*, sd.product_id, p.product_name, p.tracking_mode
          FROM public.stock_transfer_lots stl
          JOIN public.stock_transfer_detail sd ON sd.id = stl.detail_id
          JOIN public.products p ON p.id = sd.product_id
         WHERE sd.transfer_id = t.id
    LOOP
        v_cant := COALESCE((p_recibido ->> l.id::text)::numeric, l.quantity_sent);

        IF v_cant < 0 OR v_cant > l.quantity_sent THEN
            RAISE EXCEPTION 'La cantidad recibida de "%" debe estar entre 0 y %.', l.product_name, l.quantity_sent;
        END IF;
        IF l.serial_number IS NOT NULL AND v_cant NOT IN (0, 1) THEN
            RAISE EXCEPTION 'La serie % se recibe entera o no se recibe.', l.serial_number;
        END IF;

        v_item := NULL;

        IF v_cant > 0 THEN
            IF l.serial_number IS NOT NULL THEN
                SELECT * INTO v_mov FROM public.fn_recibir_serie(
                    l.product_id, l.serial_number, l.expiry_date, p_user_id, t.dest_branch_id);
            ELSIF l.lot_code IS NOT NULL THEN
                SELECT * INTO v_mov FROM public.fn_recibir_lote(
                    l.product_id, v_cant, l.lot_code, l.expiry_date, p_user_id, t.dest_branch_id);
            ELSE
                -- Sin lote: la existencia implícita del destino. Si el producto
                -- usa lotes, es stock heredado sin lote, y fn_existencia_implicita
                -- no la crea; se crea acá para no perderlo en el camino.
                v_item := public.fn_existencia_implicita(l.product_id, t.dest_branch_id, p_user_id);
                IF v_item IS NULL THEN
                    INSERT INTO public.stock_items (tenant_id, branch_id, product_id, quantity, created_by, modified_by)
                    VALUES (t.tenant_id, t.dest_branch_id, l.product_id, 0, p_user_id, p_user_id)
                    ON CONFLICT (tenant_id, branch_id, product_id, lot_code, expiry_date)
                        WHERE serial_number IS NULL DO NOTHING
                    RETURNING id INTO v_item;

                    IF v_item IS NULL THEN
                        SELECT si.id INTO v_item FROM public.stock_items si
                         WHERE si.product_id = l.product_id AND si.branch_id = t.dest_branch_id
                           AND si.lot_code IS NULL AND si.serial_number IS NULL;
                    END IF;
                END IF;

                SELECT * INTO v_mov FROM public.fn_mover_stock(
                    l.product_id, v_cant, p_user_id, v_item, t.dest_branch_id);
            END IF;

            v_item := v_mov.stock_item_id;

            PERFORM public.fn_registrar_movimiento(
                l.product_id, v_item, t.dest_branch_id, 'TRASPASO_ENTRADA',
                v_cant, v_mov.stock_before, v_mov.stock_after, t.id, p_user_id);
        END IF;

        -- Lo que no llegó es una pérdida del origen, que lo despachó. El stock ya
        -- había salido al enviar: el movimiento solo la deja registrada en las
        -- mermas, sin cambiar saldos.
        IF v_cant < l.quantity_sent THEN
            PERFORM public.fn_registrar_movimiento(
                l.product_id, l.origin_stock_item_id, t.origin_branch_id, 'MERMA',
                -(l.quantity_sent - v_cant),
                (SELECT quantity FROM public.stock_items WHERE id = l.origin_stock_item_id),
                (SELECT quantity FROM public.stock_items WHERE id = l.origin_stock_item_id),
                t.id, p_user_id, 'TRASPASO',
                format('No llegó a destino en el traspaso %s', t.number));
        END IF;

        UPDATE public.stock_transfer_lots
           SET quantity_received = v_cant, dest_stock_item_id = v_item
         WHERE id = l.id;
    END LOOP;

    UPDATE public.stock_transfers
       SET status = 'recibido', received_at = now(), received_by = p_user_id,
           modified_by = p_user_id, modified = now()
     WHERE id = t.id;
END $$;

COMMENT ON FUNCTION public.fn_recibir_traspaso(uuid, integer, jsonb) IS
    'Recibe un traspaso enviado en su sucursal de destino: ingresa cada lote con su '
    'código y vencimiento, y registra como merma del origen lo que no llegó.';

GRANT EXECUTE ON FUNCTION
    public.fn_registrar_movimiento(uuid, uuid, uuid, varchar, numeric, numeric, numeric, uuid, integer, varchar, varchar),
    public.fn_enviar_traspaso(uuid, integer),
    public.fn_recibir_traspaso(uuid, integer, jsonb)
TO app_pos;

-- La serie solo bloquea contra existencias activas, igual que el índice.
CREATE OR REPLACE FUNCTION public.fn_recibir_serie(
    p_product_id    uuid,
    p_serial_number varchar,
    p_expiry_date   date    DEFAULT NULL,
    p_user_id       integer DEFAULT 0,
    p_branch_id     uuid    DEFAULT NULL
)
RETURNS TABLE (stock_item_id uuid, stock_before numeric, stock_after numeric)
LANGUAGE plpgsql
AS $$
DECLARE
    v_branch uuid := public.fn_sucursal_requerida(p_branch_id);
    v_item   uuid;
    v_tenant integer;
    v_serie  varchar;
BEGIN
    v_serie := trim(p_serial_number);

    IF coalesce(v_serie, '') = '' THEN
        RAISE EXCEPTION 'El número de serie es obligatorio para un producto con seguimiento por series.';
    END IF;

    SELECT p.tenant_id INTO v_tenant FROM public.products p WHERE p.id = p_product_id;

    IF v_tenant IS NULL THEN
        RAISE EXCEPTION 'No existe el producto %.', p_product_id;
    END IF;

    -- En toda la farmacia, no solo en la sucursal: una serie es una unidad
    -- física. Una existencia inactiva es una serie que salió en un traspaso y
    -- ya no está en esa sucursal: no bloquea.
    SELECT si.id INTO v_item
      FROM public.stock_items si
     WHERE si.serial_number = v_serie AND si.state;

    IF v_item IS NOT NULL THEN
        RAISE EXCEPTION 'El número de serie % ya está registrado.', v_serie;
    END IF;

    INSERT INTO public.stock_items
        (tenant_id, branch_id, product_id, serial_number, expiry_date, quantity, created_by, modified_by)
    VALUES (v_tenant, v_branch, p_product_id, v_serie, p_expiry_date, 0, p_user_id, p_user_id)
    RETURNING id INTO v_item;

    RETURN QUERY SELECT * FROM public.fn_mover_stock(p_product_id, 1, p_user_id, v_item, v_branch);
END $$;

-- -----------------------------------------------------------------------------
-- 6. Menú: "Traspasos", junto al control de stock
-- -----------------------------------------------------------------------------
-- Va en el menú junto a inventory-stock, pero los permisos se copian de la
-- recepción de pedidos: traspasar es la misma clase de tarea (mover mercadería
-- entre lugares) y la hace el mismo personal. inventory-stock hoy es solo de
-- SuperAdmin.
DO $$
DECLARE
    v_form    integer;
    v_padre   integer;
    v_source  integer;
BEGIN
    SELECT f.form_id INTO v_padre
      FROM sec.forms f
     WHERE f.route = 'inventory-stock' AND f.state;

    SELECT f.id INTO v_source
      FROM sec.forms f
     WHERE f.route = 'purchases-receiving-admin' AND f.state;

    IF v_padre IS NULL OR v_source IS NULL THEN
        RAISE EXCEPTION 'No se encontraron "inventory-stock" y "purchases-receiving-admin"; el menú no tiene la forma esperada.';
    END IF;

    SELECT id INTO v_form FROM sec.forms WHERE route = 'stock-transfers';

    IF v_form IS NULL THEN
        v_form := set_sequences_key('sec.forms');

        INSERT INTO sec.forms
            (id, form_id, name_form, description, icon_css, show_order, route,
             show_menu, is_form_register, module_id, state,
             created_by, created, modified_by, modified, controller)
        SELECT v_form, v_padre, 'Traspasos entre Sucursales',
               'Envío y recepción de stock entre sucursales', 'fal fa-exchange', 5,
               'stock-transfers', true, true, f.module_id, true,
               1, now(), 1, now(), 'ninguno'
          FROM sec.forms f WHERE f.route = 'inventory-stock';
    END IF;

    INSERT INTO sec.roles_forms
        (rol_id, form_id, can_create, can_read, can_update, can_delete,
         state, created_by, created, modified_by, modified, tenant_id)
    SELECT rf.rol_id, v_form, rf.can_create, rf.can_read, rf.can_update, rf.can_delete,
           true, 1, now(), 1, now(), rf.tenant_id
      FROM sec.roles_forms rf
     WHERE rf.form_id = v_source AND rf.state AND rf.can_read
       AND NOT EXISTS (SELECT 1 FROM sec.roles_forms x
                        WHERE x.rol_id = rf.rol_id AND x.form_id = v_form);
END $$;

COMMIT;
