<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventarios</li>
        <li class="breadcrumb-item active" aria-current="page">Traspasos entre Sucursales</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Traspasos de <span class="fw-300"><i>{{ authStore.getUser?.BranchName }}</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <div class="d-flex flex-wrap gap-2 align-items-center mb-3">
              <button v-if="canCreate" type="button" class="btn btn-sm btn-primary" @click="openTransfer('0')">
                <span class="fal fa-plus-square me-1"></span>Nuevo Traspaso
              </button>
              <span v-if="pendingToReceive > 0" class="badge bg-warning text-dark fs-6 ms-md-auto">
                <i class="fal fa-truck me-1"></i>{{ pendingToReceive }} por recibir
              </span>
            </div>

            <div class="row align-items-end g-2 mb-3">
              <div class="col-6 col-md-3">
                <label class="form-label">Desde</label>
                <input type="date" class="form-control form-control-sm" v-model="dateFrom" />
              </div>
              <div class="col-6 col-md-3">
                <label class="form-label">Hasta</label>
                <input type="date" class="form-control form-control-sm" v-model="dateTo" />
              </div>
              <div class="col-8 col-md-3">
                <label class="form-label">Estado</label>
                <select class="form-select form-select-sm" v-model="status">
                  <option value="">Todos</option>
                  <option v-for="(label, key) in transferStatusLabel" :key="key" :value="key">{{ label }}</option>
                </select>
              </div>
              <div class="col-4 col-md-3">
                <button type="button" class="btn btn-sm btn-primary w-100" @click="loadTransfers">Buscar</button>
              </div>
            </div>
            <small class="text-muted d-block mb-2">
              <i class="fal fa-info-circle me-1"></i>
              Los borradores y los traspasos en tránsito se muestran siempre, sin importar la fecha.
            </small>

            <div v-if="transfers.length === 0" class="text-center py-5">
              <i class="fal fa-exchange fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-0">No hay traspasos de esta sucursal en el período.</p>
            </div>

            <template v-else>
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead>
                    <tr>
                      <th>N°</th>
                      <th>Fecha</th>
                      <th>Movimiento</th>
                      <th class="text-center">Productos</th>
                      <th class="text-center">Unidades</th>
                      <th class="text-center">Estado</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="t in transfers" :key="t.Id">
                      <td class="fw-semibold">{{ t.Number }}</td>
                      <td><small>{{ formatDateTime(t.Created) }}</small></td>
                      <td>
                        <span v-if="t.Direction === 'salida'">
                          <i class="fal fa-arrow-right text-danger me-1"></i>Envía a <strong>{{ t.DestBranchName }}</strong>
                        </span>
                        <span v-else>
                          <i class="fal fa-arrow-left text-success me-1"></i>Llega de <strong>{{ t.OriginBranchName }}</strong>
                        </span>
                      </td>
                      <td class="text-center">{{ t.LinesCount }}</td>
                      <td class="text-center">{{ t.TotalQuantity }}</td>
                      <td class="text-center">
                        <span class="badge" :class="transferStatusBadge[t.Status]">{{ transferStatusLabel[t.Status] }}</span>
                      </td>
                      <td class="text-center">
                        <button type="button" class="btn btn-sm"
                          :class="actionFor(t).cls" @click="openTransfer(t.Id)">
                          <span :class="actionFor(t).icon" class="me-1"></span>{{ actionFor(t).label }}
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <div class="d-md-none">
                <div class="row g-2">
                  <div class="col-12" v-for="t in transfers" :key="'m' + t.Id">
                    <div class="card shadow-sm">
                      <div class="card-body py-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <span class="fw-semibold">Traspaso {{ t.Number }}</span>
                          <span class="badge" :class="transferStatusBadge[t.Status]">{{ transferStatusLabel[t.Status] }}</span>
                        </div>
                        <small class="text-muted d-block">
                          {{ t.Direction === 'salida' ? `Envía a ${t.DestBranchName}` : `Llega de ${t.OriginBranchName}` }}
                          · {{ t.TotalQuantity }} u. · {{ formatDateTime(t.Created) }}
                        </small>
                        <button type="button" class="btn btn-sm w-100 mt-2"
                          :class="actionFor(t).cls" @click="openTransfer(t.Id)">
                          <span :class="actionFor(t).icon" class="me-1"></span>{{ actionFor(t).label }}
                        </button>
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
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import useStockTransfer from '@/modules/inventory/composables/useStockTransfer';
import {
  type StockTransfer, transferStatusBadge, transferStatusLabel,
} from '@/modules/inventory/models/stockTransfer.model';
import { useAuthStore } from '@/modules/auth/stores/auth.store';
import { usePermissions } from '@/modules/common/composables/usePermissions';
import { firstOfMonthIso, todayIso } from '@/utils/dateHelper';

const router = useRouter();
const authStore = useAuthStore();
const { can } = usePermissions();
const { getTransfers } = useStockTransfer();

const canCreate = computed(() => can('stock-transfers', 'create'));

const transfers = ref<StockTransfer[]>([]);
const dateFrom = ref(firstOfMonthIso());
const dateTo = ref(todayIso());
const status = ref('');

const pendingToReceive = computed(() =>
  transfers.value.filter(t => t.Direction === 'entrada' && t.Status === 'enviado').length);

onMounted(() => loadTransfers());

const loadTransfers = async () => {
  const { ok, Data } = await getTransfers(dateFrom.value, dateTo.value, status.value);
  if (ok) transfers.value = Data;
};

/** Lo que se puede hacer con el traspaso desde esta sucursal, para el botón de la fila. */
const actionFor = (t: StockTransfer) => {
  if (t.Status === 'enviado' && t.Direction === 'entrada')
    return { label: 'Recibir', icon: 'fal fa-inbox-in', cls: 'btn-warning' };
  if (t.Status === 'borrador' && t.Direction === 'salida')
    return { label: 'Continuar', icon: 'fal fa-edit', cls: 'btn-outline-primary' };
  return { label: 'Ver', icon: 'fal fa-eye', cls: 'btn-outline-secondary' };
};

const openTransfer = (id: string) => {
  router.push({ name: 'stock-transfer', params: { id } });
};

const formatDateTime = (value: string) =>
  value ? new Date(value).toLocaleString('es-BO', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false,
  }) : '—';
</script>

<style scoped></style>
