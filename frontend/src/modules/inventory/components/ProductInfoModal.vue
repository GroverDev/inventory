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

          <!--
            Precio y stock de este momento: la grilla del POS los trae de cuando se
            abrió la pantalla y pudieron cambiar (otra caja vendió, se corrigió el
            precio). Arranca con los de la tarjeta y se reemplaza al llegar los
            actuales.
          -->
          <div class="row g-2 mb-3">
            <!-- Precio: recuadro neutro con el número en el color de la marca. -->
            <div class="col-6">
              <div class="border rounded-3 px-3 py-2 h-100">
                <small class="text-muted d-block">Precio</small>
                <span class="fw-bold fs-4 text-primary text-nowrap">Bs. {{ formatNum(precio) }}</span>
              </div>
            </div>
            <!--
              Stock: recuadro con fondo propio según la cantidad, y el número con
              su unidad. Distinto en forma y color del precio para que no se lean
              como un solo dato.
            -->
            <div class="col-6">
              <div class="rounded-3 px-3 py-2 h-100" :class="stockClase">
                <small class="d-block opacity-75">Stock disponible</small>
                <span class="fw-bold fs-4 text-nowrap">{{ stock > 0 ? stock : 'Agotado' }}</span>
                <small v-if="stock > 0 && producto?.UnitName" class="ms-1 opacity-75">{{ producto.UnitName }}</small>
              </div>
            </div>
            <div v-if="actualizando" class="col-12">
              <small class="text-muted">
                <span class="spinner-border spinner-border-sm me-1"></span>Actualizando…
              </small>
            </div>
          </div>

          <!-- Lo que cambia la venta va primero y no se puede pasar por alto. -->
          <div v-if="producto?.RequiresAuthorization" class="alert alert-warning py-2">
            <i class="fal fa-file-medical me-1"></i>
            <strong>Requiere respaldo para la venta</strong> — receta médica o autorización.
          </div>

          <!--
            Foto entera del producto (800 px, sin recortar): confirmar el empaque
            es para lo que se abre esta ficha. Flota a la derecha en pantallas
            anchas para que el texto la rodee y no empuje la composición hacia
            abajo; en móvil va arriba, centrada.
          -->
          <div v-if="imagenUrl && !imagenFallo" class="ficha-imagen text-center float-md-end ms-md-3 mb-3">
            <img :src="imagenUrl" :alt="producto?.ProductName" class="rounded border" role="button"
              title="Clic para ampliar" @click="ampliada = true" @error="imagenFallo = true" />
            <small class="text-muted d-block"><i class="fal fa-search-plus me-1"></i>Clic para ampliar</small>
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
          <button v-if="producto && stock > 0" type="button"
            class="btn btn-sm btn-primary" @click="$emit('agregar', producto.Id)">
            <i class="fal fa-plus me-1"></i>Agregar al carrito
          </button>
        </div>

      </div>
    </div>
  </div>
  <div v-if="visible" class="modal-backdrop fade show"></div>

  <ImageLightbox
    :open="ampliada"
    :src="imagenUrl"
    :placeholder="mediaUrl(producto?.ImagePath, true)"
    :caption="producto?.ProductName"
    @close="ampliada = false"
  />
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import usePharma from '@/modules/inventory/composables/usePharma';
import useProduct from '@/modules/inventory/composables/useProduct';
import { ProductPharma, type ProductEquivalent } from '@/modules/inventory/models/pharma.model';
import type { Product } from '@/modules/inventory/models/product.model';
import { renderMarkdown } from '@/utils/markdown';
import { mediaUrl } from '@/utils/mediaUrl';
import ImageLightbox from '@/modules/inventory/components/ImageLightbox.vue';

const props = defineProps<{ visible: boolean; producto: Product | null }>();
const emit = defineEmits<{
  cerrar: [];
  agregar: [productId: string];
  /** Llegaron el precio y el stock actuales: quien tiene la lista puede ponerse al día. */
  actualizado: [productId: string, datos: { SalePrice: number; CurrentStock: number }];
}>();

const { getByProduct, getLeaflet, getEquivalents } = usePharma();
const { validateProductSelection } = useProduct();

const ficha = ref(new ProductPharma());
const equivalentes = ref<ProductEquivalent[]>([]);
const prospecto = ref('');
const cargando = ref(false);

const actual = ref<{ SalePrice: number; CurrentStock: number } | null>(null);
const actualizando = ref(false);
const precio = computed(() => actual.value?.SalePrice ?? props.producto?.SalePrice ?? 0);
const stock = computed(() => actual.value?.CurrentStock ?? props.producto?.CurrentStock ?? 0);
const stockClase = computed(() =>
  stock.value > 5 ? 'bg-success-subtle text-success-emphasis'
  : stock.value > 0 ? 'bg-warning-subtle text-warning-emphasis'
  : 'bg-danger-subtle text-danger-emphasis');

// Si el archivo no está (404) se omite el bloque en vez de mostrar un ícono roto.
const imagenFallo = ref(false);
const ampliada = ref(false);
const imagenUrl = computed(() => mediaUrl(props.producto?.ImagePath));
watch(() => props.producto?.ImagePath, () => { imagenFallo.value = false; });
watch(() => props.visible, (v) => { if (!v) ampliada.value = false; });

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
const diferencia = (e: ProductEquivalent) => e.SalePrice - precio.value;

/**
 * Todo se pide al abrir y no con la lista de productos: son tres consultas por
 * producto, y la grilla del punto de venta muestra cientos. Traerlas por
 * adelantado haría lenta la pantalla que más se usa para no ahorrar nada.
 */
watch(() => [props.visible, props.producto?.Id], async ([visible]) => {
  if (!visible || !props.producto) return;

  cargando.value = true;
  actual.value = null;
  ficha.value = new ProductPharma();
  equivalentes.value = [];
  prospecto.value = '';

  try {
    const id = props.producto.Id;
    actualizando.value = true;
    // Aparte de las otras: es lo que se ve primero y no debe esperar a la ficha.
    validateProductSelection(id)
      .then(r => { if (r.ok && r.Data && props.producto?.Id === id) {
          actual.value = r.Data;
          emit('actualizado', id, { SalePrice: r.Data.SalePrice, CurrentStock: r.Data.CurrentStock });
        }
      })
      .finally(() => { actualizando.value = false; });

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
/* contain: se ve la foto entera, sin recortar los bordes del empaque. */
.ficha-imagen img { max-width: 100%; max-height: 240px; object-fit: contain; background: var(--bs-body-bg); }
@media (min-width: 768px) { .ficha-imagen { width: 220px; } }
.prospecto :deep(h1),
.prospecto :deep(h2),
.prospecto :deep(h3) { font-size: .95rem; font-weight: 600; margin-top: .6rem; }
.prospecto :deep(p) { margin-bottom: .4rem; font-size: .875rem; }
.prospecto :deep(ul) { margin-bottom: .4rem; padding-left: 1.2rem; font-size: .875rem; }
</style>
