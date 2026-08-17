<template>
  <div v-if="visible" class="modal fade show d-block" tabindex="-1" @click.self="cerrar">
    <div class="modal-dialog modal-lg modal-dialog-scrollable modal-dialog-centered">
      <div class="modal-content">

        <div class="modal-header py-2">
          <div>
            <h6 class="modal-title mb-0">{{ producto?.ProductName }}</h6>
            <small class="text-muted">
              {{ producto?.LaboratoryName || 'Sin laboratorio' }}
              <span v-if="ficha.Presentation"> · {{ ficha.Presentation }}</span>
            </small>
          </div>
          <button type="button" class="btn-close" @click="cerrar"></button>
        </div>

        <div class="modal-body">

          <!-- Lo que cambia la venta va primero y no se puede pasar por alto. -->
          <div v-if="producto?.RequiresAuthorization" class="alert alert-warning py-2">
            <i class="fal fa-file-medical me-1"></i>
            <strong>Requiere respaldo para la venta</strong> — receta médica o autorización.
          </div>

          <div v-if="cargando" class="text-center py-4">
            <div class="spinner-border spinner-border-sm text-primary"></div>
          </div>

          <template v-else>
            <!-- Composición: lo primero que se pregunta en el mostrador. -->
            <div v-if="principiosActivos.length > 0" class="mb-3">
              <small class="text-muted d-block mb-1">Composición</small>
              <div class="d-flex flex-wrap gap-1">
                <span v-for="c in principiosActivos" :key="c.SubstanceId"
                  class="badge bg-primary-subtle text-primary-emphasis border border-primary-subtle">
                  {{ c.SubstanceName }}
                  <span v-if="c.ConcentrationValue"> {{ c.ConcentrationValue }} {{ c.ConcentrationUnit }}</span>
                </span>
              </div>
              <!--
                Los excipientes se separan porque responden otra pregunta:
                no "¿qué es?" sino "¿puedo tomarlo?" — gluten, lactosa, azúcar.
              -->
              <div v-if="excipientes.length > 0" class="mt-1">
                <small class="text-muted">
                  Excipientes: {{ excipientes.map(e => e.SubstanceName).join(', ') }}
                </small>
              </div>
            </div>

            <div v-if="ficha.FormName || ficha.RouteName || ficha.DosageReference" class="mb-3">
              <div class="row g-2">
                <div v-if="ficha.FormName" class="col-auto">
                  <small class="text-muted d-block">Forma</small>{{ ficha.FormName }}
                </div>
                <div v-if="ficha.RouteName" class="col-auto ms-3">
                  <small class="text-muted d-block">Vía</small>{{ ficha.RouteName }}
                </div>
              </div>
              <div v-if="ficha.DosageReference" class="mt-2">
                <small class="text-muted d-block">Posología de referencia</small>
                {{ ficha.DosageReference }}
                <small class="text-muted d-block">
                  Según prospecto. La dosis la indica quien receta.
                </small>
              </div>
            </div>

            <!--
              Alternativas. Se muestran SIEMPRE, no solo cuando falta stock: el
              motivo más común para ofrecer otra es el precio.

              Van en dos listas separadas a propósito. Las de arriba tienen la
              misma composición y son intercambiables de verdad; las de abajo las
              definió la farmacia y pueden tener otro principio activo. Quien
              vende necesita ver la diferencia: la decisión clínica es suya.
            -->
            <div v-if="automaticas.length > 0" class="mb-3">
              <small class="text-muted d-block mb-1">
                Misma composición ({{ automaticas.length }})
              </small>
              <div v-for="e in automaticas" :key="e.ProductId"
                class="d-flex align-items-center justify-content-between border rounded p-2 mb-1">
                <div class="me-2">
                  <div class="fw-semibold small">{{ e.ProductName }}</div>
                  <small class="text-muted">
                    <span v-if="e.ProductType">{{ etiquetaTipo(e.ProductType) }}</span>
                    <span v-if="e.Presentation"> · {{ e.Presentation }}</span>
                    · stock {{ e.CurrentStock }}
                  </small>
                </div>
                <div class="text-end text-nowrap">
                  <div class="fw-bold">Bs. {{ formatNum(e.SalePrice) }}</div>
                  <!-- La diferencia es el dato accionable, más que el precio. -->
                  <small v-if="diferencia(e) < 0" class="text-success">
                    {{ formatNum(Math.abs(diferencia(e))) }} menos
                  </small>
                  <small v-else-if="diferencia(e) > 0" class="text-muted">
                    {{ formatNum(diferencia(e)) }} más
                  </small>
                  <button type="button" class="btn btn-sm btn-outline-primary d-block mt-1"
                    :disabled="e.CurrentStock <= 0"
                    @click="$emit('agregar', e.ProductId)">
                    {{ e.CurrentStock > 0 ? 'Agregar' : 'Sin stock' }}
                  </button>
                </div>
              </div>
            </div>

            <div v-if="manuales.length > 0" class="mb-3">
              <small class="text-muted d-block mb-1">
                Otras opciones sugeridas ({{ manuales.length }})
              </small>
              <div v-for="e in manuales" :key="e.ProductId"
                class="d-flex align-items-center justify-content-between border rounded p-2 mb-1 border-warning-subtle">
                <div class="me-2">
                  <div class="fw-semibold small">{{ e.ProductName }}</div>
                  <small class="text-muted">
                    <span v-if="e.Reason">{{ e.Reason }} · </span>stock {{ e.CurrentStock }}
                  </small>
                  <!--
                    Aviso explícito: la composición puede ser distinta, y sin
                    esto un cajero podría entregarla como si fuera lo mismo.
                  -->
                  <small class="d-block text-warning-emphasis" style="font-size:.7rem">
                    Sugerencia de la farmacia, puede tener otra composición.
                  </small>
                </div>
                <div class="text-end text-nowrap">
                  <div class="fw-bold">Bs. {{ formatNum(e.SalePrice) }}</div>
                  <small v-if="diferencia(e) < 0" class="text-success">
                    {{ formatNum(Math.abs(diferencia(e))) }} menos
                  </small>
                  <button type="button" class="btn btn-sm btn-outline-primary d-block mt-1"
                    :disabled="e.CurrentStock <= 0"
                    @click="$emit('agregar', e.ProductId)">
                    {{ e.CurrentStock > 0 ? 'Agregar' : 'Sin stock' }}
                  </button>
                </div>
              </div>
            </div>

            <!-- Prospecto: se pide al abrir, no está en la lista de productos. -->
            <div v-if="prospecto">
              <small class="text-muted d-block mb-1">Prospecto</small>
              <div class="border rounded p-2 prospecto" v-html="prospectoHtml"></div>
              <small class="text-muted d-block mt-1">
                Información del prospecto del fabricante.
              </small>
            </div>

            <div v-if="!hayDatos" class="text-center text-muted py-3">
              <i class="fal fa-info-circle fa-2x d-block mb-2"></i>
              Este producto no tiene datos farmacéuticos cargados.
            </div>
          </template>

        </div>

        <div class="modal-footer py-2">
          <button type="button" class="btn btn-sm btn-secondary" @click="cerrar">Cerrar</button>
          <button v-if="producto && producto.CurrentStock > 0" type="button"
            class="btn btn-sm btn-primary" @click="$emit('agregar', producto.Id)">
            <i class="fal fa-plus me-1"></i>Agregar al carrito
          </button>
        </div>

      </div>
    </div>
  </div>
  <div v-if="visible" class="modal-backdrop fade show"></div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import usePharma from '@/modules/inventory/composables/usePharma';
