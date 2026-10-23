<template>
  <Layout>
    <PageHeader title="Tablero Información" page-title="Inicio" />
<h1 class="subheader-title mb-2">Dashboard, Inventarios</h1>
    

    <!-- KPI Cards -->
      <div class="row g-3 mb-4">
        <div class="col-6 col-md-3">
          <div class="card h-100 border-0 shadow-sm">
            <div class="card-body">
              <div class="d-flex align-items-center justify-content-between mb-2">
                <small class="text-muted fw-semibold text-uppercase" style="font-size:.7rem">Ventas Hoy</small>
                <span class="badge bg-success bg-opacity-10 text-success rounded-pill">
                  <i class="ri-money-dollar-box-line"></i>
                </span>
              </div>
              <div class="h4 mb-0 fw-bold">Bs. {{ formatNum(kpi.TodaySalesTotal) }}</div>
              <small class="text-muted">{{ kpi.TodaySalesCount }} venta(s)</small>
            </div>
          </div>
        </div>

        <div class="col-6 col-md-3">
          <div class="card h-100 border-0 shadow-sm">
            <div class="card-body">
              <div class="d-flex align-items-center justify-content-between mb-2">
                <small class="text-muted fw-semibold text-uppercase" style="font-size:.7rem">Ventas del Mes</small>
                <span class="badge bg-primary bg-opacity-10 text-primary rounded-pill">
                  <i class="ri-line-chart-line"></i>
                </span>
              </div>
              <div class="h4 mb-0 fw-bold">Bs. {{ formatNum(kpi.MonthSalesTotal) }}</div>
              <small class="text-muted">{{ kpi.MonthSalesCount }} venta(s)</small>
            </div>
          </div>
        </div>

        <div class="col-6 col-md-3">
          <div class="card h-100 border-0 shadow-sm">
            <div class="card-body">
              <div class="d-flex align-items-center justify-content-between mb-2">
                <small class="text-muted fw-semibold text-uppercase" style="font-size:.7rem">Compras Pendientes</small>
                <span class="badge bg-warning bg-opacity-10 text-warning rounded-pill">
                  <i class="ri-shopping-cart-2-line"></i>
                </span>
              </div>
              <div class="h4 mb-0 fw-bold">{{ kpi.PendingPurchasesCount }}</div>
              <small class="text-muted">órdenes por recibir</small>
            </div>
          </div>
        </div>

        <div class="col-6 col-md-3">
          <div class="card h-100 border-0 shadow-sm">
            <div class="card-body">
              <div class="d-flex align-items-center justify-content-between mb-2">
                <small class="text-muted fw-semibold text-uppercase" style="font-size:.7rem">Stock Bajo</small>
                <span class="badge bg-danger bg-opacity-10 text-danger rounded-pill">
                  <i class="ri-archive-line"></i>
                </span>
              </div>
              <div class="h4 mb-0 fw-bold">{{ kpi.LowStockCount }}</div>
              <small class="text-muted">producto(s) bajo mínimo</small>
            </div>
          </div>
        </div>
      </div>

      <!-- Tablas -->
      <div class="row g-3">

        <!-- Últimas ventas del día -->
        <div class="col-12 col-lg-6">
          <div class="card border-0 shadow-sm h-100">
            <div class="card-header bg-transparent border-bottom">
              <h6 class="mb-0 fw-semibold">
                <i class="ri-bill-line me-2 text-success"></i>Últimas Ventas del Día
              </h6>
            </div>
            <div class="card-body p-0">
              <div v-if="kpi.RecentSales.length === 0" class="text-center py-4">
                <i class="ri-inbox-line ri-2x text-muted d-block mb-2"></i>
                <small class="text-muted">Sin ventas registradas hoy</small>
              </div>
              <table v-else class="table table-sm table-hover mb-0 align-middle">
                <thead class="">
                  <tr>
                    <th>Cliente</th>
                    <th>Hora</th>
                    <th class="text-end">Total</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="sale in kpi.RecentSales" :key="sale.Id">
                    <td>
                      <span class="fw-semibold">{{ sale.CustomerName }}</span>
                    </td>
                    <td>
                      <small class="text-muted">{{ formatTime(sale.SaleDate) }}</small>
                    </td>
                    <td class="text-end fw-semibold text-success">
                      Bs. {{ formatNum(sale.Total) }}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

        <!-- Productos con stock bajo -->
        <div class="col-12 col-lg-6">
          <div class="card border-0 shadow-sm h-100">
            <div class="card-header bg-transparent border-bottom">
              <h6 class="mb-0 fw-semibold">
                <i class="ri-error-warning-line me-2 text-danger"></i>Productos con Stock Crítico
              </h6>
            </div>
            <div class="card-body p-0">
              <div v-if="kpi.LowStockProducts.length === 0" class="text-center py-4">
                <i class="ri-checkbox-circle-line ri-2x text-success d-block mb-2"></i>
                <small class="text-muted">Todos los productos tienen stock suficiente</small>
              </div>
              <table v-else class="table table-sm table-hover mb-0 align-middle">
                <thead class="">
                  <tr>
                    <th>Producto</th>
                    <th class="text-center">Stock</th>
                    <th class="text-center">Mínimo</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="product in kpi.LowStockProducts" :key="product.Id">
                    <td>
                      <div class="fw-semibold">{{ product.ProductName }}</div>
                      <small class="text-muted">{{ product.ProductCode }}</small>
                    </td>
                    <td class="text-center">
                      <span class="badge bg-danger">{{ product.CurrentStock }}</span>
                    </td>
                    <td class="text-center">
                      <span class="text-muted">{{ product.MinReorderQuantity }}</span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>

      </div>
    
  </Layout>
</template>

<script setup lang="ts">
import Layout from '@/layouts/main.vue'
import PageHeader from '@/components/page-header.vue'
import { onMounted, ref } from 'vue';
import { DashboardKpi } from '../models/dashboard.model';
import useDashboard from '../composables/useDashboard';

const { getDashboard } = useDashboard();
const kpi = ref(new DashboardKpi());

onMounted(async () => {
  const { ok, Data } = await getDashboard();
  if (ok) kpi.value = Data;
});

const formatNum = (value: number) =>
  value.toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const formatTime = (dateStr: string) => {
  const d = new Date(dateStr);
  return d.toLocaleTimeString('es-BO', { hour: '2-digit', minute: '2-digit', hour12: false });
};
</script>

<style scoped>
.card { transition: box-shadow .15s; }
.card:hover { box-shadow: 0 .25rem .75rem rgba(0,0,0,.08) !important; }
</style>
