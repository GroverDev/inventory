<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventarios</li>
        <li class="breadcrumb-item active" aria-current="page">Trazabilidad de Lote</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Trazabilidad de <span class="fw-300"><i>LOTE</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <p class="text-muted small mb-3">
              Ante un retiro del laboratorio o un reclamo de garantía: buscá el lote
              o el número de serie y obtené a qué clientes se les vendió, con sus
              datos de contacto.
            </p>

            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-6 col-lg-4">
                <label class="form-label">Código de lote o número de serie</label>
                <div class="input-group body-bg shadow-inset-2 rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="sa sa-magnifier text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="lote"
                    placeholder="Ej: IBU-2609-A o SN-00123"
                    autocomplete="off"
                    @keyup.enter="buscar"
                  />
                  <button class="btn btn-primary" type="button" :disabled="!lote" @click="buscar">
                    Buscar
                  </button>
                </div>
              </div>
              <div class="col-12 col-md-auto ms-md-auto" v-if="ventas.length > 0">
                <button type="button" class="btn btn-sm btn-outline-success w-100" @click="exportar">
                  <span class="fal fa-file-excel me-1"></span>Exportar
                </button>
              </div>
            </div>

            <div v-if="loading" class="text-center py-5">
              <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Cargando...</span>
              </div>
            </div>

            <!-- Sin buscar todavía: no es un resultado vacío, es que no se preguntó nada. -->
            <div v-else-if="!buscado" class="text-center py-5">
              <i class="fal fa-barcode-read fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted">Ingresá un código de lote para rastrearlo.</p>
            </div>

            <div v-else-if="ventas.length === 0" class="text-center py-5">
              <i class="fal fa-box-open fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-1">
                No hay ventas registradas de <strong>{{ loteBuscado }}</strong>.
              </p>
              <small class="text-muted">
                Si existe, sigue en el estante sin vender. Verificá el código si
                esperabas encontrar ventas.
              </small>
            </div>

            <template v-else>
              <div class="alert alert-warning py-2">
                <i class="fal fa-exclamation-triangle me-1"></i>
                <strong>{{ unidades }}</strong> unidad(es) de
                <strong>{{ ventas[0].ProductName }}</strong>,
                {{ ventas[0].LotCode ? 'lote' : 'serie' }}
                <strong>{{ ventas[0].LotCode || ventas[0].SerialNumber }}</strong>, en
                <strong>{{ ventas.length }}</strong> venta(s)
                <span v-if="ventas[0].ExpiryDate"> · vence {{ formatDate(ventas[0].ExpiryDate) }}</span>
              </div>

              <div class="table-responsive">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead>
                    <tr>
                      <th>Fecha</th>
                      <th>Cliente</th>
                      <th>Documento</th>
                      <th>Teléfono</th>
                      <th class="text-center">Cantidad</th>
                      <th class="text-center">Venta</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="v in ventas" :key="v.SaleId + v.SaleDate">
                      <td>{{ formatDate(v.SaleDate) }}</td>
                      <td class="fw-semibold">{{ v.Cliente }}</td>
                      <td><small class="text-muted font-monospace">{{ v.DocumentNumber || '—' }}</small></td>
                      <td>
                        <a v-if="v.Cellphone" :href="`tel:${v.Cellphone}`" class="text-decoration-none">
                          {{ v.Cellphone }}
                        </a>
                        <span v-else class="text-muted">—</span>
                      </td>
                      <td class="text-center">{{ v.Quantity }}</td>
                      <td class="text-center">
                        <button type="button" class="btn btn-outline-info btn-sm"
                          title="Ver la venta" @click="verVenta(v)">
                          <span class="fal fa-receipt"></span>
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </template>

          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import useStockMovement from '@/modules/inventory/composables/useStockMovement';
import type { LotTraceabilityResponse } from '@/modules/inventory/models/stockMovement.model';
import { exportToExcel } from '@/utils/excelHelper';
import { todayIso } from '@/utils/dateHelper';

const { getTraceability } = useStockMovement();
const route = useRoute();
const router = useRouter();

const lote = ref('');
const loteBuscado = ref('');
const ventas = ref<LotTraceabilityResponse[]>([]);
const loading = ref(false);
/** Distingue "todavía no buscaste" de "buscaste y no hay nada". */
const buscado = ref(false);

const formatDate = (val: string): string => {
  if (!val) return '—';
  return new Date(val).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const unidades = computed(() => ventas.value.reduce((acc, v) => acc + v.Quantity, 0));

// Se puede llegar con el lote ya elegido desde la pantalla de vencimientos.
onMounted(() => {
  const inicial = route.query.lote as string | undefined;
  if (inicial) {
    lote.value = inicial;
    buscar();
  }
});

async function buscar() {
  if (!lote.value) return;
  loading.value = true;
  try {
    const { ok, Data } = await getTraceability(lote.value);
    ventas.value = ok ? Data : [];
    loteBuscado.value = lote.value;
    buscado.value = true;
  } finally {
    loading.value = false;
  }
}

const verVenta = (v: LotTraceabilityResponse) =>
  router.push({ name: 'sale-detail', params: { id: v.SaleId } });

/** El listado se exporta para llamar a los clientes uno por uno. */
const exportar = () => {
  const filas = ventas.value.map(v => ({
    Fecha: formatDate(v.SaleDate),
    Cliente: v.Cliente,
    Documento: v.DocumentNumber ?? '',
    Teléfono: v.Cellphone ?? '',
    Cantidad: v.Quantity,
    Producto: v.ProductName,
    Lote: v.LotCode,
    Serie: v.SerialNumber,
  }));
  exportToExcel(filas, `trazabilidad_${loteBuscado.value}_${todayIso()}.xlsx`);
};
</script>

<style scoped></style>
