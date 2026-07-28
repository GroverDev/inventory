<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de Compras</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">Recepcionar Orden</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Recepcionar <span class="fw-300"><i>Orden de Compra</i></span></h2>
        </div>
        <div class="panel-container show">

          <!-- Acciones -->
          <div class="panel-content pt-0">
            <div class="row align-items-center">
              <div class="col-8">
                <div class="d-none d-md-flex gap-2">
                  <button type="button" class="btn btn-sm btn-success"
                    :disabled="isSaved || !canSubmit" @click="saveReceive">
                    <span class="fal fa-box-check me-1"></span>Confirmar Recepción
                  </button>
                  <button v-if="canClose" type="button" class="btn btn-sm btn-outline-secondary"
                    :disabled="isSaved" @click="closeWithShortage">
                    <span class="fal fa-lock-alt me-1"></span>Cerrar con Faltante
                  </button>
                  <button type="button" class="btn btn-warning btn-sm" @click="returnPage">
                    <span class="fal fa-ban me-1"></span>Cancelar
                  </button>
                </div>
                <div class="d-md-none">
                  <div class="btn-group">
                    <button type="button" class="btn btn-success dropdown-toggle"
                      data-bs-toggle="dropdown" data-bs-display="static">Opciones</button>
                    <div class="dropdown-menu">
                      <button type="button" class="dropdown-item" :disabled="isSaved || !canSubmit" @click="saveReceive">
                        <span class="fal fa-box-check me-1"></span>Confirmar Recepción
                      </button>
                      <button v-if="canClose" type="button" class="dropdown-item" :disabled="isSaved" @click="closeWithShortage">
                        <span class="fal fa-lock-alt me-1"></span>Cerrar con Faltante
                      </button>
                      <button type="button" class="dropdown-item" @click="returnPage">
                        <span class="fal fa-ban me-1"></span>Cancelar
                      </button>
                    </div>
                  </div>
                </div>
              </div>
              <div class="col-4 text-end">
                <button type="button" class="btn btn-danger btn-sm" @click="returnPage">
                  <span class="fal fa-arrow-alt-to-left me-1"></span>Volver
                </button>
              </div>
            </div>
          </div>

          <!-- Info de la orden -->
          <div class="panel-content pt-0" v-if="purchase.Id">
            <h6 class="text-muted border-bottom pb-2 mb-3">
              <i class="fal fa-file-invoice me-1"></i> Información de la Orden
            </h6>
            <div class="row mb-3">
              <div class="col-12 col-md-4 mb-2">
                <small class="text-muted d-block">Proveedor</small>
                <strong>{{ purchase.ProviderName }}</strong>
              </div>
              <div class="col-6 col-md-2 mb-2">
                <small class="text-muted d-block">Fecha de Compra</small>
                <strong>{{ formatDate(purchase.PurchaseDate) }}</strong>
              </div>
              <div class="col-6 col-md-3 mb-2">
                <small class="text-muted d-block">Estado actual</small>
                <span :class="statusBadge(purchase.PurchaseStatusId)">
                  {{ purchase.PurchaseStatusName || statusLabel(purchase.PurchaseStatusId) }}
                </span>
              </div>
              <div class="col-12 col-md-3 mb-2">
                <label class="form-label mb-1">Fecha de Recepción <span class="text-danger">*</span></label>
                <input type="date" class="form-control form-control-sm"
                  v-model="delivery.DeliveryDate" :max="today" :disabled="isSaved" />
              </div>
            </div>

            <!-- Aviso cuando ya no queda nada por recibir -->
            <div v-if="!hasPending" class="alert alert-info py-2">
              <i class="fal fa-info-circle me-1"></i>
              Esta orden no tiene saldo pendiente de recepción.
            </div>

            <!-- Detalle de recepción -->
            <h6 class="text-muted border-bottom pb-2 mb-3">
              <i class="fal fa-boxes me-1"></i> Cantidades a Recibir
            </h6>
            <div class="table-responsive">
              <table class="table table-sm align-middle">
                <thead>
                  <tr>
                    <th>Producto</th>
                    <th class="text-center">Ordenado</th>
                    <th class="text-center">Recibido</th>
                    <th class="text-center">Pendiente</th>
                    <th class="text-center" style="width:150px">A Recibir</th>
                    <th class="text-end" style="width:150px">Precio Unit.</th>
                    <th class="text-end" style="width:130px">Subtotal</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(line, i) in delivery.Detail" :key="line.ProductId"
                    :class="{ 'opacity-50': line.PendingQuantity === 0 }">
                    <td class="fw-semibold">{{ line.ProductName }}</td>
                    <td class="text-center">{{ line.OrderedQuantity }}</td>
                    <td class="text-center">{{ line.ReceivedQuantity }}</td>
                    <td class="text-center">
                      <span :class="line.PendingQuantity > 0 ? 'badge bg-warning text-dark' : 'badge bg-success'">
                        {{ line.PendingQuantity }}
                      </span>
                    </td>
                    <td class="text-center">
                      <input
                        type="number"
                        class="form-control form-control-sm text-center"
                        :class="{ 'is-invalid': lineErrors[i] }"
                        min="0"
                        :max="line.PendingQuantity"
                        v-model.number="line.DeliveryQuantity"
                        :disabled="isSaved || line.PendingQuantity === 0"
                        @input="clampLine(i)"
                      />
                    </td>
                    <td class="text-end">
                      <input
                        type="number"
                        class="form-control form-control-sm text-end"
                        min="0"
                        step="0.01"
                        v-model.number="line.UnitPrice"
                        :disabled="isSaved || line.PendingQuantity === 0"
                      />
                    </td>
                    <td class="text-end fw-semibold">{{ formatCurrency(line.DeliveryQuantity * line.UnitPrice) }}</td>
                  </tr>
                </tbody>
                <tfoot>
                  <tr>
                    <th colspan="6" class="text-end">Total de esta recepción</th>
                    <th class="text-end">{{ formatCurrency(deliveryTotal) }}</th>
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>

          <div v-else class="panel-content text-center py-5">
            <i class="fal fa-spinner fa-spin fa-2x text-muted"></i>
          </div>

        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import utils from '@/utils/msg';
