<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Reportes</li>
        <li class="breadcrumb-item active">Mermas</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Reporte de <span class="fw-300"><i>Mermas</i></span></h2>
        </div>
        <div class="panel-container show">

          <!-- Filtros -->
          <div class="panel-content pt-0">
            <div class="row align-items-end g-2">
              <div class="col-6 col-md-3">
                <label class="form-label">Fecha Inicio</label>
                <input type="date" class="form-control form-control-sm" v-model="filtro.desde" />
              </div>
              <div class="col-6 col-md-3">
                <label class="form-label">Fecha Fin</label>
                <input type="date" class="form-control form-control-sm" v-model="filtro.hasta" />
              </div>
              <div class="col-12 col-md-3 d-flex gap-2">
                <button class="btn btn-primary btn-sm flex-fill" @click="load">
                  <i class="fal fa-search me-1"></i>Buscar
                </button>
                <button class="btn btn-success btn-sm flex-fill" :disabled="!reporte.Detalle.length" @click="exportar">
                  <i class="fal fa-file-excel me-1"></i>Excel
                </button>
              </div>
            </div>
          </div>

          <!-- Totales -->
          <div class="panel-content pt-0" v-if="cargado">
            <div class="row g-2">
              <div class="col-6 col-md-4">
                <div class="card border-0 bg-body-secondary text-center py-2">
                  <small class="text-muted">Eventos de baja</small>
                  <div class="fw-bold fs-5">{{ reporte.TotalEventos }}</div>
                </div>
              </div>
              <div class="col-6 col-md-4">
                <div class="card border-0 bg-body-secondary text-center py-2">
                  <small class="text-muted">Unidades perdidas</small>
                  <div class="fw-bold fs-5">{{ reporte.TotalUnidades }}</div>
                </div>
              </div>
              <div class="col-12 col-md-4">
                <div class="card border-0 bg-danger bg-opacity-10 text-center py-2">
                  <small class="text-muted">Valor perdido</small>
                  <div class="fw-bold fs-5 text-danger">{{ fmt(reporte.TotalValorPerdido) }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Sin buscar todavía -->
          <div class="panel-content pt-0" v-if="!cargado">
            <div class="text-center py-5">
              <i class="fal fa-search fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted">Seleccione un rango de fechas y haga clic en Buscar</p>
            </div>
          </div>

          <template v-else>
            <div class="panel-content pt-0" v-if="reporte.Detalle.length === 0">
              <div class="text-center py-5">
                <i class="fal fa-thumbs-up fa-3x text-muted d-block mb-3"></i>
                <p class="text-muted">No hubo bajas por vencimiento en este período.</p>
              </div>
            </div>

            <template v-else>
              <!-- Por producto -->
              <div class="panel-content pt-0">
                <h6 class="text-muted border-bottom pb-2 mb-3">Por producto</h6>
                <div class="table-responsive">
                  <table class="table table-hover table-sm align-middle mb-0">
                    <thead>
                      <tr>
                        <th>SKU</th>
                        <th>Producto</th>
                        <th class="text-center">Eventos</th>
                        <th class="text-center">Unidades</th>
                        <th class="text-end">Valor Perdido</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="p in reporte.PorProducto" :key="p.ProductId">
                        <td><small class="text-muted font-monospace">{{ p.ProductCode }}</small></td>
                        <td class="fw-semibold">{{ p.ProductName }}</td>
                        <td class="text-center">{{ p.Eventos }}</td>
                        <td class="text-center">{{ p.Unidades }}</td>
                        <td class="text-end text-danger fw-semibold">{{ fmt(p.ValorPerdido) }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <!-- Detalle -->
              <div class="panel-content pt-0">
                <h6 class="text-muted border-bottom pb-2 mb-3">Detalle</h6>
                <div class="table-responsive">
                  <table class="table table-hover table-sm align-middle mb-0">
                    <thead>
                      <tr>
                        <th>Fecha</th>
                        <th>Producto</th>
                        <th>Lote</th>
                        <th class="text-center">Cantidad</th>
                        <th class="text-end">Valor</th>
                        <th>Motivo</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="(d, i) in reporte.Detalle" :key="i">
                        <td class="text-nowrap"><small>{{ fmtDate(d.Created) }}</small></td>
                        <td>{{ d.ProductName }}</td>
                        <td><code class="bg-body-secondary rounded px-2 py-1 small">{{ d.LotCode || '—' }}</code></td>
                        <td class="text-center">{{ d.Cantidad }}</td>
                        <td class="text-end text-danger">{{ fmt(d.ValorPerdido) }}</td>
                        <td><small>{{ d.Reason ?? '—' }}</small></td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </template>
          </template>

        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import useStockMovement from '@/modules/inventory/composables/useStockMovement';
import { WriteOffReportResponse } from '@/modules/inventory/models/stockMovement.model';
import { exportToExcel } from '@/utils/excelHelper';
import { todayIso, firstOfMonthIso } from '@/utils/dateHelper';

const { getWriteOffs } = useStockMovement();
const reporte = ref(new WriteOffReportResponse());
const cargado = ref(false);

const filtro = ref({ desde: firstOfMonthIso(), hasta: todayIso() });

const fmt     = (v: number) => v.toLocaleString('es-BO', { style: 'currency', currency: 'BOB' });
const fmtDate = (v: string) => new Date(v).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });

const load = async () => {
  const { ok, Data } = await getWriteOffs(filtro.value.desde, filtro.value.hasta);
  if (ok) reporte.value = Data;
  cargado.value = true;
};

const exportar = () => {
  const rows = reporte.value.Detalle.map(d => ({
    Fecha:    fmtDate(d.Created),
    Producto: d.ProductName,
    SKU:      d.ProductCode,
    Lote:     d.LotCode ?? '',
    Cantidad: d.Cantidad,
    Valor:    d.ValorPerdido,
    Motivo:   d.Reason ?? '',
  }));
  exportToExcel(rows, `reporte_mermas_${filtro.value.desde}_${filtro.value.hasta}.xlsx`);
};
</script>
