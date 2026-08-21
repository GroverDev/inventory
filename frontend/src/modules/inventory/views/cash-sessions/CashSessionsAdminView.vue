<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Punto de Venta</li>
        <li class="breadcrumb-item active" aria-current="page">Turnos de Caja</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Turnos de <span class="fw-300"><i>CAJA</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <!-- Filtros -->
            <div class="mb-3">
              <div class="d-flex flex-wrap align-items-center gap-2 mb-2">
                <span class="text-muted small me-1">Ver:</span>
                <button v-for="q in quickFilters" :key="q.key" type="button"
                  class="btn btn-sm" :class="activeQuick === q.key ? 'btn-primary' : 'btn-outline-secondary'"
                  @click="applyQuick(q.key)">
                  <i :class="q.icon" class="me-1"></i>{{ q.label }}
                </button>
              </div>
              <div class="row align-items-end g-2">
                <div class="col-6 col-md-3">
                  <label class="form-label small text-muted mb-1">Desde</label>
                  <input type="date" class="form-control form-control-sm" v-model="filtro.dateFrom" @change="activeQuick = null" />
                </div>
                <div class="col-6 col-md-3">
                  <label class="form-label small text-muted mb-1">Hasta</label>
                  <input type="date" class="form-control form-control-sm" v-model="filtro.dateTo" @change="activeQuick = null" />
                </div>
                <div class="col-12 col-md-3">
                  <button class="btn btn-primary btn-sm w-100" @click="loadSessions">
                    <i class="fal fa-search me-1"></i>Buscar
                  </button>
                </div>
              </div>
            </div>

            <!-- Estado vacío -->
            <div v-if="sessions.length === 0" class="text-center py-5">
              <i class="fal fa-cash-register fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-0">No se encontraron turnos de caja en el período seleccionado.</p>
            </div>

            <!-- Tabla desktop -->
            <div v-else class="d-none d-md-block">
              <table class="table table-hover table-sm align-middle mb-0">
                <thead class="">
                  <tr>
                    <th>Cajero</th>
                    <th>Apertura</th>
                    <th>Cierre</th>
                    <th class="text-end">Fondo</th>
                    <th class="text-end">Ventas</th>
                    <th class="text-end">Gastos</th>
                    <th class="text-end">Esperado</th>
                    <th class="text-end">Declarado</th>
                    <th class="text-end">Diferencia</th>
                    <th class="text-center">Estado</th>
                    <th class="text-center">Detalle</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="s in sessions" :key="s.Id">
                    <td>
                      <i class="fal fa-user me-1 text-muted"></i>
                      <span class="fw-semibold">{{ s.UserFullName }}</span>
                    </td>
                    <td><small>{{ formatDate(s.OpenedAt) }}</small></td>
                    <td><small>{{ s.ClosedAt ? formatDate(s.ClosedAt) : '—' }}</small></td>
                    <td class="text-end small">Bs. {{ formatNum(s.OpeningAmount) }}</td>
                    <td class="text-end small text-success">Bs. {{ formatNum(s.TotalSales) }}</td>
                    <td class="text-end small text-danger">Bs. {{ formatNum(s.TotalExpenses + s.TotalWithdrawals) }}</td>
                    <td class="text-end small fw-semibold">
                      Bs. {{ formatNum(expectedAmount(s)) }}
                    </td>
                    <td class="text-end small">{{ s.DeclaredAmount !== null ? 'Bs. ' + formatNum(s.DeclaredAmount) : '—' }}</td>
                    <td class="text-end small fw-semibold"
                      :class="s.Difference === null ? '' : s.Difference >= 0 ? 'text-success' : 'text-danger'">
                      {{ s.Difference !== null ? 'Bs. ' + formatNum(s.Difference) : '—' }}
                    </td>
                    <td class="text-center">
                      <span class="badge" :class="isStaleOpen(s) ? 'bg-warning text-dark' : s.IsOpen ? 'bg-success' : 'bg-secondary'">
                        {{ s.IsOpen ? 'Abierta' : 'Cerrada' }}
                      </span>
                      <small v-if="isStaleOpen(s)" class="d-block text-warning mt-1">
                        <i class="fal fa-exclamation-triangle me-1"></i>hace {{ daysOpen(s) }} día{{ daysOpen(s) === 1 ? '' : 's' }}
                      </small>
                    </td>
                    <td class="text-center">
                      <button class="btn btn-sm btn-outline-secondary py-0 px-2" @click="selectSession(s)">
                        <i class="fal fa-eye"></i>
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- Cards móvil -->
            <div class="d-md-none">
              <div class="row g-3">
                <div v-for="s in sessions" :key="s.Id" class="col-12">
                  <div class="card shadow rounded-3">
                    <div class="card-body d-flex flex-column gap-2">
                      <div class="d-flex justify-content-between align-items-center">
                        <p class="fw-semibold mb-0 lh-sm">{{ s.UserFullName }}</p>
                        <span class="badge rounded-pill" :class="isStaleOpen(s) ? 'text-bg-warning' : s.IsOpen ? 'text-bg-success' : 'text-bg-secondary'">
                          {{ s.IsOpen ? 'Abierta' : 'Cerrada' }}
                        </span>
                      </div>
                      <small class="text-muted"><i class="fal fa-calendar me-1"></i>{{ formatDate(s.OpenedAt) }}</small>
                      <small v-if="isStaleOpen(s)" class="text-warning">
                        <i class="fal fa-exclamation-triangle me-1"></i>Abierta hace {{ daysOpen(s) }} día{{ daysOpen(s) === 1 ? '' : 's' }}
                      </small>
                      <!-- Los mismos seis importes que las columnas de la tabla en
                           escritorio, para que la tarjeta no cuente otra historia. -->
                      <div class="row g-1 text-center small">
                        <div class="col-4">
                          <div class="text-muted">Fondo</div>
                          <div class="fw-semibold">Bs. {{ formatNum(s.OpeningAmount) }}</div>
                        </div>
                        <div class="col-4">
                          <div class="text-muted">Ventas</div>
                          <div class="text-success fw-semibold">Bs. {{ formatNum(s.TotalSales) }}</div>
                        </div>
                        <div class="col-4">
                          <div class="text-muted">Gastos</div>
                          <div class="text-danger fw-semibold">Bs. {{ formatNum(s.TotalExpenses + s.TotalWithdrawals) }}</div>
                        </div>
                      </div>
                      <div class="row g-1 text-center small border-top pt-1">
                        <div class="col-4">
                          <div class="text-muted">Esperado</div>
                          <div class="fw-semibold">Bs. {{ formatNum(expectedAmount(s)) }}</div>
                        </div>
                        <div class="col-4">
                          <div class="text-muted">Declarado</div>
                          <div class="fw-semibold">
                            {{ s.DeclaredAmount !== null ? 'Bs. ' + formatNum(s.DeclaredAmount) : '—' }}
                          </div>
                        </div>
                        <div class="col-4">
                          <div class="text-muted">Diferencia</div>
                          <div :class="s.Difference === null ? 'text-muted' : s.Difference >= 0 ? 'text-success fw-semibold' : 'text-danger fw-semibold'">
                            {{ s.Difference !== null ? 'Bs. ' + formatNum(s.Difference) : '—' }}
                          </div>
                        </div>
                      </div>
                      <button class="btn btn-sm btn-outline-secondary w-100 mt-auto pt-1" @click="selectSession(s)">
                        <i class="fal fa-eye me-1"></i>Ver detalle
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>

          </div>
        </div>
      </div>
    </div>

    <!-- ══ MODAL: Detalle de Sesión ══ -->
    <div v-if="selectedSession" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,.5); z-index:3000;">
      <div class="modal-dialog modal-dialog-centered modal-xl">
        <div class="modal-content">
          <div class="modal-header py-2">
            <h6 class="modal-title fw-bold">
              <i class="fal fa-cash-register me-2"></i>Turno — {{ selectedSession.UserFullName }}
              <small class="text-muted fw-normal ms-2">{{ formatDate(selectedSession.OpenedAt) }}</small>
              <span v-if="isStaleOpen(selectedSession)" class="badge bg-warning text-dark ms-2">
                <i class="fal fa-exclamation-triangle me-1"></i>Abierta hace {{ daysOpen(selectedSession) }} día{{ daysOpen(selectedSession) === 1 ? '' : 's' }}
              </span>
            </h6>
            <button type="button" class="btn-close" @click="selectedSession = null"></button>
          </div>
          <div class="modal-body" style="max-height:80vh; overflow-y:auto;">

            <!-- Resumen arqueo -->
            <!-- Se muestran los cuatro términos que componen el esperado, y no
                 solo algunos: si falta uno (los ingresos, por ejemplo) la cuenta
                 no cierra en pantalla y la diferencia parece salida de la nada. -->
            <div class="row g-2 mb-2">
              <div class="col-6 col-md-3">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">Fondo inicial</small>
                  <strong>Bs. {{ formatNum(selectedSession.OpeningAmount) }}</strong>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">+ Ventas</small>
                  <strong class="text-success">Bs. {{ formatNum(selectedSession.TotalSales) }}</strong>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">+ Ingresos</small>
                  <strong class="text-success">Bs. {{ formatNum(selectedSession.TotalIncome) }}</strong>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">− Gastos + Retiros</small>
                  <strong class="text-danger">Bs. {{ formatNum(selectedSession.TotalExpenses + selectedSession.TotalWithdrawals) }}</strong>
                </div>
              </div>
            </div>

            <div class="row g-2 mb-3">
              <div class="col-4">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">Esperado en caja</small>
                  <strong>Bs. {{ formatNum(expectedAmount(selectedSession)) }}</strong>
                </div>
              </div>
              <div class="col-4">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">Declarado</small>
                  <strong>{{ selectedSession.DeclaredAmount !== null ? 'Bs. ' + formatNum(selectedSession.DeclaredAmount) : '—' }}</strong>
                </div>
              </div>
              <div class="col-4">
                <div class="border rounded text-center p-2"
                  :class="selectedSession.Difference === null ? '' : selectedSession.Difference >= 0 ? 'border-success' : 'border-danger'">
                  <small class="text-muted d-block">Diferencia</small>
                  <!-- Mientras la caja siga abierta no hay arqueo: un "0.00" en
                       verde se leería como que cuadra. -->
                  <strong v-if="selectedSession.Difference === null" class="text-muted">—</strong>
                  <strong v-else :class="selectedSession.Difference >= 0 ? 'text-success' : 'text-danger'">
                    Bs. {{ formatNum(selectedSession.Difference) }}
                  </strong>
                </div>
              </div>
            </div>

            <!-- Tabs -->
            <ul class="nav nav-tabs mb-3">
              <li class="nav-item">
                <button class="nav-link" :class="activeTab === 'sales' ? 'active' : ''" @click="activeTab = 'sales'">
                  <i class="fal fa-receipt me-1"></i>Ventas
                  <span v-if="sessionSales.length" class="badge bg-primary ms-1">{{ sessionSales.length }}</span>
                </button>
              </li>
              <li class="nav-item">
                <button class="nav-link" :class="activeTab === 'movements' ? 'active' : ''" @click="activeTab = 'movements'">
                  <i class="fal fa-exchange-alt me-1"></i>Movimientos
                  <span v-if="selectedSession.Movements.length" class="badge bg-secondary ms-1">{{ selectedSession.Movements.length }}</span>
                </button>
              </li>
            </ul>

            <!-- TAB: Ventas -->
            <div v-show="activeTab === 'sales'">
              <div v-if="loadingSales" class="text-center py-4 text-muted">
                <i class="fal fa-spinner fa-spin me-2"></i>Cargando ventas...
              </div>
              <div v-else-if="sessionSales.length === 0" class="text-center py-4 text-muted small">
                No hay ventas registradas en este turno.
              </div>
              <template v-else>
                <!-- Totales de ventas -->
                <div class="row g-2 mb-3">
                  <div class="col-6 col-md-3">
                    <div class="card border text-center py-2 mb-0">
                      <small class="text-muted d-block">Transacciones</small>
                      <strong>{{ sessionSales.length }}</strong>
                    </div>
                  </div>
                  <div class="col-6 col-md-3">
                    <div class="card border text-center py-2 mb-0">
                      <small class="text-muted d-block">Subtotal</small>
                      <strong>Bs. {{ formatNum(sessionSales.reduce((a,s)=>a+s.Subtotal,0)) }}</strong>
                    </div>
                  </div>
                  <div class="col-6 col-md-3">
                    <div class="card border text-center py-2 mb-0">
                      <small class="text-muted d-block">Descuentos</small>
                      <strong class="text-danger">Bs. {{ formatNum(sessionSales.reduce((a,s)=>a+s.TotalDiscounts,0)) }}</strong>
                    </div>
                  </div>
                  <div class="col-6 col-md-3">
                    <div class="card border border-success text-center py-2 mb-0">
                      <small class="text-muted d-block">Total neto</small>
                      <strong class="text-success">Bs. {{ formatNum(sessionSales.reduce((a,s)=>a+s.Total,0)) }}</strong>
                    </div>
                  </div>
                </div>

                <!-- Tabla de ventas con detalle expandible -->
                <div class="table-responsive">
                  <table class="table table-sm table-hover align-middle mb-0">
                    <thead class="">
                      <tr>
                        <th style="width:28px"></th>
                        <th>Fecha</th>
                        <th>Cliente</th>
                        <th>Cajero</th>
                        <th>Método pago</th>
                        <th class="text-end">Total</th>
                      </tr>
                    </thead>
                    <tbody>
                      <template v-for="sale in sessionSales" :key="sale.Id">
                        <tr @click="toggleSale(sale.Id)" style="cursor:pointer;">
                          <td class="text-center">
                            <i class="fal" :class="expandedSales.has(sale.Id) ? 'fa-chevron-down' : 'fa-chevron-right'" style="font-size:.7rem;"></i>
                          </td>
                          <td><small>{{ formatDate(sale.SaleDate) }}</small></td>
                          <td class="fw-semibold small">{{ sale.CustomerName }}</td>
                          <td><small class="text-muted">{{ sale.SellerName }}</small></td>
                          <td>
                            <small v-for="p in sale.Payments" :key="p.PaymentMethodName" class="badge bg-secondary bg-opacity-10 text-secondary border me-1">
                              {{ p.PaymentMethodName }}
                            </small>
                          </td>
                          <td class="text-end fw-semibold text-success small">Bs. {{ formatNum(sale.Total) }}</td>
                        </tr>
                        <!-- Detalle de productos -->
                        <tr v-if="expandedSales.has(sale.Id)">
                          <td colspan="6" class="p-0">
                            <table class="table table-sm mb-0 bg-body-secondary">
                              <thead>
                                <tr class="text-muted small">
                                  <th class="ps-4">Producto</th>
                                  <th class="text-center">Cant.</th>
                                  <th class="text-end">P. Unit.</th>
                                  <th class="text-end">Descuento</th>
                                  <th class="text-end">Subtotal</th>
                                </tr>
                              </thead>
                              <tbody>
                                <tr v-for="d in sale.Detail" :key="d.ProductName" class="small">
                                  <td class="ps-4">{{ d.ProductName }}</td>
                                  <td class="text-center">{{ d.Quantity }}</td>
                                  <td class="text-end">Bs. {{ formatNum(d.UnitPrice) }}</td>
                                  <td class="text-end text-danger">{{ d.LineTotalDiscounts > 0 ? 'Bs. ' + formatNum(d.LineTotalDiscounts) : '—' }}</td>
                                  <td class="text-end fw-semibold">Bs. {{ formatNum(d.LineTotal) }}</td>
                                </tr>
                              </tbody>
                            </table>
                            <div v-if="sale.HeaderDiscountAmount > 0" class="small text-danger px-3 py-1 border-top">
                              <i class="fal fa-tag me-1"></i>Descuento aplicado a toda la venta: Bs. {{ formatNum(sale.HeaderDiscountAmount) }}
                            </div>
                          </td>
                        </tr>
                      </template>
                    </tbody>
                  </table>
                </div>
              </template>
            </div>

            <!-- TAB: Movimientos -->
            <div v-show="activeTab === 'movements'">
              <div v-if="selectedSession.Movements.length === 0" class="text-center py-3 text-muted small">
                Sin movimientos en este turno.
              </div>
              <table v-else class="table table-sm table-hover align-middle mb-0">
                <thead class="">
                  <tr>
                    <th>Tipo</th>
                    <th>Descripción</th>
                    <th class="text-end">Monto</th>
                    <th>Fecha</th>
                    <th>Registrado por</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="m in selectedSession.Movements" :key="m.Id">
                    <td>
                      <span class="badge"
                        :class="m.MovementType === 'expense' ? 'bg-danger-subtle text-danger' : m.MovementType === 'withdrawal' ? 'bg-warning-subtle text-warning' : 'bg-success-subtle text-success'">
                        {{ movementLabel(m.MovementType) }}
                      </span>
                    </td>
                    <td><small>{{ m.Description }}</small></td>
                    <td class="text-end small">Bs. {{ formatNum(m.Amount) }}</td>
                    <td><small class="text-muted">{{ formatDate(m.Created) }}</small></td>
                    <td><small class="text-muted">{{ m.CreatedByName || '—' }}</small></td>
                  </tr>
                </tbody>
              </table>
              <div v-if="selectedSession.Notes" class="mt-3 alert alert-secondary py-2 small">
                <strong>Observaciones:</strong> {{ selectedSession.Notes }}
              </div>
            </div>

          </div>
          <div class="modal-footer py-2 d-flex justify-content-between">
            <button v-if="activeTab === 'sales' && sessionSales.length" class="btn btn-success btn-sm" @click="exportSessionSales">
              <i class="fal fa-file-excel me-1"></i>Exportar Excel
            </button>
            <span v-else></span>
            <button class="btn btn-outline-secondary btn-sm" @click="selectedSession = null">Cerrar</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import type { CashSession, SessionSale } from '@/modules/inventory/models/cashSession.model';
