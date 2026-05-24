<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Reportes</li>
        <li class="breadcrumb-item active">Stock</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Reporte de <span class="fw-300"><i>Stock</i></span></h2>
        </div>
        <div class="panel-container show">

          <!-- Filtros -->
          <div class="panel-content pt-0">
            <div class="row align-items-end g-2">
              <div class="col-12 col-md-4">
                <label class="form-label">Producto</label>
                <input type="text" class="form-control form-control-sm" v-model="filtro.name"
                  placeholder="Buscar por nombre..." @keyup.enter="load" />
              </div>
              <div class="col-12 col-md-3">
                <label class="form-label">Estado</label>
                <select class="form-select form-select-sm" v-model="filtro.lowOnly">
                  <option :value="false">Todos</option>
                  <option :value="true">Solo stock bajo mínimo</option>
                </select>
              </div>
              <div class="col-12 col-md-3 d-flex gap-2">
                <button class="btn btn-primary btn-sm flex-fill" @click="load">
                  <i class="fal fa-search me-1"></i>Buscar
                </button>
                <button class="btn btn-success btn-sm flex-fill" :disabled="!displayRows.length" @click="exportar">
                  <i class="fal fa-file-excel me-1"></i>Excel
                </button>
              </div>
            </div>
          </div>

          <!-- Totales -->
          <div class="panel-content pt-0" v-if="displayRows.length">
            <div class="row g-2">
              <div class="col-6 col-md-3">
                <div class="card border-0 bg-light text-center py-2">
                  <small class="text-muted">Productos</small>
                  <div class="fw-bold fs-5">{{ displayRows.length }}</div>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="card border-0 bg-danger bg-opacity-10 text-center py-2">
                  <small class="text-muted">Bajo mínimo</small>
                  <div class="fw-bold fs-5 text-danger">{{ lowStockCount }}</div>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="card border-0 bg-light text-center py-2">
                  <small class="text-muted">Unidades totales</small>
                  <div class="fw-bold fs-5">{{ totalUnits.toLocaleString('es-BO') }}</div>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="card border-0 bg-success bg-opacity-10 text-center py-2">
                  <small class="text-muted">Valor inventario</small>
                  <div class="fw-bold fs-5 text-success">{{ fmt(totalValue) }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Tabla -->
          <div class="panel-content pt-0">
            <div v-if="!loaded" class="text-center py-5">
              <i class="fal fa-boxes fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted">Haga clic en Buscar para cargar el inventario</p>
            </div>
            <div v-else-if="!displayRows.length" class="text-center py-4">
              <p class="text-muted">Sin resultados para los filtros seleccionados</p>
            </div>
            <template v-else>
              <!-- Desktop -->
              <div class="d-none d-md-block table-responsive">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th>Código</th>
                      <th>Producto</th>
                      <th>Laboratorio</th>
                      <th class="text-center">Stock Actual</th>
                      <th class="text-center">Mínimo</th>
                      <th class="text-end">Precio</th>
                      <th class="text-end">Valor</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="p in displayRows" :key="p.Id"
                        :class="p.CurrentStock < p.MinReorderQuantity ? 'table-danger' : ''">
                      <td class="text-nowrap"><small>{{ p.ProductCode }}</small></td>
                      <td>{{ p.ProductName }}</td>
                      <td><small class="text-muted">{{ p.LaboratoryName }}</small></td>
                      <td class="text-center fw-semibold">{{ p.CurrentStock }}</td>
                      <td class="text-center text-muted">{{ p.MinReorderQuantity }}</td>
                      <td class="text-end">{{ fmt(p.SalePrice) }}</td>
                      <td class="text-end fw-semibold">{{ fmt(p.CurrentStock * p.SalePrice) }}</td>
                    </tr>
                  </tbody>
                  <tfoot class="table-light fw-bold">
                    <tr>
                      <td colspan="3">TOTALES</td>
                      <td class="text-center">{{ totalUnits.toLocaleString('es-BO') }}</td>
                      <td></td>
                      <td></td>
                      <td class="text-end text-success">{{ fmt(totalValue) }}</td>
                    </tr>
                  </tfoot>
                </table>
              </div>
              <!-- Mobile -->
              <div class="d-md-none">
                <div v-for="p in displayRows" :key="p.Id" class="card mb-2 shadow-sm"
                     :class="p.CurrentStock < p.MinReorderQuantity ? 'border-danger' : ''">
                  <div class="card-body py-2 px-3">
                    <div class="d-flex justify-content-between">
                      <span class="fw-semibold">{{ p.ProductName }}</span>
                      <span class="fw-bold">{{ fmt(p.CurrentStock * p.SalePrice) }}</span>
                    </div>
                    <div class="d-flex gap-3 mt-1">
                      <small class="text-muted">Stock: <strong :class="p.CurrentStock < p.MinReorderQuantity ? 'text-danger' : ''">{{ p.CurrentStock }}</strong></small>
                      <small class="text-muted">Mín: {{ p.MinReorderQuantity }}</small>
                      <small class="text-muted">Precio: {{ fmt(p.SalePrice) }}</small>
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
import useProduct from '@/modules/inventory/composables/useProduct';
import type { Product } from '@/modules/inventory/models/product.model';
import { exportToExcel } from '@/utils/excelHelper';

const { getProductsByName } = useProduct();
const products = ref<Product[]>([]);
const loaded   = ref(false);
const filtro   = ref({ name: '', lowOnly: false });

const displayRows = computed(() =>
  filtro.value.lowOnly
    ? products.value.filter(p => p.CurrentStock < p.MinReorderQuantity)
    : products.value
);

const lowStockCount = computed(() => products.value.filter(p => p.CurrentStock < p.MinReorderQuantity).length);
const totalUnits    = computed(() => displayRows.value.reduce((s, p) => s + p.CurrentStock, 0));
const totalValue    = computed(() => displayRows.value.reduce((s, p) => s + p.CurrentStock * p.SalePrice, 0));

const fmt = (v: number) => v.toLocaleString('es-BO', { style: 'currency', currency: 'BOB' });

const load = async () => {
  const { ok, Data } = await getProductsByName(filtro.value.name);
  if (ok) { products.value = Data ?? []; loaded.value = true; }
};

const exportar = () => {
  const rows = displayRows.value.map(p => ({
    Codigo:          p.ProductCode,
    Producto:        p.ProductName,
    Laboratorio:     p.LaboratoryName,
    Stock_Actual:    p.CurrentStock,
    Stock_Minimo:    p.MinReorderQuantity,
    Precio_Venta:    p.SalePrice,
    Valor_Inventario: +(p.CurrentStock * p.SalePrice).toFixed(2),
    Alerta:          p.CurrentStock < p.MinReorderQuantity ? 'BAJO MÍNIMO' : '',
  }));
  exportToExcel(rows, `reporte_stock_${new Date().toISOString().split('T')[0]}.xlsx`);
};
</script>
