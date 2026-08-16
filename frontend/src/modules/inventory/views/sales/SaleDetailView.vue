<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de Ventas</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">Detalle de Venta</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Detalle de <span class="fw-300"><i>Venta</i></span></h2>
          <div class="panel-toolbar" v-if="sale.Id">
            <button
              v-if="sale.IsActive && sale.Detail.length > 0"
              type="button" class="btn btn-warning btn-sm me-2"
              @click="openReturnModal"
            >
              <i class="fal fa-undo me-1"></i>Registrar Devolución
            </button>
            <button type="button" class="btn btn-secondary btn-sm" @click="returnPage">
              <i class="fal fa-arrow-alt-to-left me-1"></i>Volver
            </button>
          </div>
        </div>

        <div class="panel-container show">

          <!-- Cargando -->
          <div v-if="!sale.Id" class="panel-content text-center py-5">
            <i class="fal fa-spinner fa-spin fa-2x text-muted"></i>
            <p class="text-muted mt-2">Cargando...</p>
          </div>

          <template v-else>

            <!-- ① Cabecera: cliente · fecha · estado -->
            <div class="panel-content pb-2">
              <div class="d-flex flex-wrap align-items-center gap-3">
                <div>
                  <div class="info-label">Cliente</div>
                  <div class="fw-semibold">{{ sale.CustomerName || '—' }}</div>
                </div>
                <div class="vr d-none d-md-block"></div>
                <div>
                  <div class="info-label">Fecha de venta</div>
                  <div>{{ formatDate(sale.SaleDate) }}</div>
                </div>
                <div class="vr d-none d-md-block"></div>
                <div>
                  <div class="info-label">Estado</div>
                  <span :class="statusBadgeClass">{{ statusLabel }}</span>
                </div>
              </div>
            </div>

            <!-- ② Resumen de importes -->
            <div class="panel-content py-2">
              <div class="row g-2">
                <!-- Subtotal -->
                <div class="col-6 col-md-2">
                  <div class="summary-card">
                    <div class="summary-label">Subtotal</div>
                    <div class="summary-value">{{ formatCurrency(sale.Subtotal) }}</div>
                  </div>
                </div>

                <!-- Descuento por línea -->
                <div v-if="totalLineDiscounts > 0" class="col-6 col-md-2">
                  <div class="summary-card">
                    <div class="summary-label">Desc. por línea</div>
                    <div class="summary-value text-success">− {{ formatCurrency(totalLineDiscounts) }}</div>
                  </div>
                </div>

                <!-- Descuento global -->
                <div v-if="sale.HeaderDiscountAmount > 0" class="col-6 col-md-2">
                  <div class="summary-card">
                    <div class="summary-label">Desc. global</div>
                    <div class="summary-value text-success">− {{ formatCurrency(sale.HeaderDiscountAmount) }}</div>
                  </div>
                </div>

                <!-- Total venta -->
                <div class="col-6 col-md-2">
                  <div class="summary-card summary-card--primary">
                    <div class="summary-label">Total venta</div>
                    <div class="summary-value">{{ formatCurrency(sale.Total) }}</div>
                  </div>
                </div>

                <!-- Devuelto + Neto (si hay devoluciones) -->
                <template v-if="hasReturns">
                  <div class="col-6 col-md-2">
                    <div class="summary-card summary-card--warning">
                      <div class="summary-label">Devuelto</div>
                      <div class="summary-value">− {{ formatCurrency(totalReturned) }}</div>
                    </div>
                  </div>
                  <div class="col-6 col-md-2">
                    <div class="summary-card summary-card--success">
                      <div class="summary-label">Neto final</div>
                      <div class="summary-value">{{ formatCurrency(netTotal) }}</div>
                    </div>
                  </div>
                </template>
              </div>
            </div>

            <!-- ③ Cobro -->
            <div class="panel-content py-2" v-if="sale.Payments && sale.Payments.length > 0">
              <h6 class="section-title"><i class="fal fa-cash-register me-1"></i> Cobro</h6>
              <div class="d-flex flex-wrap gap-2">
                <div v-for="(p, i) in sale.Payments" :key="i" class="payment-chip">
                  <i :class="p.IconCss || 'fal fa-money-bill'" class="me-1 text-primary"></i>
                  <span class="fw-semibold">{{ p.PaymentMethodName || 'Pago' }}</span>
                  <span class="ms-2 text-muted small">{{ formatCurrency(p.AmountGiven) }}</span>
                  <span v-if="p.AmountReturned > 0" class="ms-1 text-success small">
                    · vuelto {{ formatCurrency(p.AmountReturned) }}
                  </span>
                </div>
              </div>
            </div>

            <!-- ④ Tabla unificada de productos -->
            <div class="panel-content py-2">
              <h6 class="section-title"><i class="fal fa-list me-1"></i> Productos</h6>

              <!-- Desktop -->
              <div class="d-none d-md-block">
                <table class="table table-sm align-middle mb-0">
                  <thead>
                    <tr>
                      <th>Producto</th>
                      <th class="text-end">P. Unit.</th>
                      <th class="text-center">Vendido</th>
                      <th class="text-center" v-if="hasReturns">Devuelto</th>
                      <th class="text-center" v-if="hasReturns">Neto</th>
                      <th class="text-end" v-if="hasLineDiscounts">Subtotal</th>
                      <th class="text-end" v-if="hasLineDiscounts">Descuento</th>
                      <th class="text-end">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(line, i) in enrichedLines" :key="i"
                      :class="lineRowClass(line)"
                      :data-bs-theme="(line.Net === 0 || line.Returned > 0) ? 'light' : undefined">
                      <td>
                        <span :class="line.Net === 0 ? 'text-decoration-line-through text-muted' : 'fw-semibold'">
                          {{ line.ProductName }}
                        </span>
                        <span v-if="line.Net === 0" class="badge bg-secondary ms-2" style="font-size:.65rem">
                          Devuelto
                        </span>
                        <!--
                          El lote va debajo del nombre y no en una columna
                          propia: solo lo tienen los productos con seguimiento,
                          y esta tabla ya suma y resta columnas según haya
                          devoluciones o descuentos.
                        -->
                        <small v-if="line.LotCode" class="d-block text-muted" style="font-size:.72rem">
                          <i class="fal fa-layer-group me-1"></i>Lote {{ line.LotCode }}
                          <span v-if="line.ExpiryDate"> · vence {{ formatDate(line.ExpiryDate) }}</span>
                        </small>
                        <small v-else-if="line.SerialNumber" class="d-block text-muted" style="font-size:.72rem">
                          <i class="fal fa-barcode me-1"></i>Serie {{ line.SerialNumber }}
                        </small>
                      </td>
                      <td class="text-end text-muted small">{{ formatCurrency(line.UnitPrice) }}</td>
                      <td class="text-center">{{ line.Quantity }}</td>
                      <td class="text-center" v-if="hasReturns">
                        <span v-if="line.Returned > 0" class="fw-semibold text-warning">
                          − {{ line.Returned }}
                        </span>
                        <span v-else class="text-muted">—</span>
                      </td>
                      <td class="text-center" v-if="hasReturns">
                        <span :class="line.Net === 0 ? 'text-muted' : 'fw-semibold'">{{ line.Net }}</span>
                      </td>
                      <td class="text-end text-muted small" v-if="hasLineDiscounts">
                        {{ formatCurrency(line.LineSubtotal) }}
                      </td>
                      <td class="text-end" v-if="hasLineDiscounts">
                        <span v-if="line.LineTotalDiscounts > 0" class="text-success fw-semibold">
                          − {{ formatCurrency(line.LineTotalDiscounts) }}
                        </span>
                        <span v-else class="text-muted">—</span>
                      </td>
                      <td class="text-end">
                        <span :class="line.Net === 0 ? 'text-decoration-line-through text-muted' : 'fw-semibold'">
                          {{ formatCurrency(line.LineTotal) }}
                        </span>
                        <span v-if="line.Returned > 0 && line.Net > 0" class="d-block text-muted" style="font-size:.75rem">
                          neto {{ formatCurrency(line.LineNet) }}
                        </span>
                      </td>
                    </tr>
                  </tbody>
                  <tfoot>
                    <!-- Subtotal tras descuentos por línea (solo si hay desc. global) -->
                    <tr v-if="sale.HeaderDiscountAmount > 0">
                      <td :colspan="tableColspan" class="text-end text-muted small">Subtotal (tras desc. por línea)</td>
                      <td class="text-end text-muted small">{{ formatCurrency(sale.Total + sale.HeaderDiscountAmount) }}</td>
                    </tr>
                    <!-- Descuento global -->
                    <tr v-if="sale.HeaderDiscountAmount > 0">
                      <td :colspan="tableColspan" class="text-end text-success small">
                        <i class="fal fa-tag me-1"></i>Desc. global
                      </td>
                      <td class="text-end text-success fw-semibold small">− {{ formatCurrency(sale.HeaderDiscountAmount) }}</td>
                    </tr>
                    <!-- Total final -->
                    <tr class="fw-bold border-top">
                      <td :colspan="tableColspan" class="text-end">TOTAL</td>
                      <td class="text-end">{{ formatCurrency(hasReturns ? netTotal : sale.Total) }}</td>
                    </tr>
                  </tfoot>
                </table>
              </div>

              <!-- Móvil: cards -->
              <div class="d-md-none">
                <div class="row g-2">
                  <div class="col-12" v-for="(line, i) in enrichedLines" :key="i">
                    <div class="card" :class="line.Net === 0 ? 'opacity-50' : ''">
                      <div class="card-body py-2">
                        <div class="d-flex justify-content-between align-items-start">
                          <span :class="line.Net === 0 ? 'text-decoration-line-through text-muted' : 'fw-semibold'">
                            {{ line.ProductName }}
                          </span>
                          <span class="fw-bold ms-2">{{ formatCurrency(line.LineNet) }}</span>
                        </div>
                        <small class="text-muted">
                          {{ line.Net }} × {{ formatCurrency(line.UnitPrice) }}
                          <span v-if="line.Returned > 0" class="text-warning ms-1">
                            · {{ line.Returned }} devuelto{{ line.Returned > 1 ? 's' : '' }}
                          </span>
                        </small>
                        <small v-if="line.LotCode" class="d-block text-muted" style="font-size:.72rem">
                          <i class="fal fa-layer-group me-1"></i>Lote {{ line.LotCode }}
                          <span v-if="line.ExpiryDate"> · vence {{ formatDate(line.ExpiryDate) }}</span>
                        </small>
                        <small v-else-if="line.SerialNumber" class="d-block text-muted" style="font-size:.72rem">
                          <i class="fal fa-barcode me-1"></i>Serie {{ line.SerialNumber }}
                        </small>
                        <div v-if="line.LineTotalDiscounts > 0" class="d-flex justify-content-between mt-1">
                          <small class="text-muted">Subtotal</small>
                          <small class="text-muted">{{ formatCurrency(line.LineSubtotal) }}</small>
                        </div>
                        <div v-if="line.LineTotalDiscounts > 0" class="d-flex justify-content-between">
                          <small class="text-success"><i class="fal fa-tag me-1"></i>Descuento</small>
                          <small class="text-success fw-semibold">− {{ formatCurrency(line.LineTotalDiscounts) }}</small>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- ⑤ Historial de devoluciones (acordeón colapsable) -->
            <div class="panel-content py-2" v-if="hasReturns">
              <div
                class="d-flex align-items-center justify-content-between mb-2 history-toggle"
                @click="showReturnHistory = !showReturnHistory"
              >
                <h6 class="section-title mb-0">
                  <i class="fal fa-history me-1 text-warning"></i>
                  Historial de devoluciones
                  <span class="badge bg-warning text-dark ms-1">{{ sale.Returns.length }}</span>
                </h6>
                <i :class="showReturnHistory ? 'fal fa-chevron-up' : 'fal fa-chevron-down'" class="text-muted small"></i>
              </div>

              <div v-if="showReturnHistory" class="mt-2">
                <div v-for="(ret, ri) in sale.Returns" :key="ri" class="return-history-item mb-2">
                  <div class="d-flex align-items-center gap-2 mb-1">
                    <span class="badge" :class="ret.IsFullReturn ? 'bg-danger' : 'bg-warning text-dark'">
                      {{ ret.IsFullReturn ? 'Total' : 'Parcial' }}
                    </span>
                    <small class="text-muted">{{ formatDate(ret.ReturnDate) }}</small>
                    <small v-if="ret.Reason" class="text-muted fst-italic">— {{ ret.Reason }}</small>
                    <span class="ms-auto fw-semibold text-danger small">− {{ formatCurrency(ret.TotalReturned) }}</span>
                  </div>
                  <div class="d-flex flex-wrap gap-1 ms-2">
                    <span v-for="(d, di) in ret.Detail" :key="di" class="return-chip">
                      {{ d.ProductName }} · {{ d.QuantityReturned }} uds.
                    </span>
                  </div>
                </div>
              </div>
            </div>

          </template>
        </div>
      </div>
    </div>
  </div>

  <!-- Modal de Devolución (teleportado al body) -->
  <Teleport to="body">
    <div v-if="showReturnModal" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,.5); z-index:9999">
      <div class="modal-dialog modal-dialog-centered modal-lg">
        <div class="modal-content">

          <div class="modal-header py-2">
            <h6 class="modal-title fw-bold">
              <i class="fal fa-undo me-2 text-warning"></i>Registrar Devolución
            </h6>
            <button type="button" class="btn-close" @click="closeReturnModal" :disabled="savingReturn"></button>
          </div>

          <div class="modal-body">
            <div class="mb-3">
              <label class="form-label small text-muted">Motivo (opcional)</label>
              <input
                type="text" class="form-control form-control-sm"
                v-model="returnReason"
                placeholder="Ej: Producto en mal estado, error de pedido..."
                maxlength="255"
              />
            </div>

            <div class="table-responsive">
              <table class="table table-sm align-middle mb-0">
                <thead>
                  <tr>
                    <th>Producto</th>
                    <th class="text-center">Vendido</th>
                    <th class="text-center">Ya devuelto</th>
                    <th class="text-center">Disponible</th>
                    <th class="text-center" style="width:110px">A devolver</th>
                    <th class="text-end">Subtotal</th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="(line, i) in returnLines" :key="i"
                    :class="line.QuantityReturned > 0 ? 'table-warning' : ''"
                  >
                    <td class="fw-semibold small">{{ line.ProductName }}</td>
                    <td class="text-center">{{ line.Quantity }}</td>
                    <td class="text-center text-danger">
                      {{ line.AlreadyReturned > 0 ? line.AlreadyReturned : '—' }}
                    </td>
                    <td class="text-center">
                      <span :class="line.Available === 0 ? 'text-muted' : ''">{{ line.Available }}</span>
                    </td>
                    <td class="text-center">
                      <input
                        type="number"
                        class="form-control form-control-sm text-center"
                        v-model.number="line.QuantityReturned"
                        :min="0" :max="line.Available"
                        :disabled="line.Available === 0"
                      />
                    </td>
                    <td class="text-end small">
                      {{ line.QuantityReturned > 0 ? formatCurrency(line.QuantityReturned * line.UnitPrice) : '—' }}
                    </td>
                  </tr>
                </tbody>
                <tfoot>
                  <tr class="fw-bold border-top">
                    <td colspan="5" class="text-end">Total a devolver</td>
                    <td class="text-end text-danger">{{ formatCurrency(returnTotal) }}</td>
                  </tr>
                </tfoot>
              </table>
            </div>

            <div v-if="returnTotal === 0" class="text-center text-muted small mt-3">
              <i class="fal fa-info-circle me-1"></i>
              Indica la cantidad a devolver en al menos un producto.
            </div>
          </div>

          <div class="modal-footer py-2">
            <button type="button" class="btn btn-outline-secondary btn-sm"
                    @click="closeReturnModal" :disabled="savingReturn">
              Cancelar
            </button>
            <button type="button" class="btn btn-warning btn-sm"
                    :disabled="returnTotal === 0 || savingReturn"
                    @click="confirmReturn">
              <span v-if="savingReturn" class="spinner-border spinner-border-sm me-1"></span>
              <i v-else class="fal fa-check me-1"></i>
              Confirmar devolución
            </button>
          </div>

        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { Sale } from '@/modules/inventory/models/sale.model';