import { MovementTypeLabels } from '@/modules/inventory/models/cashMovement.model';
import useCashSession from '@/modules/inventory/composables/useCashSession';
import { exportToExcel } from '@/utils/excelHelper';
import { todayIso, toIsoDate } from '@/utils/dateHelper';

const { getSessions, getSessionById, getSessionSales } = useCashSession();

const sessions = ref<CashSession[]>([]);
const selectedSession = ref<CashSession | null>(null);
const activeQuick = ref<string | null>('today');
const activeTab = ref<'sales' | 'movements'>('sales');
const sessionSales = ref<SessionSale[]>([]);
const loadingSales = ref(false);
const expandedSales = ref<Set<string>>(new Set());

const today = todayIso;
const daysAgo = (n: number) => {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return toIsoDate(d);
};

const filtro = ref({ dateFrom: today(), dateTo: today() });

const quickFilters = [
  { key: 'today',   label: 'Hoy',           icon: 'fal fa-calendar-day' },
  { key: 'week',    label: 'Esta semana',    icon: 'fal fa-calendar-week' },
  { key: 'month',   label: 'Este mes',       icon: 'fal fa-calendar-alt' },
];

const applyQuick = (key: string) => {
  activeQuick.value = key;
  if (key === 'today') {
    filtro.value = { dateFrom: today(), dateTo: today() };
  } else if (key === 'week') {
    filtro.value = { dateFrom: daysAgo(6), dateTo: today() };
  } else if (key === 'month') {
    filtro.value = { dateFrom: daysAgo(29), dateTo: today() };
  }
  loadSessions();
};

