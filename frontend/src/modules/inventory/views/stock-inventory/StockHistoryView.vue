<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventarios</li>
        <li class="breadcrumb-item">
          <a href="#" @click.prevent="router.back()">Control de Stock</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">Historial de Movimientos</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Historial de <span class="fw-300"><i>MOVIMIENTOS</i></span></h2>
          <div class="panel-toolbar">
            <button type="button" class="btn btn-sm btn-outline-secondary" @click="router.back()">
              <span class="fal fa-arrow-left me-1"></span>Volver
            </button>
          </div>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <!-- Info del producto -->
            <div class="alert alert-light border mb-3 py-2">
              <div class="d-flex align-items-center gap-3">
                <i class="fal fa-box fa-2x text-primary"></i>
                <div>
                  <div class="fw-semibold">{{ productName }}</div>
                  <small class="text-muted font-monospace">{{ productCode }}</small>
                </div>
              </div>
            </div>

            <!-- Cargando -->
            <div v-if="loading" class="text-center py-5">
              <div class="spinner-border text-primary" role="status"></div>
              <p class="text-muted mt-2">Cargando movimientos...</p>
            </div>

            <!-- Sin movimientos -->
            <div v-else-if="movements.length === 0" class="text-center py-5">
              <i class="fal fa-history fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted">No se encontraron movimientos para este producto.</p>
            </div>

            <!-- Tabla desktop -->
            <template v-else>
              <div class="mb-2">
                <small class="text-muted">
                  <span class="fal fa-list me-1"></span>
                  <strong>{{ movements.length }}</strong> movimiento(s)
                </small>
              </div>

              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="">
                    <tr>
                      <th>Fecha</th>
                      <th class="text-center">Tipo</th>
                      <th class="text-center">Cantidad</th>
                      <th class="text-center d-none d-lg-table-cell">Antes</th>
                      <th class="text-center d-none d-lg-table-cell">Después</th>
                      <th class="d-none d-xl-table-cell">Motivo</th>
                      <th class="d-none d-xl-table-cell">Observación</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="mov in movements" :key="mov.Id">
                      <td>
                        <small>{{ formatDate(mov.Created) }}</small>
                      </td>
                      <td class="text-center">
                        <span class="badge" :class="typeBadge(mov.MovementType)">
                          {{ typeLabel(mov.MovementType) }}
                        </span>
                      </td>
                      <td class="text-center fw-semibold" :class="mov.Quantity >= 0 ? 'text-success' : 'text-danger'">
                        {{ mov.Quantity >= 0 ? '+' : '' }}{{ mov.Quantity }}
                      </td>
                      <td class="text-center d-none d-lg-table-cell">
                        <small class="text-muted">{{ mov.StockBefore }}</small>
                      </td>
                      <td class="text-center d-none d-lg-table-cell">
                        <small class="text-muted">{{ mov.StockAfter }}</small>
                      </td>
                      <td class="d-none d-xl-table-cell">
                        <small>{{ mov.Reason ?? '—' }}</small>
                      </td>
                      <td class="d-none d-xl-table-cell">
                        <small class="text-muted">{{ mov.Observation ?? '—' }}</small>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- Cards móvil -->
              <div class="d-md-none">
                <div class="row g-2">
                  <div class="col-12" v-for="mov in movements" :key="mov.Id">
                    <div class="card border-start border-3" :class="movCardBorder(mov.MovementType)">
                      <div class="card-body py-2 px-3">
                        <div class="d-flex justify-content-between align-items-start mb-1">
                          <small class="text-muted">{{ formatDate(mov.Created) }}</small>
                          <span class="badge" :class="typeBadge(mov.MovementType)">{{ typeLabel(mov.MovementType) }}</span>
                        </div>
                        <div class="d-flex justify-content-between align-items-center">
                          <div>
                            <div class="fw-semibold" :class="mov.Quantity >= 0 ? 'text-success' : 'text-danger'">
                              {{ mov.Quantity >= 0 ? '+' : '' }}{{ mov.Quantity }} unidades
                            </div>
                            <small class="text-muted">{{ mov.StockBefore }} → {{ mov.StockAfter }}</small>
                          </div>
                          <div class="text-end">
                            <small v-if="mov.Reason" class="text-muted d-block">{{ mov.Reason }}</small>
                            <small v-if="mov.Observation" class="text-muted">{{ mov.Observation }}</small>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </template>

          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import useStockMovement from '@/modules/inventory/composables/useStockMovement';
import type { StockMovementResponse } from '@/modules/inventory/models/stockMovement.model';

const route = useRoute();
const router = useRouter();
const { getMovementsByProduct } = useStockMovement();

const movements = ref<StockMovementResponse[]>([]);
const loading = ref(false);

const productId = route.params.id as string;
const productName = route.query.name as string ?? '';
const productCode = route.query.code as string ?? '';

const formatDate = (dateStr: string): string => {
  const d = new Date(dateStr);
  return d.toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' })
    + ' ' + d.toLocaleTimeString('es-BO', { hour: '2-digit', minute: '2-digit' });
};

const typeLabel = (type: string): string => {
  switch (type) {
    case 'VENTA': return 'Venta';
    case 'COMPRA': return 'Compra';
    case 'AJUSTE': return 'Ajuste';
    default: return type;
  }
};

const typeBadge = (type: string): string => {
  switch (type) {
    case 'VENTA': return 'bg-danger';
    case 'COMPRA': return 'bg-success';
    case 'AJUSTE': return 'bg-warning text-dark';
    default: return 'bg-secondary';
  }
};

const movCardBorder = (type: string): string => {
  switch (type) {
    case 'VENTA': return 'border-danger';
    case 'COMPRA': return 'border-success';
    case 'AJUSTE': return 'border-warning';
    default: return 'border-secondary';
  }
};

onMounted(async () => {
  loading.value = true;
  const { Data } = await getMovementsByProduct(productId);
  movements.value = Data ?? [];
  loading.value = false;
});
</script>

<style scoped></style>
