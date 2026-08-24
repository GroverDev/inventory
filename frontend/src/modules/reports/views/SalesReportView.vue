<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Reportes</li>
        <li class="breadcrumb-item active">Ventas</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Reporte de <span class="fw-300"><i>Ventas</i></span></h2>
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
              <div class="col-12 col-md-3 d-flex gap-2">
                <button class="btn btn-primary btn-sm flex-fill" @click="load">
                  <i class="fal fa-search me-1"></i>Buscar
                </button>
                <button class="btn btn-success btn-sm flex-fill" :disabled="!sales.length" @click="exportar">
                  <i class="fal fa-file-excel me-1"></i>Excel
                </button>
              </div>
            </div>
          </div>

          <!-- Totales -->
          <div class="panel-content pt-0" v-if="sales.length">
            <div class="row g-2">
              <div class="col-6 col-md-3">
                <div class="card border-0 bg-body-secondary text-center py-2">
                  <small class="text-muted">Registros</small>
                  <div class="fw-bold fs-5">{{ sales.length }}</div>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="card border-0 bg-body-secondary text-center py-2">
                  <small class="text-muted">Subtotal</small>
                  <div class="fw-bold fs-5">{{ fmt(totalSubtotal) }}</div>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="card border-0 bg-body-secondary text-center py-2">
                  <small class="text-muted">Descuentos</small>
                  <div class="fw-bold fs-5 text-danger">{{ fmt(totalDiscounts) }}</div>
                </div>
              </div>
              <div class="col-6 col-md-3" v-if="totalReturned > 0">
                <div class="card border-0 bg-body-secondary text-center py-2">
                  <small class="text-muted">Devoluciones</small>
                  <div class="fw-bold fs-5 text-warning">− {{ fmt(totalReturned) }}</div>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="card border-0 bg-success bg-opacity-10 text-center py-2">
                  <small class="text-muted">Total Neto</small>
                  <div class="fw-bold fs-5 text-success">{{ fmt(totalNet) }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Tabla -->
          <div class="panel-content pt-0">
            <div v-if="!sales.length" class="text-center py-5">
              <i class="fal fa-receipt fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted">Seleccione un rango de fechas y haga clic en Buscar</p>
            </div>
            <template v-else>
              <!-- Desktop -->
              <div class="d-none d-md-block table-responsive">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="">
                    <tr>
                      <th>Fecha</th>
                      <th>Cliente</th>
                      <th class="text-end">Subtotal</th>
                      <th class="text-end">Descuentos</th>
                      <th class="text-end">Devuelto</th>
                      <th class="text-end">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="s in sales" :key="s.Id">
                      <td class="text-nowrap">{{ fmtDate(s.SaleDate) }}</td>
                      <td>{{ s.CustomerName }}</td>
                      <td class="text-end">{{ fmt(s.Subtotal) }}</td>
                      <td class="text-end text-danger">{{ fmt(s.TotalDiscounts) }}</td>
                      <td class="text-end text-warning">
                        <span v-if="s.TotalReturned > 0">− {{ fmt(s.TotalReturned) }}</span>
                        <span v-else class="text-muted">—</span>
                      </td>
                      <td class="text-end fw-semibold">{{ fmt(s.NetTotal) }}</td>
                    </tr>
                  </tbody>
                  <tfoot class="fw-bold">
                    <tr>
                      <td colspan="2">TOTALES</td>
                      <td class="text-end">{{ fmt(totalSubtotal) }}</td>
                      <td class="text-end text-danger">{{ fmt(totalDiscounts) }}</td>
                      <td class="text-end text-warning">
                        <span v-if="totalReturned > 0">− {{ fmt(totalReturned) }}</span>
                        <span v-else class="text-muted">—</span>
                      </td>
                      <td class="text-end text-success">{{ fmt(totalNet) }}</td>
                    </tr>
                  </tfoot>
                </table>
              </div>
              <!-- Mobile -->
              <div class="d-md-none">
                <div v-for="s in sales" :key="s.Id" class="card mb-2 shadow-sm">
                  <div class="card-body py-2 px-3">
                    <div class="d-flex justify-content-between">
                      <span class="fw-semibold">{{ s.CustomerName }}</span>
                      <span class="fw-bold text-success">{{ fmt(s.NetTotal) }}</span>
                    </div>
                    <small class="text-muted">{{ fmtDate(s.SaleDate) }}</small>
                    <span v-if="s.TotalDiscounts > 0" class="ms-2 text-danger small">
                      desc. {{ fmt(s.TotalDiscounts) }}
                    </span>
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
import useSales from '@/modules/inventory/composables/useSales';
import type { Sale } from '@/modules/inventory/models/sale.model';
import { exportToExcel } from '@/utils/excelHelper';
import { todayIso, firstOfMonthIso } from '@/utils/dateHelper';

const { getSales } = useSales();
const sales = ref<Sale[]>([]);

const today = todayIso();
const firstOfMonth = firstOfMonthIso();
const filtro = ref({ dateInitial: firstOfMonth, dateEnd: today });

const totalSubtotal  = computed(() => sales.value.reduce((s, v) => s + v.Subtotal, 0));
const totalDiscounts = computed(() => sales.value.reduce((s, v) => s + v.TotalDiscounts, 0));
// Neto de verdad: el total facturado menos lo devuelto. Antes esto sumaba
// v.Total (bruto) y se rotulaba "Total Neto", que era justamente lo que no era.
const totalReturned  = computed(() => sales.value.reduce((s, v) => s + v.TotalReturned, 0));
const totalNet       = computed(() => sales.value.reduce((s, v) => s + v.NetTotal, 0));

const fmt     = (v: number) => v.toLocaleString('es-BO', { style: 'currency', currency: 'BOB' });
const fmtDate = (v: string | Date) => new Date(v).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });

const load = async () => {
  const { ok, Data } = await getSales(filtro.value.dateInitial, filtro.value.dateEnd, 1, 10000);
  if (ok) sales.value = Data?.Items ?? [];
};

const exportar = () => {
  const rows = sales.value.map(s => ({
    Fecha:       fmtDate(s.SaleDate),
    Cliente:     s.CustomerName,
    Subtotal:    s.Subtotal,
    Descuentos:  s.TotalDiscounts,
    Facturado:   s.Total,
    Devuelto:    s.TotalReturned,
    Total:       s.NetTotal,
  }));
  rows.push({ Fecha: 'TOTALES', Cliente: '', Subtotal: totalSubtotal.value, Descuentos: totalDiscounts.value, Facturado: totalNet.value + totalReturned.value, Devuelto: totalReturned.value, Total: totalNet.value });
  exportToExcel(rows, `reporte_ventas_${filtro.value.dateInitial}_${filtro.value.dateEnd}.xlsx`);
};
</script>