const loadSessions = async () => {
  const resp = await getSessions(filtro.value.dateFrom, filtro.value.dateTo);
  sessions.value = resp.ok ? (resp.Data ?? []) : [];
};

const selectSession = async (s: CashSession) => {
  selectedSession.value = s;
  activeTab.value = 'sales';
  expandedSales.value = new Set();
  sessionSales.value = [];
  loadingSales.value = true;

  const [salesResp, sessionResp] = await Promise.all([
    getSessionSales(s.Id),
    getSessionById(s.Id),
  ]);

  sessionSales.value = salesResp.ok ? (salesResp.Data ?? []) : [];
  if (sessionResp.ok && sessionResp.Data) {
    selectedSession.value = sessionResp.Data;
  }

  loadingSales.value = false;
};

const toggleSale = (id: string) => {
  if (expandedSales.value.has(id)) {
    expandedSales.value.delete(id);
  } else {
    expandedSales.value.add(id);
  }
  expandedSales.value = new Set(expandedSales.value);
};

const exportSessionSales = () => {
  if (!selectedSession.value) return;
  const rows: object[] = [];
  for (const sale of sessionSales.value) {
    for (const d of sale.Detail) {
      rows.push({
        Fecha: formatDate(sale.SaleDate),
        Cliente: sale.CustomerName,
        Cajero: sale.SellerName,
        Producto: d.ProductName,
        Cantidad: d.Quantity,
        PrecioUnitario: d.UnitPrice,
        Descuento: d.LineTotalDiscounts,
        Subtotal: d.LineTotal,
        TotalVenta: sale.Total,
        MetodoPago: sale.Payments.map(p => p.PaymentMethodName).join(' / '),
      });
    }
  }
  const cajero = selectedSession.value.UserFullName.replace(/\s+/g, '_');
  const fecha = selectedSession.value.OpenedAt.slice(0, 10);
  exportToExcel(rows, `turno_${cajero}_${fecha}.xlsx`);
};

