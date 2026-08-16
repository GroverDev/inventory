<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventarios</li>
        <li class="breadcrumb-item active" aria-current="page">Vencimientos</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Control de <span class="fw-300"><i>VENCIMIENTOS</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <!-- Filtros -->
            <div class="row align-items-end g-2 mb-3 mt-0">
              <div class="col-12 col-md-5 col-lg-4">
                <label class="form-label">Buscar producto o lote</label>
                <div class="input-group body-bg shadow-inset-2 rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="sa sa-magnifier text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="search"
                    placeholder="Nombre, SKU o lote..."
                    autocomplete="off"
                  />
                </div>
              </div>
              <div class="col-6 col-md-3 col-lg-2">
                <label class="form-label">Horizonte</label>
                <select class="form-select form-select-sm" v-model.number="dias" @change="loadExpiring">
                  <option :value="30">Próximos 30 días</option>
                  <option :value="60">Próximos 60 días</option>
                  <option :value="90">Próximos 90 días</option>
                  <option :value="180">Próximos 6 meses</option>
                  <option :value="0">Todo lo fechado</option>
                </select>
              </div>
              <div class="col-6 col-md-3 col-lg-2">
                <label class="form-label">Urgencia</label>
                <select class="form-select form-select-sm" v-model="estado">
                  <option value="">Todas</option>
                  <option value="VENCIDO">Vencido</option>
                  <option value="CRITICO">Crítico</option>
                  <option value="PROXIMO">Próximo</option>
                  <option value="VIGENTE">Vigente</option>
                </select>
              </div>
              <div class="col-12 col-md-auto ms-md-auto">
                <button
                  type="button"
                  class="btn btn-sm btn-outline-success w-100"
                  :disabled="filtered.length === 0"
                  @click="exportList"
                >
                  <span class="fal fa-file-excel me-1"></span>Exportar
                </button>
              </div>
            </div>

            <!-- Resumen: lo que hay que decidir hoy, antes de mirar la lista -->
            <div v-if="!loading && items.length > 0" class="row g-2 mb-3">
              <div class="col-6 col-lg-3" v-for="card in summary" :key="card.estado">
                <div class="card h-100 border shadow-sm" :class="card.border">
                  <div class="card-body py-2 px-3">
                    <div class="d-flex justify-content-between align-items-center">
                      <small class="text-muted text-uppercase" style="font-size:0.7rem">{{ card.label }}</small>
                      <span class="badge" :class="card.badge">{{ card.count }}</span>
                    </div>
                    <div class="fw-semibold">{{ formatCurrency(card.valor) }}</div>
                    <small class="text-muted" style="font-size:0.7rem">
                      {{ card.unidades }} unidad(es) en riesgo
                    </small>
                  </div>
                </div>
              </div>
            </div>

            <!-- Cargando -->
            <div v-if="loading" class="text-center py-5">
              <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Cargando...</span>
              </div>
              <p class="text-muted mt-2">Cargando vencimientos...</p>
            </div>

            <!--
              Que no haya nada por vencer es una buena noticia, pero también es lo
              que se ve cuando ningún producto usa lotes todavía. Se distinguen los
              dos casos: si no hay existencias fechadas en absoluto, el mensaje
              explica cómo empezar.
            -->
            <div v-else-if="items.length === 0" class="text-center py-5">
              <i class="fal fa-calendar-check fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-1">No hay existencias con vencimiento en este horizonte.</p>
              <small class="text-muted">
                El vencimiento se registra al recibir un pedido de un producto que
                lleve control por lotes. Se activa desde la ficha del producto.
              </small>
            </div>

            <div v-else-if="filtered.length === 0" class="text-center py-5">
              <i class="fal fa-filter fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted">Ningún lote coincide con el filtro.</p>
            </div>

            <template v-else>
              <div class="d-flex align-items-center justify-content-between mb-2 flex-wrap gap-2">
                <small class="text-muted">
                  <span class="fal fa-layer-group me-1"></span>
                  <strong>{{ filtered.length }}</strong> lote(s) —
                  valor en riesgo <strong>{{ formatCurrency(totalFiltrado) }}</strong>
                </small>
              </div>

              <!-- Tabla desktop -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead>
                    <tr>
                      <th>SKU</th>
                      <th>Producto</th>
                      <th>Lote</th>
                      <th class="text-center">Vence</th>
                      <th class="text-center">Días</th>
                      <th class="text-center">Cantidad</th>
                      <th class="text-end">Valor en riesgo</th>
                      <th class="text-center">Estado</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in filtered" :key="item.StockItemId">
                      <td><small class="text-muted font-monospace">{{ item.ProductCode }}</small></td>
                      <td class="fw-semibold">{{ item.ProductName }}</td>
                      <td><code class="bg-body-secondary rounded px-2 py-1 small">{{ item.LotCode || '—' }}</code></td>
                      <td class="text-center">{{ formatDate(item.ExpiryDate) }}</td>
                      <td class="text-center">{{ diasTexto(item) }}</td>
                      <td class="text-center">{{ item.Quantity }}</td>
                      <td class="text-end fw-semibold">{{ formatCurrency(item.ValorEnRiesgo) }}</td>
                      <td class="text-center">
                        <span class="badge" :class="estadoBadge(item.Estado)">{{ estadoLabel(item.Estado) }}</span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-info btn-sm me-1"
                          title="Ver historial de movimientos"
                          @click="goHistory(item)"
                        >
                          <span class="fal fa-history me-1"></span>Historial
                        </button>
                        <button
                          v-if="item.LotCode"
                          type="button"
                          class="btn btn-outline-secondary btn-sm"
                          title="A quién se le vendió este lote"
                          @click="goTraceability(item)"
                        >
                          <span class="fal fa-route"></span>
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- Cards móvil -->
              <div class="d-md-none">
                <div class="row g-3">
                  <div class="col-12 col-sm-6" v-for="item in filtered" :key="item.StockItemId">
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <code class="bg-body-secondary rounded px-2 py-1 small">{{ item.ProductCode }}</code>
                          <span class="badge rounded-pill" :class="estadoBadge(item.Estado)">
                            {{ estadoLabel(item.Estado) }}
                          </span>
                        </div>
                        <p class="fw-semibold mb-0 lh-sm">{{ item.ProductName }}</p>
                        <small class="text-muted">
                          Lote <strong>{{ item.LotCode || '—' }}</strong> ·
                          vence {{ formatDate(item.ExpiryDate) }} ({{ diasTexto(item) }})
                        </small>
                        <div class="d-flex justify-content-between align-items-end">
                          <div>
                            <span class="fw-semibold">{{ item.Quantity }}</span>
                            <div class="text-muted" style="font-size:0.7rem;">Unidades</div>
                          </div>
                          <div class="text-end">
                            <small class="fw-semibold">{{ formatCurrency(item.ValorEnRiesgo) }}</small>
                            <div class="text-muted" style="font-size:0.7rem;">En riesgo</div>
                          </div>
                        </div>
                        <button type="button" class="btn btn-sm btn-outline-info mt-auto" @click="goHistory(item)">
                          <span class="fal fa-history me-1"></span>Historial
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
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import useStockMovement from '@/modules/inventory/composables/useStockMovement';
import type { ExpiryStatus, StockExpiryResponse } from '@/modules/inventory/models/stockMovement.model';
import { exportToExcel } from '@/utils/excelHelper';
import { todayIso } from '@/utils/dateHelper';

