<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item active" aria-current="page">Registro de Ventas</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>VENTAS</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <!-- Filtros -->
            <div class="mb-3">
              <div class="d-flex flex-wrap align-items-center gap-2 mb-2">
                <span class="text-muted small me-1">Ver:</span>
                <button
                  v-for="q in quickFilters" :key="q.key"
                  type="button"
                  class="btn btn-sm"
                  :class="activeQuick === q.key ? 'btn-primary' : 'btn-outline-secondary'"
                  @click="applyQuick(q.key)"
                >
                  <i :class="q.icon" class="me-1"></i>{{ q.label }}
                </button>
              </div>

              <div class="row align-items-end g-2">
                <div class="col-6 col-md-3">
                  <label class="form-label small text-muted mb-1">Desde</label>
                  <input type="date" class="form-control form-control-sm"
                         v-model="filtro.dateInitial" @change="activeQuick = null" />
                </div>
                <div class="col-6 col-md-3">
                  <label class="form-label small text-muted mb-1">Hasta</label>
                  <input type="date" class="form-control form-control-sm"
                         v-model="filtro.dateEnd" @change="activeQuick = null" />
                </div>
                <div class="col-6 col-md-3">
                  <label class="form-label small text-muted mb-1">Vendedor</label>
                  <select class="form-select form-select-sm" v-model="filtro.seller">
                    <option value="">Todos</option>
                    <option v-for="s in sellerOptions" :key="s" :value="s">{{ s }}</option>
                  </select>
                </div>
                <div class="col-6 col-md-3 d-flex gap-2">
                  <button class="btn btn-primary btn-sm flex-fill" @click="getSalesData">
                    <span class="fal fa-search me-1"></span>Buscar
                  </button>
                  <button
                    v-if="filteredSales.length > 0"
                    class="btn btn-outline-success btn-sm"
                    @click="exportExcel"
                    title="Exportar a Excel"
                  >
                    <span class="fal fa-file-excel"></span>
                  </button>
                </div>
              </div>
            </div>

            <!-- KPIs del período -->
            <div v-if="filteredSales.length > 0" class="row g-2 mb-3">
              <div class="col-6 col-md-3">
                <div class="kpi-card">
                  <div class="kpi-label"><i class="fal fa-receipt me-1"></i>Ventas</div>
                  <div class="kpi-value">{{ filteredSales.length }}</div>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="kpi-card">
                  <div class="kpi-label"><i class="fal fa-calculator me-1"></i>Subtotal</div>
                  <div class="kpi-value">{{ formatCurrency(totalSubtotalPeriod) }}</div>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="kpi-card kpi-card--discount">
                  <div class="kpi-label"><i class="fal fa-tag me-1"></i>Descuentos</div>
                  <div class="kpi-value text-danger">− {{ formatCurrency(totalDiscountsPeriod) }}</div>
                  <div class="kpi-sub" v-if="totalSubtotalPeriod > 0">
                    {{ discountRatePct }}% del subtotal
                  </div>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="kpi-card kpi-card--total">
                  <div class="kpi-label"><i class="fal fa-check-circle me-1"></i>Total cobrado</div>
                  <div class="kpi-value">{{ formatCurrency(totalPeriod) }}</div>
                </div>
              </div>
            </div>

            <!-- Contador -->
            <div v-if="filteredSales.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="fal fa-list me-1"></span>
                <strong>{{ filteredSales.length }}</strong> venta(s) encontrada(s)
              </small>
            </div>

            <!-- Estado vacío -->
            <div v-if="filteredSales.length === 0" class="text-center py-5">
              <i class="fal fa-receipt fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-0">No se encontraron ventas en el período seleccionado.</p>
            </div>

            <template v-else>
              <!-- Tabla desktop -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="">
                    <tr>
                      <th>Fecha</th>
                      <th>Cliente</th>
                      <th>Vendedor</th>
                      <th class="text-end">Subtotal</th>
                      <th class="text-end">Descuentos</th>
                      <th class="text-end">Total</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(sale, index) in filteredSales" :key="index">
                      <td>{{ formatDate(sale.SaleDate) }}</td>
                      <td class="fw-semibold">{{ sale.CustomerName }}</td>
                      <td><small class="text-muted"><i class="fal fa-user me-1"></i>{{ sale.SellerName || '—' }}</small></td>
                      <td class="text-end">{{ formatCurrency(sale.Subtotal) }}</td>
                      <td class="text-end">
                        <span v-if="sale.TotalDiscounts > 0" class="text-danger fw-semibold">
                          − {{ formatCurrency(sale.TotalDiscounts) }}
                        </span>
                        <span v-else class="text-muted">—</span>
                      </td>
                      <td class="text-end fw-semibold">{{ formatCurrency(sale.Total) }}</td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Ver detalle"
                          @click="viewDetail(sale.Id)"
                        >
                          <span class="fal fa-eye"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeSale(sale.Id)"
                        >
                          <span class="fal fa-trash-alt"></span>
                        </button>
                      </td>
                    </tr>
                  </tbody>
                  <tfoot>
                    <tr class="fw-bold">
                      <td colspan="3" class="text-end text-muted small">TOTALES DEL PERÍODO</td>
                      <td class="text-end">{{ formatCurrency(totalSubtotalPeriod) }}</td>
                      <td class="text-end text-danger">
                        <span v-if="totalDiscountsPeriod > 0">− {{ formatCurrency(totalDiscountsPeriod) }}</span>
                        <span v-else class="text-muted">—</span>
                      </td>
                      <td class="text-end text-primary">{{ formatCurrency(totalPeriod) }}</td>
                      <td></td>
                    </tr>
                  </tfoot>
                </table>
              </div>

              <!-- Cards móvil -->
              <div class="d-md-none">
                <div class="row g-3">
                  <div class="col-12" v-for="(sale, index) in filteredSales" :key="index">
                    <div class="card shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <p class="fw-semibold mb-0 lh-sm">{{ sale.CustomerName }}</p>
                          <span class="fs-6 fw-bold text-primary">{{ formatCurrency(sale.Total) }}</span>
                        </div>
                        <small class="text-muted"><i class="fal fa-calendar me-1"></i>{{ formatDate(sale.SaleDate) }}</small>
                        <small class="text-muted"><i class="fal fa-user me-1"></i>{{ sale.SellerName || '—' }}</small>
                        <!-- Desglose de importes cuando hay descuentos -->
                        <div v-if="sale.TotalDiscounts > 0" class="d-flex gap-3 px-2 py-1 rounded bg-body-secondary">
                          <div class="text-center flex-fill">
                            <div class="kpi-label">Subtotal</div>
                            <div class="small fw-semibold">{{ formatCurrency(sale.Subtotal) }}</div>
                          </div>
                          <div class="text-center flex-fill">
                            <div class="kpi-label">Desc.</div>
                            <div class="small fw-semibold text-danger">− {{ formatCurrency(sale.TotalDiscounts) }}</div>
                          </div>
                          <div class="text-center flex-fill">
                            <div class="kpi-label">Total</div>
                            <div class="small fw-semibold text-primary">{{ formatCurrency(sale.Total) }}</div>
                          </div>
                        </div>
                        <div class="d-flex gap-2 pt-1">
                          <button type="button" class="btn btn-sm btn-outline-primary flex-grow-1"
                            @click="viewDetail(sale.Id)">
                            <span class="fal fa-eye me-1"></span>Ver Detalle
                          </button>
                          <button type="button" class="btn btn-sm btn-outline-danger"
                            @click="removeSale(sale.Id)">
                            <span class="fal fa-trash-alt"></span>
                          </button>
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
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import useSales from '@/modules/inventory/composables/useSales';
import type { Sale } from '@/modules/inventory/models/sale.model';
import { exportToExcel } from '@/utils/excelHelper';
import utils from '@/utils/msg';

