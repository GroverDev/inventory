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
                <thead class="table-light">
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
                      Bs. {{ formatNum(s.OpeningAmount + s.TotalSales - s.TotalExpenses - s.TotalWithdrawals + s.TotalIncome) }}
                    </td>
                    <td class="text-end small">{{ s.DeclaredAmount !== null ? 'Bs. ' + formatNum(s.DeclaredAmount) : '—' }}</td>
                    <td class="text-end small fw-semibold"
                      :class="s.Difference === null ? '' : s.Difference >= 0 ? 'text-success' : 'text-danger'">
                      {{ s.Difference !== null ? 'Bs. ' + formatNum(s.Difference) : '—' }}
                    </td>
                    <td class="text-center">
                      <span class="badge" :class="s.IsOpen ? 'bg-success' : 'bg-secondary'">
                        {{ s.IsOpen ? 'Abierta' : 'Cerrada' }}
                      </span>
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
              <div v-for="s in sessions" :key="s.Id" class="card mb-2">
                <div class="card-body py-2 px-3">
                  <div class="d-flex justify-content-between align-items-start mb-1">
                    <div>
                      <span class="fw-semibold small">{{ s.UserFullName }}</span><br>
                      <small class="text-muted">{{ formatDate(s.OpenedAt) }}</small>
                    </div>
                    <span class="badge" :class="s.IsOpen ? 'bg-success' : 'bg-secondary'">
                      {{ s.IsOpen ? 'Abierta' : 'Cerrada' }}
                    </span>
                  </div>
                  <div class="row g-1 text-center small mb-2">
                    <div class="col-4">
                      <div class="text-muted">Ventas</div>
                      <div class="text-success fw-semibold">Bs. {{ formatNum(s.TotalSales) }}</div>
                    </div>
                    <div class="col-4">
                      <div class="text-muted">Gastos</div>
                      <div class="text-danger fw-semibold">Bs. {{ formatNum(s.TotalExpenses + s.TotalWithdrawals) }}</div>
                    </div>
                    <div class="col-4">
                      <div class="text-muted">Diferencia</div>
                      <div :class="s.Difference === null ? '' : s.Difference >= 0 ? 'text-success fw-semibold' : 'text-danger fw-semibold'">
                        {{ s.Difference !== null ? 'Bs. ' + formatNum(s.Difference) : '—' }}
                      </div>
                    </div>
                  </div>
                  <button class="btn btn-sm btn-outline-secondary w-100" @click="selectSession(s)">
                    <i class="fal fa-eye me-1"></i>Ver detalle
                  </button>
                </div>
              </div>
            </div>

          </div>
        </div>
      </div>
    </div>

    <!-- ══ MODAL: Detalle de Sesión ══ -->
    <div v-if="selectedSession" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered modal-lg">
        <div class="modal-content">
          <div class="modal-header py-2">
            <h6 class="modal-title fw-bold">
              <i class="fal fa-cash-register me-2"></i>Detalle de Turno — {{ selectedSession.UserFullName }}
            </h6>
            <button type="button" class="btn-close" @click="selectedSession = null"></button>
          </div>
          <div class="modal-body">

            <!-- Resumen arqueo -->
            <div class="row g-2 mb-3">
              <div class="col-6 col-md-3">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">Fondo inicial</small>
                  <strong>Bs. {{ formatNum(selectedSession.OpeningAmount) }}</strong>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">Ventas</small>
                  <strong class="text-success">Bs. {{ formatNum(selectedSession.TotalSales) }}</strong>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">Gastos + Retiros</small>
                  <strong class="text-danger">Bs. {{ formatNum(selectedSession.TotalExpenses + selectedSession.TotalWithdrawals) }}</strong>
                </div>
              </div>
              <div class="col-6 col-md-3">
                <div class="border rounded p-2 text-center">
                  <small class="text-muted d-block">Diferencia</small>
                  <strong :class="(selectedSession.Difference ?? 0) >= 0 ? 'text-success' : 'text-danger'">
                    Bs. {{ formatNum(selectedSession.Difference ?? 0) }}
                  </strong>
                </div>
              </div>
            </div>

            <!-- Movimientos -->
            <h6 class="fw-semibold small text-muted mb-2">Movimientos registrados</h6>
            <div v-if="selectedSession.Movements.length === 0" class="text-center py-3 text-muted small">
              Sin movimientos en este turno.
            </div>
            <table v-else class="table table-sm table-hover align-middle mb-0">
              <thead class="table-light">
                <tr>
                  <th>Tipo</th>
                  <th>Descripción</th>
                  <th class="text-end">Monto</th>
                  <th>Fecha</th>
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
                </tr>
              </tbody>
            </table>

            <div v-if="selectedSession.Notes" class="mt-3 alert alert-secondary py-2 small">
              <strong>Observaciones:</strong> {{ selectedSession.Notes }}
            </div>
          </div>
          <div class="modal-footer py-2">
            <button class="btn btn-outline-secondary btn-sm" @click="selectedSession = null">Cerrar</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import type { CashSession } from '@/modules/inventory/models/cashSession.model';
import { MovementTypeLabels } from '@/modules/inventory/models/cashMovement.model';
import useCashSession from '@/modules/inventory/composables/useCashSession';

const { getSessions } = useCashSession();

const sessions = ref<CashSession[]>([]);
const selectedSession = ref<CashSession | null>(null);
const activeQuick = ref<string | null>('today');

const today = () => new Date().toISOString().slice(0, 10);
const daysAgo = (n: number) => {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return d.toISOString().slice(0, 10);
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

const selectSession = (s: CashSession) => {
  selectedSession.value = s;
};

const formatNum = (val: number) =>
  (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const formatDate = (val: string) =>
  new Date(val).toLocaleString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });

const movementLabel = (type: string) => MovementTypeLabels[type] ?? type;

onMounted(() => loadSessions());
</script>
