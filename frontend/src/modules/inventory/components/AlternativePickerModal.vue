<template>
  <!--
    Al body: dentro del layout de inventario hay un ancestro que crea contexto de
    posicionamiento, y el modal salía corrido y cortado contra la barra lateral
    en vez de centrado sobre la pantalla.
  -->
  <Teleport to="body">
  <!--
    z-index por encima de 2500, que es el de la barra lateral de la plantilla:
    con el 1055 que trae Bootstrap, el menú se pintaba ENCIMA del modal y tapaba
    su mitad izquierda.
  -->
  <div v-if="visible" class="modal d-block" tabindex="-1"
    style="background:rgba(0,0,0,.5); z-index:2600">
    <div class="modal-dialog modal-dialog-centered modal-lg">
      <div class="modal-content">

        <div class="modal-header py-2">
          <h6 class="modal-title fw-bold">
            <i class="fal fa-exchange me-2"></i>Sugerir una alternativa
          </h6>
          <button type="button" class="btn-close" @click="cerrar"></button>
        </div>

        <div class="modal-body">
          <p class="small text-muted mb-3">
            Lo que elijas acá se le va a ofrecer al vendedor cuando alguien pida
            <strong>{{ productName }}</strong>.
          </p>

          <!-- Paso 1: elegir el producto -->
          <div v-if="!elegido">
            <div class="input-group mb-3">
              <span class="input-group-text bg-transparent">
                <i class="sa sa-magnifier text-success"></i>
              </span>
              <input
                ref="buscador"
                type="text"
                class="form-control"
                placeholder="Buscar por nombre..."
                v-model.trim="busqueda"
                @keyup.enter="buscar"
              />
              <button class="btn btn-primary" type="button" :disabled="busqueda.length < 3" @click="buscar">
                Buscar
              </button>
            </div>

            <div v-if="buscando" class="text-center py-4">
              <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Buscando...</span>
              </div>
            </div>

            <!-- Sin buscar todavía no es lo mismo que sin resultados. -->
            <div v-else-if="!buscado" class="text-center py-4 text-muted">
              <i class="fal fa-search fa-2x d-block mb-2"></i>
              <p class="mb-0 small">Escribí al menos 3 letras del nombre y buscá.</p>
            </div>

            <div v-else-if="resultados.length === 0" class="text-center py-4 text-muted">
              <i class="fal fa-box-open fa-2x d-block mb-2"></i>
              <p class="mb-1 small">Ningún producto coincide con «{{ busqueda }}».</p>
              <small v-if="ocultos > 0">
                {{ ocultos }} quedaron fuera por estar ya sugeridos o deducirse solos.
              </small>
            </div>

            <template v-else>
              <div class="table-responsive" style="max-height:340px; overflow-y:auto">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead>
                    <tr>
                      <th>Producto</th>
                      <th class="text-end">Precio</th>
                      <th class="text-end">Diferencia</th>
                      <th class="text-center">Stock</th>
                      <th></th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="p in resultados" :key="p.Id" :class="{ 'opacity-50': p.CurrentStock <= 0 }">
                      <td>
                        <span class="fw-semibold">{{ p.ProductName }}</span>
                        <small class="d-block text-muted">{{ p.LaboratoryName || '—' }}</small>
                      </td>
                      <td class="text-end">{{ formatCurrency(p.SalePrice) }}</td>
                      <td class="text-end">
                        <!--
                          La diferencia contra el producto que se está editando:
                          si el motivo más común es "más económico", el dato tiene
                          que estar a la vista al elegir, no después.
                        -->
                        <span :class="claseDiferencia(p.SalePrice)">{{ diferencia(p.SalePrice) }}</span>
                      </td>
                      <td class="text-center">
                        <span class="badge" :class="p.CurrentStock > 0 ? 'bg-success-subtle text-success-emphasis border border-success-subtle' : 'bg-secondary-subtle text-secondary-emphasis border border-secondary-subtle'">
                          {{ p.CurrentStock }}
                        </span>
                      </td>
                      <td class="text-end">
                        <!--
                          Sin stock no se puede elegir: una sugerencia que no se
                          puede entregar le hace perder la venta a quien atiende.
                          Se muestra igual, para que se vea que existe.
                        -->
                        <button
                          type="button"
                          class="btn btn-sm"
                          :class="p.CurrentStock > 0 ? 'btn-outline-primary' : 'btn-outline-secondary'"
                          :disabled="p.CurrentStock <= 0"
                          :title="p.CurrentStock > 0 ? '' : 'Sin stock: no se puede sugerir'"
                          @click="elegir(p)"
                        >
                          {{ p.CurrentStock > 0 ? 'Elegir' : 'Sin stock' }}
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
              <small v-if="ocultos > 0" class="text-muted d-block mt-2">
                <i class="fal fa-info-circle me-1"></i>
                {{ ocultos }} resultado(s) ocultos: ya están sugeridos o el sistema
                los deduce solo por composición.
              </small>
            </template>
          </div>

          <!-- Paso 2: por qué se sugiere -->
          <div v-else>
            <div class="d-flex justify-content-between align-items-start border rounded p-2 mb-3">
              <div>
                <span class="fw-semibold">{{ elegido.ProductName }}</span>
                <small class="d-block text-muted">
                  {{ formatCurrency(elegido.SalePrice) }}
                  · <span :class="claseDiferencia(elegido.SalePrice)">{{ diferencia(elegido.SalePrice) }}</span>
                </small>
              </div>
              <button type="button" class="btn btn-sm btn-outline-secondary" @click="elegido = null">
                Cambiar
              </button>
            </div>

            <label class="form-label">¿Por qué se sugiere?</label>
            <!--
              Los motivos frecuentes van como botones porque es el texto que el
              vendedor lee en el mostrador: escrito libre por cada persona, la
              misma idea termina redactada de cinco formas distintas.
            -->
            <div class="d-flex flex-wrap gap-2 mb-2">
              <button
                v-for="m in motivosFrecuentes"
                :key="m"
                type="button"
                class="btn btn-sm"
                :class="motivo === m ? 'btn-primary' : 'btn-outline-secondary'"
                @click="motivo = m"
              >
                {{ m }}
              </button>
            </div>
            <input
              type="text"
              class="form-control"
              maxlength="150"
              placeholder="O escribí otro motivo..."
              v-model.trim="motivo"
            />
          </div>
        </div>

        <div class="modal-footer py-2">
          <button class="btn btn-outline-secondary btn-sm" @click="cerrar">Cancelar</button>
          <button class="btn btn-primary btn-sm" :disabled="!elegido || guardando" @click="confirmar">
            <span v-if="guardando" class="spinner-border spinner-border-sm me-1"></span>
            Agregar sugerencia
          </button>
        </div>

      </div>
    </div>
  </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import useProduct from '@/modules/inventory/composables/useProduct';