const sales = ref<Sale[]>([]);
const { getSales, deleteSale } = useSales();
const router = useRouter();

// ── Helpers de fecha (local, no UTC) ──────────────────────
const toLocalDateStr = (d: Date): string => {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
};

const today = toLocalDateStr(new Date());

const getWeekStart = (): string => {
  const d = new Date();
  const day = d.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  d.setDate(d.getDate() + diff);
  return toLocalDateStr(d);
};

const getMonthStart = (): string =>
  toLocalDateStr(new Date(new Date().getFullYear(), new Date().getMonth(), 1));

// ── Accesos rápidos ────────────────────────────────────────
const quickFilters = [
  { key: 'today',  label: 'Hoy',        icon: 'fal fa-sun' },
  { key: 'week',   label: 'Esta semana', icon: 'fal fa-calendar-week' },
  { key: 'month',  label: 'Este mes',    icon: 'fal fa-calendar-alt' },
] as const;

type QuickKey = typeof quickFilters[number]['key'];

const activeQuick = ref<QuickKey | null>('today');
const filtro = ref({ dateInitial: today, dateEnd: today, seller: '' });

const applyQuick = (key: QuickKey) => {
  activeQuick.value = key;
  if (key === 'today') filtro.value = { ...filtro.value, dateInitial: today, dateEnd: today };
  if (key === 'week')  filtro.value = { ...filtro.value, dateInitial: getWeekStart(), dateEnd: today };
  if (key === 'month') filtro.value = { ...filtro.value, dateInitial: getMonthStart(), dateEnd: today };
  getSalesData();
};