const { getExpiring } = useStockMovement();
const router = useRouter();

const items = ref<StockExpiryResponse[]>([]);
const loading = ref(false);
const search = ref('');
const estado = ref<'' | ExpiryStatus>('');
/** Noventa días es el horizonte con que trabaja la vista del servidor por defecto. */
const dias = ref(90);

const formatDate = (val: string): string => {
  if (!val) return '—';
  return new Date(val).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const formatCurrency = (val: number): string =>
  (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const estadoLabel = (value: ExpiryStatus): string => {
  if (value === 'VENCIDO') return 'Vencido';
  if (value === 'CRITICO') return 'Crítico';
  if (value === 'PROXIMO') return 'Próximo';
  return 'Vigente';
};

/**
 * Mismo criterio de contraste que el resto de la app: `subtle` + `emphasis` son
 * las variantes que el bloque de tema oscuro redefine.
 */
const estadoBadge = (value: ExpiryStatus): string => {
  const base = 'badge border';
  if (value === 'VENCIDO') return `${base} bg-danger-subtle text-danger-emphasis border-danger-subtle`;
  if (value === 'CRITICO') return `${base} bg-warning-subtle text-warning-emphasis border-warning-subtle`;
  if (value === 'PROXIMO') return `${base} bg-info-subtle text-info-emphasis border-info-subtle`;
  return `${base} bg-success-subtle text-success-emphasis border-success-subtle`;
};

/** «Vencido hace 5 días» dice más que un −5 suelto en una columna. */
const diasTexto = (item: StockExpiryResponse): string => {
  if (item.DiasRestantes < 0) return `hace ${Math.abs(item.DiasRestantes)} d`;
  if (item.DiasRestantes === 0) return 'hoy';
  return `en ${item.DiasRestantes} d`;
};

const filtered = computed(() => {
  const texto = search.value.toLowerCase();
  return items.value.filter(item => {
    if (estado.value && item.Estado !== estado.value) return false;
    if (!texto) return true;
    return item.ProductName.toLowerCase().includes(texto)
      || item.ProductCode.toLowerCase().includes(texto)
      || (item.LotCode ?? '').toLowerCase().includes(texto);
  });
});

const totalFiltrado = computed(() =>
  filtered.value.reduce((acc, item) => acc + item.ValorEnRiesgo, 0)
);

/**
 * El resumen se calcula sobre todo lo traído, no sobre lo filtrado: es el marco
 * de referencia, y cambiaría bajo los pies del usuario al escribir en el buscador.
 */
const summary = computed(() => {
  const estados: { estado: ExpiryStatus; label: string; badge: string; border: string }[] = [
    { estado: 'VENCIDO', label: 'Vencido', badge: 'bg-danger', border: 'border-danger-subtle' },
    { estado: 'CRITICO', label: 'Crítico (30 d)', badge: 'bg-warning text-dark', border: 'border-warning-subtle' },
    { estado: 'PROXIMO', label: 'Próximo (90 d)', badge: 'bg-info', border: 'border-info-subtle' },
    { estado: 'VIGENTE', label: 'Vigente', badge: 'bg-success', border: 'border-success-subtle' },
  ];

  return estados.map(e => {
    const grupo = items.value.filter(i => i.Estado === e.estado);
    return {
      ...e,
      count: grupo.length,
      valor: grupo.reduce((acc, i) => acc + i.ValorEnRiesgo, 0),
      unidades: grupo.reduce((acc, i) => acc + i.Quantity, 0),
    };
  });
});

onMounted(loadExpiring);

async function loadExpiring() {
  loading.value = true;
  try {
    const { ok, Data } = await getExpiring(dias.value);
    items.value = ok ? Data : [];
  } finally {
    loading.value = false;
  }
}

const goHistory = (item: StockExpiryResponse) =>
  router.push({ name: 'stock-history', params: { id: item.ProductId } });

/** Desde acá se llega con el lote ya cargado; la pantalla también acepta buscarlo. */
const goTraceability = (item: StockExpiryResponse) =>
  router.push({ name: 'stock-traceability', query: { lote: item.LotCode } });

/** Se exporta lo que el usuario está viendo, con los encabezados de la pantalla. */
const exportList = () => {
  const filas = filtered.value.map(item => ({
    SKU: item.ProductCode,
    Producto: item.ProductName,
    Lote: item.LotCode,
    Vence: formatDate(item.ExpiryDate),
    'Días restantes': item.DiasRestantes,
    Cantidad: item.Quantity,
    'Valor en riesgo': item.ValorEnRiesgo,
    Estado: estadoLabel(item.Estado),
  }));
  // Local: con UTC el archivo sale fechado mañana si se exporta de noche.
  const hoy = todayIso();
  exportToExcel(filas, `vencimientos_${hoy}.xlsx`);
};
</script>

<style scoped></style>