import {
  Purchase, PurchaseDelivery, PurchaseDeliveryDetail, PURCHASE_STATUS,
} from '@/modules/inventory/models/purchase.model';
import usePurchase from '@/modules/inventory/composables/usePurchase';

const router = useRouter();
const route = useRoute();
const { getPurchaseById, receivePurchase, closePurchase } = usePurchase();

const purchase = ref(new Purchase());
const delivery = ref(new PurchaseDelivery());
const isSaved = ref(false);
const lineErrors = ref<boolean[]>([]);

const today = new Date().toISOString().split('T')[0];

const formatDate = (val: string | Date): string => {
  if (!val) return '—';
  return new Date(val).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const formatCurrency = (val: number): string =>
  (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const statusBadge = (id: number) => {
  if (id === PURCHASE_STATUS.TOTALLY_RECEIVED) return 'badge bg-success';
  if (id === PURCHASE_STATUS.PARTIALLY_RECEIVED) return 'badge bg-warning text-dark';
  if (id === PURCHASE_STATUS.CANCELLED) return 'badge bg-danger';
  if (id === PURCHASE_STATUS.CLOSED) return 'badge bg-secondary';
  return 'badge bg-info text-dark';
};

const statusLabel = (id: number) => {
  if (id === PURCHASE_STATUS.TOTALLY_RECEIVED) return 'Tot. Recibido';
  if (id === PURCHASE_STATUS.PARTIALLY_RECEIVED) return 'Parc. Recibido';
  if (id === PURCHASE_STATUS.CANCELLED) return 'Cancelado';
  if (id === PURCHASE_STATUS.CLOSED) return 'Cerrado';
  return 'Solicitado';
};

const hasPending = computed(() => delivery.value.Detail.some(d => d.PendingQuantity > 0));

const deliveryTotal = computed(() =>
  delivery.value.Detail.reduce((acc, d) => acc + (d.DeliveryQuantity * d.UnitPrice), 0)
);

/** Habilita el guardado solo si hay al menos una unidad cargada. */
const canSubmit = computed(() =>
  hasPending.value && delivery.value.Detail.some(d => d.DeliveryQuantity > 0)
);

/** El cierre con faltante solo aplica sobre una orden ya parcialmente recibida. */
const canClose = computed(() => purchase.value.PurchaseStatusId === PURCHASE_STATUS.PARTIALLY_RECEIVED);

/** Uid de operación: evita que un doble envío duplique el ingreso de stock. */
const newOperationUid = (): string =>
  typeof crypto !== 'undefined' && 'randomUUID' in crypto
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(16).slice(2)}`.padEnd(36, '0');

onMounted(async () => {
  const id = route.params.id as string;
  if (!id || id === '0') { returnPage(); return; }
  await loadPurchase(id);
});

const loadPurchase = async (id: string) => {
  const { ok, Data } = await getPurchaseById(id);
  if (!ok) { returnPage(); return; }

  purchase.value = Data;
  if (Data.PurchaseDate) purchase.value.PurchaseDate = Data.PurchaseDate.toString().substring(0, 10);

  delivery.value = new PurchaseDelivery();
  delivery.value.PurchaseId = Data.Id;
  delivery.value.DeliveryDate = today;
  delivery.value.OperationUid = newOperationUid();

  delivery.value.Detail = (Data.Detail ?? []).map(d => {
    const detail = new PurchaseDeliveryDetail();
    detail.ProductId = d.ProductId;
    detail.ProductName = d.ProductName;
    detail.OrderedQuantity = Number(d.OrderedQuantity ?? 0);
    detail.ReceivedQuantity = Number(d.ReceivedQuantity ?? 0);
    // Si el backend no envía el pendiente, se deriva en lugar de quedar en
    // undefined: así la fila se comporta de forma coherente y no queda un
    // estado intermedio donde el campo se edita pero el botón nunca habilita.
    detail.PendingQuantity = Number(
      d.PendingQuantity ?? Math.max(0, detail.OrderedQuantity - detail.ReceivedQuantity)
    );
    // Se propone recibir el saldo pendiente, no el total ordenado.
    detail.DeliveryQuantity = d.PendingQuantity;
    // El precio pactado es el punto de partida; se corrige si el proveedor facturó otro.
    detail.UnitPrice = d.OrderUnitPrice ?? 0;
    return detail;
  });

  lineErrors.value = delivery.value.Detail.map(() => false);
};

/** Recorta la cantidad al pendiente disponible mientras el usuario tipea. */
const clampLine = (index: number) => {
  const line = delivery.value.Detail[index];
  const qty = Number(line.DeliveryQuantity);

  if (Number.isNaN(qty) || qty < 0) {
    line.DeliveryQuantity = 0;
  } else if (qty > line.PendingQuantity) {
    line.DeliveryQuantity = line.PendingQuantity;
  }
  lineErrors.value[index] = false;
};

const returnPage = () => router.push({ name: 'purchases-admin' });

const saveReceive = async () => {
  if (!delivery.value.DeliveryDate) {
    utils.showMessageModal({ Description: 'Ingrese la fecha de recepción.', MessageType: 'warning' });
    return;
  }
  if (delivery.value.DeliveryDate > today) {
    utils.showMessageModal({ Description: 'La fecha de recepción no puede ser futura.', MessageType: 'warning' });
    return;
  }

  // El servidor revalida contra el pendiente real; esto solo evita el viaje.
  const invalid = delivery.value.Detail.findIndex(d => d.DeliveryQuantity > d.PendingQuantity);
  if (invalid >= 0) {
    lineErrors.value[invalid] = true;
    utils.showMessageModal({
      Description: `No puede recibir más de ${delivery.value.Detail[invalid].PendingQuantity} de "${delivery.value.Detail[invalid].ProductName}".`,
      MessageType: 'warning',
    });
    return;
  }

  const lines = delivery.value.Detail.filter(d => d.DeliveryQuantity > 0);
  if (lines.length === 0) {
    utils.showMessageModal({ Description: 'Debe indicar al menos un producto con cantidad recibida.', MessageType: 'warning' });
    return;
  }

  const partial = delivery.value.Detail.some(d => d.DeliveryQuantity < d.PendingQuantity);
  const question = partial
    ? '¿Confirma esta recepción parcial? La orden quedará con saldo pendiente.'
    : '¿Confirma la recepción de esta orden?';

  if (!await utils.showMessageQuestion(question)) return;

  const { ok: saved } = await receivePurchase(purchase.value.Id, delivery.value);
  if (saved) {
    isSaved.value = true;
    await utils.showMessageModal({ Description: 'La recepción se registró correctamente.', MessageType: 'success' });
    returnPage();
  }
};

const closeWithShortage = async () => {
  const ok = await utils.showMessageQuestion(
    '¿Cerrar la orden con faltante? El saldo pendiente ya no se podrá recibir.'
  );
  if (!ok) return;

  const { ok: closed } = await closePurchase(purchase.value.Id);
  if (closed) {
    isSaved.value = true;
    await utils.showMessageModal({ Description: 'La orden se cerró con faltante.', MessageType: 'success' });
    returnPage();
  }
};
</script>

<style scoped></style>