// ── Computed ───────────────────────────────────────────────
const sellerOptions = computed(() => {
  const names = sales.value.map(s => s.SellerName).filter(n => !!n);
  return [...new Set(names)].sort();
});

const filteredSales = computed(() =>
  filtro.value.seller
    ? sales.value.filter(s => s.SellerName === filtro.value.seller)
    : sales.value
);

const totalSubtotalPeriod = computed(() =>
  +filteredSales.value.reduce((s, v) => s + v.Subtotal, 0).toFixed(2)
);
const totalDiscountsPeriod = computed(() =>
  +filteredSales.value.reduce((s, v) => s + v.TotalDiscounts, 0).toFixed(2)
);
const totalPeriod = computed(() =>
  +filteredSales.value.reduce((s, v) => s + v.Total, 0).toFixed(2)
);
const discountRatePct = computed(() =>
  totalSubtotalPeriod.value > 0
    ? ((totalDiscountsPeriod.value / totalSubtotalPeriod.value) * 100).toFixed(1)
    : '0.0'
);

// ── Formatters ─────────────────────────────────────────────
const formatDate = (val: string | Date): string => {
  if (!val) return '—';
  return new Date(val).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const formatCurrency = (val: number): string =>
  (val ?? 0).toLocaleString('es-BO', { style: 'currency', currency: 'BOB' });

// ── Datos ──────────────────────────────────────────────────
const getSalesData = async () => {
  const { Data } = await getSales(filtro.value.dateInitial, filtro.value.dateEnd);
  sales.value = Data ?? [];
};

const viewDetail = (id: string) => router.push({ name: 'sale-detail', params: { id } });

const removeSale = async (id: string) => {
  const ok = await utils.showMessageQuestion('¿Desea eliminar esta venta?');
  if (ok) {
    const { ok: deleted } = await deleteSale(id);
    if (deleted) {
      await utils.showMessageModal({ Description: 'La venta se eliminó correctamente.', MessageType: 'success' });
      await getSalesData();
    }
  }
};

// ── Export Excel ───────────────────────────────────────────
const exportExcel = () => {
  const rows = filteredSales.value.map(s => ({
    Fecha:       formatDate(s.SaleDate),
    Cliente:     s.CustomerName,
    Vendedor:    s.SellerName || '',
    Subtotal:    s.Subtotal,
    Descuentos:  s.TotalDiscounts,
    Total:       s.Total,
  }));

  // Fila de totales al final
  rows.push({
    Fecha:      'TOTALES',
    Cliente:    '',
    Vendedor:   '',
    Subtotal:   totalSubtotalPeriod.value,
    Descuentos: totalDiscountsPeriod.value,
    Total:      totalPeriod.value,
  });

  const fileName = `ventas_${filtro.value.dateInitial}_${filtro.value.dateEnd}.xlsx`;
  exportToExcel(rows, fileName);
};

onMounted(() => applyQuick('today'));
</script>

<style scoped>
.kpi-card {
  background: var(--bs-tertiary-bg, #f8f9fa);
  border: 1px solid var(--bs-border-color);
  border-radius: 0.5rem;
  padding: 0.6rem 0.75rem;
  text-align: center;
}
.kpi-card--discount {
  border-color: rgba(var(--bs-danger-rgb), 0.3);
  background: rgba(var(--bs-danger-rgb), 0.04);
}
.kpi-card--total {
  border-color: rgba(var(--bs-primary-rgb), 0.35);
  background: rgba(var(--bs-primary-rgb), 0.06);
}
.kpi-label {
  font-size: 0.68rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--bs-secondary-color, #6c757d);
  margin-bottom: 0.15rem;
}
.kpi-value {
  font-weight: 700;
  font-size: 0.9rem;
}
.kpi-sub {
  font-size: 0.65rem;
  color: var(--bs-secondary-color, #6c757d);
  margin-top: 0.1rem;
}
</style>
