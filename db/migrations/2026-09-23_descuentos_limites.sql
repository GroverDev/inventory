-- =============================================================================
-- Topes del descuento manual, configurables por empresa y por sucursal
-- =============================================================================
-- Hasta acá los topes salían de appsettings (PosSettings): iguales para todas
-- las farmacias y todas las sucursales, y cambiarlos exigía reiniciar el
-- backend. Ahora se cargan desde la app.
--
-- Dos topes, cada uno como porcentaje y como monto fijo:
--   * cashier_*  Tope del cajero. Por encima, el POS pide autorización de un
--                supervisor. Sin valor, rige el de appsettings (15 % / Bs. 50).
--   * general_*  Tope máximo para cualquier rol. Nadie lo supera, ni con
--                autorización. Sin valor, no hay tope (como hasta ahora).
--
-- Una fila con branch_id nulo es la de la empresa; una con sucursal, su
-- excepción. Cada valor se resuelve por separado: el de la sucursal si lo
-- tiene, si no el de la empresa, si no el default.
--
-- Solo afecta a descuentos manuales: los del catálogo ya los aprobó quien los
-- dio de alta.
--
-- Idempotente.
-- =============================================================================

BEGIN;

CREATE TABLE IF NOT EXISTS public.discount_limits (
    id                 uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id          integer       NOT NULL DEFAULT public.current_tenant() REFERENCES sec.tenants(id),
    branch_id          uuid,
    cashier_max_pct    numeric(5,2)  CHECK (cashier_max_pct BETWEEN 0 AND 100),
    cashier_max_amount numeric(10,2) CHECK (cashier_max_amount >= 0),
    general_max_pct    numeric(5,2)  CHECK (general_max_pct BETWEEN 0 AND 100),
    general_max_amount numeric(10,2) CHECK (general_max_amount >= 0),
    created_by         integer       NOT NULL DEFAULT 0,
    created            timestamptz   NOT NULL DEFAULT now(),
    modified_by        integer       NOT NULL DEFAULT 0,
    modified           timestamptz   NOT NULL DEFAULT now(),
    CONSTRAINT discount_limits_sucursal_fk
        FOREIGN KEY (tenant_id, branch_id) REFERENCES public.branches (tenant_id, id),
    -- El tope del cajero por encima del máximo no tendría sentido: el supervisor
    -- no podría autorizar nada entre uno y otro.
    CONSTRAINT discount_limits_pct_coherente
        CHECK (cashier_max_pct IS NULL OR general_max_pct IS NULL OR cashier_max_pct <= general_max_pct),
    CONSTRAINT discount_limits_monto_coherente
        CHECK (cashier_max_amount IS NULL OR general_max_amount IS NULL OR cashier_max_amount <= general_max_amount)
);

COMMENT ON TABLE public.discount_limits IS
    'Topes del descuento manual. branch_id nulo = empresa; con sucursal = su excepción. '
    'Cada valor nulo hereda del nivel superior.';

-- Una fila de empresa por tenant y una por sucursal.
CREATE UNIQUE INDEX IF NOT EXISTS discount_limits_empresa_uk
    ON public.discount_limits (tenant_id) WHERE branch_id IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS discount_limits_sucursal_uk
    ON public.discount_limits (branch_id) WHERE branch_id IS NOT NULL;

GRANT SELECT, INSERT, UPDATE, DELETE ON public.discount_limits TO app_pos;

ALTER TABLE public.discount_limits ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.discount_limits FORCE  ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_aislado ON public.discount_limits;
CREATE POLICY tenant_aislado ON public.discount_limits
    USING      (tenant_id = public.current_tenant())
    WITH CHECK (tenant_id = public.current_tenant());

-- -----------------------------------------------------------------------------
-- Menú: "Límites de Descuento", junto al catálogo de descuentos
-- -----------------------------------------------------------------------------
-- Cada rol recibe los permisos que ya tiene sobre discounts-admin: quien
-- administra los descuentos del catálogo administra también sus topes.
DO $$
DECLARE
    v_form   integer;
    v_padre  integer;
    v_source integer;
BEGIN
    SELECT f.form_id, f.id INTO v_padre, v_source
      FROM sec.forms f WHERE f.route = 'discounts-admin' AND f.state;

    IF v_padre IS NULL THEN
        RAISE EXCEPTION 'No se encontró el formulario "discounts-admin"; el menú no tiene la forma esperada.';
    END IF;

    SELECT id INTO v_form FROM sec.forms WHERE route = 'discount-limits';

    IF v_form IS NULL THEN
        v_form := set_sequences_key('sec.forms');

        INSERT INTO sec.forms
            (id, form_id, name_form, description, icon_css, show_order, route,
             show_menu, is_form_register, module_id, state,
             created_by, created, modified_by, modified, controller)
        SELECT v_form, v_padre, 'Límites de Descuento',
               'Topes del descuento manual por empresa y sucursal', 'fal fa-percent', 4,
               'discount-limits', true, true, f.module_id, true,
               1, now(), 1, now(), 'ninguno'
          FROM sec.forms f WHERE f.id = v_source;
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