const formatNum = (val: number) =>
  (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const formatDate = (val: string) =>
  new Date(val).toLocaleString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false });

// Turnos pensados para durar un día; uno abierto por más tiempo suele ser un
// olvido (nadie lo cerró) más que un caso de uso real — se avisa en vez de
// cerrarlo solo, porque un cajero puede legítimamente cruzar medianoche.
const STALE_OPEN_DAYS = 1;

const daysOpen = (s: CashSession): number =>
  Math.floor((Date.now() - new Date(s.OpenedAt).getTime()) / 86_400_000);

const isStaleOpen = (s: CashSession): boolean => s.IsOpen && daysOpen(s) >= STALE_OPEN_DAYS;

/**
 * Lo que debería haber en la caja. Es la misma fórmula que aplica el backend al
 * cerrar (CashSessionApplication.CloseSession).
 *
 * En una sesión ya cerrada se muestra el valor que quedó guardado, no uno
 * recalculado: el arqueo es un hecho histórico y recalcularlo lo haría cambiar
 * si algún movimiento se editara después.
 */
const expectedAmount = (s: CashSession): number =>
  s.ExpectedAmount !== null
    ? s.ExpectedAmount
    : s.OpeningAmount + s.TotalSales + s.TotalIncome - s.TotalExpenses - s.TotalWithdrawals;

const movementLabel = (type: string) => MovementTypeLabels[type] ?? type;

onMounted(() => loadSessions());
</script>