import { SaleReturnRequest, SaleReturnDetailRequest } from '@/modules/inventory/models/saleReturn.model';
import useSales from '@/modules/inventory/composables/useSales';
import useSaleReturn from '@/modules/inventory/composables/useSaleReturn';
import utils from '@/utils/msg';

const router = useRouter();
const route = useRoute();
const { getSaleById } = useSales();
const { createReturn } = useSaleReturn();

const sale = ref(new Sale());
const showReturnHistory = ref(false);

// ── Formatters ─────────────────────────────────────────────
const formatDate = (val: string | Date): string => {
  if (!val) return '—';
  return new Date(val).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const formatCurrency = (val: number): string =>
  (val ?? 0).toLocaleString('es-BO', { style: 'currency', currency: 'BOB' });

// ── Computed ───────────────────────────────────────────────
const hasReturns = computed(() => (sale.value.Returns?.length ?? 0) > 0);
const hasLineDiscounts = computed(() => sale.value.Detail.some(d => d.LineTotalDiscounts > 0));
const totalLineDiscounts = computed(() =>
  sale.value.Detail.reduce((s, d) => s + d.LineTotalDiscounts, 0)
);
// Colspan del label en tfoot = todas las columnas menos la última (valor)
const tableColspan = computed(() =>
  3 + (hasReturns.value ? 2 : 0) + (hasLineDiscounts.value ? 2 : 0)
);

const totalReturned = computed(() =>
  sale.value.Returns?.reduce((s, r) => s + r.TotalReturned, 0) ?? 0
);

const netTotal = computed(() => sale.value.Total - totalReturned.value);

const statusLabel = computed(() => {
  if (!sale.value.IsActive) return 'Devuelta (total)';
  if (hasReturns.value) return 'Con devolución parcial';
  return 'Activa';
});

const statusBadgeClass = computed(() => {
  if (!sale.value.IsActive) return 'badge bg-danger';
  if (hasReturns.value) return 'badge bg-warning text-dark';
  return 'badge bg-success';
});

const enrichedLines = computed(() =>
  sale.value.Detail.map((d) => {
    const returned = (sale.value.Returns ?? [])
      .flatMap((r) => r.Detail)
      .filter((rd) => rd.SaleDetailId === d.Id)
      .reduce((s, rd) => s + rd.QuantityReturned, 0);
    const net = d.Quantity - returned;
    return {
      ...d,
      Returned: returned,
      Net: net,
      LineReturned: returned * d.UnitPrice,
      LineNet: net * d.UnitPrice,
    };
  })
);

const lineRowClass = (line: { Returned: number; Net: number }) => {
  if (line.Net === 0) return 'table-secondary';
  if (line.Returned > 0) return 'table-warning';
  return '';
};

// ── Carga de datos ─────────────────────────────────────────
const loadSale = async () => {
  const id = route.params.id as string;
  if (id) {
    const { ok, Data } = await getSaleById(id);
    if (ok && Data) sale.value = Data;
  }
};

onMounted(loadSale);

const returnPage = () => router.push({ name: 'sales-admin' });

// ── Modal de devolución ────────────────────────────────────
interface ReturnLine {
  SaleDetailId: string;
  ProductId: string;
  ProductName: string;
  Quantity: number;
  AlreadyReturned: number;
  Available: number;
  UnitPrice: number;
  QuantityReturned: number;
}

const showReturnModal = ref(false);
const savingReturn = ref(false);
const returnReason = ref('');
const returnLines = ref<ReturnLine[]>([]);

const returnTotal = computed(() =>
  returnLines.value.reduce((s, l) => s + l.QuantityReturned * l.UnitPrice, 0)
);

const openReturnModal = () => {
  returnReason.value = '';
  returnLines.value = sale.value.Detail.map((d) => {
    const alreadyReturned = (sale.value.Returns ?? [])
      .flatMap((r) => r.Detail)
      .filter((rd) => rd.SaleDetailId === d.Id)
      .reduce((s, rd) => s + rd.QuantityReturned, 0);
    return {
      SaleDetailId: d.Id,
      ProductId: d.ProductId,
      ProductName: d.ProductName,
      Quantity: d.Quantity,
      AlreadyReturned: alreadyReturned,
      Available: d.Quantity - alreadyReturned,
      UnitPrice: d.UnitPrice,
      QuantityReturned: 0,
    };
  });
  showReturnModal.value = true;
};

const closeReturnModal = () => { showReturnModal.value = false; };

const confirmReturn = async () => {
  const linesToReturn = returnLines.value.filter((l) => l.QuantityReturned > 0);
  if (linesToReturn.length === 0) return;

  const confirmed = await utils.showMessageQuestion(
    `¿Confirmar devolución de ${formatCurrency(returnTotal.value)}?`
  );
  if (!confirmed) return;

  savingReturn.value = true;
  try {
    const request = new SaleReturnRequest();
    request.SaleId = sale.value.Id;
    request.Reason = returnReason.value || null;
    request.Detail = linesToReturn.map((l) => {
      const d = new SaleReturnDetailRequest();
      d.SaleDetailId = l.SaleDetailId;
      d.ProductId = l.ProductId;
      d.QuantityReturned = l.QuantityReturned;
      d.UnitPrice = l.UnitPrice;
      return d;
    });

    const { ok, Message } = await createReturn(request);
    if (ok) {
      closeReturnModal();
      await utils.showMessageModal({ Description: 'Devolución registrada correctamente.', MessageType: 'success' });
      await loadSale();
    } else {
      utils.showMessageModal({
        Description: Message?.Description || 'No se pudo registrar la devolución.',
        MessageType: 'error',
      });
    }
  } finally {
    savingReturn.value = false;
  }
};
</script>

<style scoped>
/* Cabecera info */
.info-label {
  font-size: 0.72rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--bs-secondary-color, #6c757d);
  margin-bottom: 0.1rem;
}

/* Título de sección */
.section-title {
  font-size: 0.72rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--bs-secondary-color, #6c757d);
  border-bottom: 1px solid var(--bs-border-color);
  padding-bottom: 0.4rem;
  margin-bottom: 0.75rem;
}

/* Cards de resumen */
.summary-card {
  background: var(--bs-tertiary-bg, #f8f9fa);
  border-radius: 0.5rem;
  padding: 0.5rem 0.75rem;
  text-align: center;
  border: 1px solid var(--bs-border-color);
}
.summary-label {
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  color: var(--bs-secondary-color, #6c757d);
}
.summary-value {
  font-weight: 600;
  font-size: 0.95rem;
  margin-top: 0.1rem;
}
.summary-card--primary {
  background: var(--bs-primary);
  border-color: var(--bs-primary);
  color: #fff;
}
.summary-card--primary .summary-label { color: rgba(255,255,255,.8); }
.summary-card--warning {
  background: rgba(var(--bs-warning-rgb), 0.12);
  border-color: rgba(var(--bs-warning-rgb), 0.35);
}
.summary-card--warning .summary-value { color: #856404; }
.summary-card--success {
  background: rgba(var(--bs-success-rgb), 0.1);
  border-color: rgba(var(--bs-success-rgb), 0.35);
}
.summary-card--success .summary-value { color: var(--bs-success); }

/* Chips de método de pago */
.payment-chip {
  display: inline-flex;
  align-items: center;
  background: var(--bs-tertiary-bg, #f8f9fa);
  border: 1px solid var(--bs-border-color);
  border-radius: 2rem;
  padding: 0.25rem 0.85rem;
  font-size: 0.85rem;
}

/* Historial acordeón */
.history-toggle {
  cursor: pointer;
  user-select: none;
  padding: 0.25rem 0;
  border-radius: 0.25rem;
}
.history-toggle:hover { opacity: 0.8; }

.return-history-item {
  border-left: 3px solid var(--bs-warning);
  padding: 0.4rem 0.75rem;
  border-radius: 0 0.375rem 0.375rem 0;
  background: rgba(var(--bs-warning-rgb), 0.06);
}

.return-chip {
  display: inline-block;
  font-size: 0.72rem;
  background: var(--bs-tertiary-bg, #f8f9fa);
  border: 1px solid var(--bs-border-color);
  border-radius: 0.25rem;
  padding: 0.1rem 0.45rem;
  color: var(--bs-secondary-color, #6c757d);
}
</style>