import { ProductPharma, type ProductEquivalent } from '@/modules/inventory/models/pharma.model';
import type { Product } from '@/modules/inventory/models/product.model';
import { renderMarkdown } from '@/utils/markdown';

const props = defineProps<{ visible: boolean; producto: Product | null }>();
const emit = defineEmits<{ cerrar: []; agregar: [productId: string] }>();

const { getByProduct, getLeaflet, getEquivalents } = usePharma();

const ficha = ref(new ProductPharma());
const equivalentes = ref<ProductEquivalent[]>([]);
const prospecto = ref('');
const cargando = ref(false);

const automaticas = computed(() => equivalentes.value.filter(e => !e.IsManual));
const manuales = computed(() => equivalentes.value.filter(e => e.IsManual));

const principiosActivos = computed(() => ficha.value.Components.filter(c => c.IsActiveIngredient));
const excipientes = computed(() => ficha.value.Components.filter(c => !c.IsActiveIngredient));

const hayDatos = computed(() =>
  ficha.value.Components.length > 0 || equivalentes.value.length > 0 ||
  !!prospecto.value || !!ficha.value.FormName || !!ficha.value.DosageReference
);

const prospectoHtml = computed(() => renderMarkdown(prospecto.value));

const formatNum = (v: number) =>
  (v ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const etiquetaTipo = (t: string) =>
  t === 'generico' ? 'Genérico' : t === 'marca' ? 'Marca' : t === 'similar' ? 'Similar' : t;

/** Negativo = la alternativa es más barata. */
const diferencia = (e: ProductEquivalent) => e.SalePrice - (props.producto?.SalePrice ?? 0);

/**
 * Todo se pide al abrir y no con la lista de productos: son tres consultas por
 * producto, y la grilla del punto de venta muestra cientos. Traerlas por
 * adelantado haría lenta la pantalla que más se usa para no ahorrar nada.
 */
watch(() => [props.visible, props.producto?.Id], async ([visible]) => {
  if (!visible || !props.producto) return;

  cargando.value = true;
  ficha.value = new ProductPharma();
  equivalentes.value = [];
  prospecto.value = '';

  try {
    const id = props.producto.Id;
    const [f, e, p] = await Promise.all([getByProduct(id), getEquivalents(id), getLeaflet(id)]);
    if (f.ok && f.Data) ficha.value = Object.assign(new ProductPharma(), f.Data);
    if (e.ok) equivalentes.value = e.Data;
    if (p.ok) prospecto.value = p.Data ?? '';
  } finally {
    cargando.value = false;
  }
});

const cerrar = () => emit('cerrar');
</script>

<style scoped>
.modal { background: rgba(0, 0, 0, .5); }
.prospecto :deep(h1),
.prospecto :deep(h2),
.prospecto :deep(h3) { font-size: .95rem; font-weight: 600; margin-top: .6rem; }
.prospecto :deep(p) { margin-bottom: .4rem; font-size: .875rem; }
.prospecto :deep(ul) { margin-bottom: .4rem; padding-left: 1.2rem; font-size: .875rem; }
</style>
