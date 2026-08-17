<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Reportes</li>
        <li class="breadcrumb-item active">Compras</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Reporte de <span class="fw-300"><i>Compras</i></span></h2>
        </div>
        <div class="panel-container show">

          <!-- Filtros -->
          <div class="panel-content pt-0">
            <div class="row align-items-end g-2">
              <div class="col-6 col-md-3">
                <label class="form-label">Fecha Inicio</label>
                <input type="date" class="form-control form-control-sm" v-model="filtro.dateInitial" />
              </div>
              <div class="col-6 col-md-3">
                <label class="form-label">Fecha Fin</label>
                <input type="date" class="form-control form-control-sm" v-model="filtro.dateEnd" />
              </div>
              <div class="col-12 col-md-2">
                <label class="form-label">Estado</label>
                <select class="form-select form-select-sm" v-model.number="filtro.statusId">
                  <option :value="1">Solicitado</option>
                  <option :value="2">Parc. Recibido</option>
                  <option :value="3">Tot. Recibido</option>
                </select>
              </div>
              <div class="col-12 col-md-3 d-flex gap-2">
                <button class="btn btn-primary btn-sm flex-fill" @click="load">
                  <i class="fal fa-search me-1"></i>Buscar
                </button>
                <button class="btn btn-success btn-sm flex-fill" :disabled="!purchases.length" @click="exportar">
                  <i class="fal fa-file-excel me-1"></i>Excel
                </button>
              </div>
            </div>
          </div>

          <!-- Totales -->
          <div class="panel-content pt-0" v-if="purchases.length">
            <div class="row g-2">
              <div class="col-6 col-md-4">
                <div class="card border-0 bg-light text-center py-2">
                  <small class="text-muted">Órdenes</small>
                  <div class="fw-bold fs-5">{{ purchases.length }}</div>
                </div>
              </div>
              <div class="col-6 col-md-4">
                <div class="card border-0 bg-light text-center py-2">
                  <small class="text-muted">Proveedores distintos</small>
                  <div class="fw-bold fs-5">{{ uniqueProviders }}</div>
                </div>
              </div>
              <div class="col-12 col-md-4">
                <div class="card border-0 bg-warning bg-opacity-10 text-center py-2">
                  <small class="text-muted">Total Compras</small>
                  <div class="fw-bold fs-5">{{ fmt(totalAmount) }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Tabla -->
          <div class="panel-content pt-0">
            <div v-if="!purchases.length" class="text-center py-5">
              <i class="fal fa-shopping-cart fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted">Seleccione un rango de fechas y haga clic en Buscar</p>
            </div>
            <template v-else>
              <!-- Desktop -->
              <div class="d-none d-md-block table-responsive">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="">
                    <tr>
                      <th>Fecha</th>
                      <th>Proveedor</th>
                      <th class="text-center">Estado</th>
                      <th>Entrega Est.</th>
                      <th class="text-end">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="p in purchases" :key="p.Id">
                      <td class="text-nowrap">{{ formatDateOnly(p.PurchaseDate) }}</td>
                      <td>{{ p.ProviderName }}</td>
                      <td class="text-center">
                        <span :class="statusBadge(p.PurchaseStatusId)">{{ statusLabel(p.PurchaseStatusId) }}</span>
                      </td>
                      <td><small class="text-muted">{{ p.EstimatedDeliveryDate ? formatDateOnly(p.EstimatedDeliveryDate) : '—' }}</small></td>
                      <td class="text-end fw-semibold">{{ fmt(p.Total) }}</td>
                    </tr>
                  </tbody>
                  <tfoot class="fw-bold">
                    <tr>
                      <td colspan="4">TOTAL</td>
                      <td class="text-end">{{ fmt(totalAmount) }}</td>
                    </tr>
                  </tfoot>
                </table>
              </div>
              <!-- Mobile -->
              <div class="d-md-none">
                <div v-for="p in purchases" :key="p.Id" class="card mb-2 shadow-sm">
                  <div class="card-body py-2 px-3">
                    <div class="d-flex justify-content-between">
                      <span class="fw-semibold">{{ p.ProviderName }}</span>
                      <span class="fw-bold">{{ fmt(p.Total) }}</span>
                    </div>
                    <div class="d-flex gap-2 mt-1">
                      <small class="text-muted">{{ formatDateOnly(p.PurchaseDate) }}</small>
                      <span :class="statusBadge(p.PurchaseStatusId)" style="font-size:.7rem">{{ statusLabel(p.PurchaseStatusId) }}</span>
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
import { ref, computed } from 'vue';
import usePurchase from '@/modules/inventory/composables/usePurchase';
import type { Purchase } from '@/modules/inventory/models/purchase.model';
import { exportToExcel } from '@/utils/excelHelper';
import { todayIso, firstOfMonthIso, formatDateOnly } from '@/utils/dateHelper';

const { getPurchases } = usePurchase();
const purchases = ref<Purchase[]>([]);

const today        = todayIso();
const firstOfMonth = firstOfMonthIso();
const filtro = ref({ dateInitial: firstOfMonth, dateEnd: today, statusId: 1 });

const totalAmount    = computed(() => purchases.value.reduce((s, p) => s + p.Total, 0));
const uniqueProviders = computed(() => new Set(purchases.value.map(p => p.ProviderId)).size);

const fmt     = (v: number) => v.toLocaleString('es-BO', { style: 'currency', currency: 'BOB' });
const statusBadge = (id: number) => id === 3 ? 'badge bg-success' : id === 2 ? 'badge bg-warning text-dark' : 'badge bg-info text-dark';
const statusLabel = (id: number) => id === 3 ? 'Tot. Recibido' : id === 2 ? 'Parc. Recibido' : 'Solicitado';

const load = async () => {
  const { ok, Data } = await getPurchases(filtro.value.dateInitial, filtro.value.dateEnd, filtro.value.statusId);
  if (ok) purchases.value = Data ?? [];
};

const exportar = () => {
  const rows = purchases.value.map(p => ({
    Fecha:            formatDateOnly(p.PurchaseDate),
    Proveedor:        p.ProviderName,
    Estado:           statusLabel(p.PurchaseStatusId),
    Entrega_Estimada: p.EstimatedDeliveryDate ? formatDateOnly(p.EstimatedDeliveryDate) : '',
    Total:            p.Total,
  }));
  rows.push({ Fecha: 'TOTAL', Proveedor: '', Estado: '', Entrega_Estimada: '', Total: totalAmount.value });
  exportToExcel(rows, `reporte_compras_${filtro.value.dateInitial}_${filtro.value.dateEnd}.xlsx`);
};
</script>