import type { Product } from '@/modules/inventory/models/product.model';

const props = defineProps<{
  visible: boolean;
  /** Producto que se está editando: no puede sugerirse a sí mismo. */
  productId: string;
  productName: string;
  productPrice: number;
  /** Ya sugeridos o deducidos por composición: se ocultan de los resultados. */
  excludeIds: string[];
  /** Precarga del atajo desde los equivalentes automáticos. */
  preselected?: Product | null;
  preselectedReason?: string;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'confirm', payload: { productId: string; reason: string }): void;
}>();

const { getProductsByName } = useProduct();

const motivosFrecuentes = [
  'Más económico',
  'Misma composición',
  'Cuando no hay stock',
  'El cliente lo prefiere',
];

const busqueda = ref('');
const resultados = ref<Product[]>([]);
const ocultos = ref(0);
const buscando = ref(false);
const buscado = ref(false);
const elegido = ref<Product | null>(null);
const motivo = ref('');
const guardando = ref(false);
const buscador = ref<HTMLInputElement | null>(null);

const formatCurrency = (val: number): string =>
  `Bs. ${(val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

/** Contra el producto que se está editando, que es la comparación que importa. */
const diferencia = (precio: number): string => {
  const d = +(precio - props.productPrice).toFixed(2);
  if (d === 0) return 'igual precio';
  return `${d > 0 ? '+' : '−'} ${formatCurrency(Math.abs(d))}`;
};

const claseDiferencia = (precio: number): string => {
  const d = +(precio - props.productPrice).toFixed(2);
  if (d < 0) return 'text-success fw-semibold';
  if (d > 0) return 'text-danger';
  return 'text-muted';
};

const reiniciar = () => {
  busqueda.value = '';
  resultados.value = [];
  ocultos.value = 0;
  buscado.value = false;
  elegido.value = props.preselected ?? null;
  motivo.value = props.preselectedReason ?? '';
};

watch(() => props.visible, async (abierto) => {
  if (!abierto) return;
  reiniciar();
  await nextTick();
  buscador.value?.focus();
});

const buscar = async () => {
  if (busqueda.value.length < 3) return;
  buscando.value = true;
  try {
    const { ok, Data } = await getProductsByName(busqueda.value);
    const todos = ok ? Data : [];
    // Se excluye el producto mismo, lo ya sugerido y lo que el sistema deduce:
    // ofrecerlos sería invitar a duplicar algo que ya está resuelto.
    const visibles = todos.filter(
      (p: Product) => p.Id !== props.productId && !props.excludeIds.includes(p.Id)
    );
    ocultos.value = todos.length - visibles.length;
    resultados.value = visibles;
    buscado.value = true;
  } finally {
    buscando.value = false;
  }
};

const elegir = (p: Product) => {
  if (p.CurrentStock <= 0) return;
  elegido.value = p;
  if (!motivo.value) motivo.value = p.SalePrice < props.productPrice ? 'Más económico' : '';
};

const cerrar = () => emit('close');

const confirmar = () => {
  if (!elegido.value) return;
  guardando.value = true;
  emit('confirm', { productId: elegido.value.Id, reason: motivo.value });
  guardando.value = false;
};
</script>

<style scoped></style>
