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
                  <template v-for="(line, i) in delivery.Detail" :key="line.ProductId">
                    <tr :class="{ 'opacity-50': line.PendingQuantity === 0 }">
                      <td class="fw-semibold">
                        {{ line.ProductName }}
                        <span v-if="usesLot(line)" class="badge bg-info-subtle text-info-emphasis border border-info-subtle ms-1">
                          <i class="fal fa-layer-group me-1"></i>Lote
                        </span>
                        <span v-else-if="usesSerial(line)" class="badge bg-info-subtle text-info-emphasis border border-info-subtle ms-1">
                          <i class="fal fa-barcode me-1"></i>Series
                        </span>
                      </td>
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

                    <!--
                      Los campos del lote van en una sub-fila y no en columnas
                      propias: solo los necesita una minoría de productos, y
                      sumarlas a una tabla de siete columnas la vuelve ilegible
                      para todos los demás.
                    -->
                    <!--
                      Series: una por unidad. Va en un textarea y no en N campos
                      porque estos códigos se leen con lector, y un lector emite
                      Enter al final de cada lectura: así se cargan de corrido
                      sin tocar el teclado.
                    -->
                    <tr v-if="usesSerial(line) && line.PendingQuantity > 0" class="lot-row">
                      <td class="border-0"></td>
                      <td colspan="6" class="border-0 pt-0">
                        <div class="row g-2">
                          <div class="col-12 col-md-5">
                            <label class="form-label small text-muted mb-1">
                              Números de serie <span class="text-danger">*</span>
                              <span :class="serialCountClass(line)">
                                {{ serialCount(line) }} de {{ line.DeliveryQuantity }}
                              </span>
                            </label>
                            <textarea
                              class="form-control form-control-sm font-monospace"
                              :class="{ 'is-invalid': serialErrors[i] }"
                              rows="3"
                              placeholder="Uno por línea, o léalos con el lector"
                              :value="line.SerialNumbers.join('\n')"
                              :disabled="isSaved"
                              @input="onSerialsInput(line, i, $event)"
                            ></textarea>
                          </div>
                          <div class="col-12 col-md-3">
                            <label class="form-label small text-muted mb-1">Vencimiento</label>
                            <input
                              type="date"
                              class="form-control form-control-sm"
                              v-model="line.ExpiryDate"
                              :disabled="isSaved"
                            />
                            <small v-if="isExpired(line)" class="text-danger">
                              <i class="fal fa-exclamation-triangle me-1"></i>Ya vencido
                            </small>
                          </div>
                          <div class="col-12 col-md-4 d-flex align-items-end">
                            <small class="text-muted">
                              <i class="fal fa-info-circle me-1"></i>
                              Un número por unidad. Si la cantidad cambia, la lista tiene que
                              acompañarla.
                            </small>
                          </div>
                        </div>
                      </td>
                    </tr>

                    <tr v-if="usesLot(line) && line.PendingQuantity > 0" class="lot-row">
                      <td class="border-0"></td>
                      <td colspan="6" class="border-0 pt-0">
                        <div class="row g-2">
                          <div class="col-12 col-md-4">
                            <label class="form-label small text-muted mb-1">
                              Lote recibido <span class="text-danger">*</span>
                            </label>
                            <input
                              type="text"
                              class="form-control form-control-sm"
                              :class="{ 'is-invalid': lotErrors[i] }"
                              maxlength="50"
                              placeholder="Código impreso en la caja"
                              v-model.trim="line.LotCode"
                              :disabled="isSaved"
                              @input="lotErrors[i] = false"
                            />
                          </div>
                          <div class="col-12 col-md-4">
                            <label class="form-label small text-muted mb-1">Vencimiento</label>
                            <input
                              type="date"
                              class="form-control form-control-sm"
                              v-model="line.ExpiryDate"
                              :disabled="isSaved"
                            />
                            <small v-if="isExpired(line)" class="text-danger">
                              <i class="fal fa-exclamation-triangle me-1"></i>Ya vencido
                            </small>
                          </div>
                          <div class="col-12 col-md-4 d-flex align-items-end">
                            <small class="text-muted">
                              <i class="fal fa-info-circle me-1"></i>
                              Un lote por recepción. Si llegaron varios, registre este y
                              repita la recepción con el saldo.
                            </small>
                          </div>
                        </div>
                      </td>
                    </tr>
                  </template>
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
import { todayIso } from '@/utils/dateHelper';

const router = useRouter();
const route = useRoute();
const { getPurchaseById, receivePurchase, closePurchase } = usePurchase();

const purchase = ref(new Purchase());
const delivery = ref(new PurchaseDelivery());
const isSaved = ref(false);
const lineErrors = ref<boolean[]>([]);
const lotErrors = ref<boolean[]>([]);
const serialErrors = ref<boolean[]>([]);

/**
 * En horario LOCAL: con UTC, a partir de las 20:00 el servidor rechazaba la
 * recepción por futura y no se podía recepcionar nada hasta la medianoche.
 */
const today = todayIso();

const formatDate = (val: string | Date): string => {
  if (!val) return '—';
  return new Date(val).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const formatCurrency = (val: number): string =>
  (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/**
 * Mismo criterio que en `PurchasesAdminView`: las variantes `subtle` +
 * `emphasis` son las únicas que el bloque `[data-bs-theme='dark']` redefine,
 * así que el texto conserva contraste en ambos temas.
 */
const statusBadge = (id: number) => {
  const base = 'badge border';
  if (id === PURCHASE_STATUS.TOTALLY_RECEIVED)
    return `${base} bg-success-subtle text-success-emphasis border-success-subtle`;
  if (id === PURCHASE_STATUS.PARTIALLY_RECEIVED)
    return `${base} bg-warning-subtle text-warning-emphasis border-warning-subtle`;
  if (id === PURCHASE_STATUS.CANCELLED)
    return `${base} bg-danger-subtle text-danger-emphasis border-danger-subtle`;
  if (id === PURCHASE_STATUS.CLOSED)
    return `${base} bg-secondary-subtle text-secondary-emphasis border-secondary-subtle`;
  return `${base} bg-info-subtle text-info-emphasis border-info-subtle`;
};

const statusLabel = (id: number) => {
  if (id === PURCHASE_STATUS.TOTALLY_RECEIVED) return 'Tot. Recibido';
  if (id === PURCHASE_STATUS.PARTIALLY_RECEIVED) return 'Parc. Recibido';
  if (id === PURCHASE_STATUS.CANCELLED) return 'Cancelado';
  if (id === PURCHASE_STATUS.CLOSED) return 'Cerrado';
  return 'Solicitado';
};

/**
 * El servidor rechaza la recepción sin lote cuando el producto lo lleva, así que
 * la fila lo pide antes de intentar el viaje. `serial` todavía no se recibe por
 * esta pantalla: se trata como los demás hasta que exista su propia captura.
 */
const usesLot = (line: PurchaseDeliveryDetail) => line.TrackingMode === 'lot';

/** Una unidad, un número de serie: la lista tiene que igualar a la cantidad. */
const usesSerial = (line: PurchaseDeliveryDetail) => line.TrackingMode === 'serial';

const serialCount = (line: PurchaseDeliveryDetail) => line.SerialNumbers.length;

const serialCountClass = (line: PurchaseDeliveryDetail) =>
  serialCount(line) === line.DeliveryQuantity ? 'badge bg-success ms-1' : 'badge bg-secondary ms-1';

/** Una serie por línea; los vacíos se descartan al escribir. */
const onSerialsInput = (line: PurchaseDeliveryDetail, index: number, event: Event) => {
  const texto = (event.target as HTMLTextAreaElement).value;
  line.SerialNumbers = texto.split('\n').map(x => x.trim()).filter(x => x.length > 0);
  serialErrors.value[index] = false;
};

/** Un vencimiento ya cumplido no bloquea, pero sí se advierte antes de confirmar. */
const isExpired = (line: PurchaseDeliveryDetail) =>
  !!line.ExpiryDate && line.ExpiryDate < today;

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
    // Se propone recibir el saldo pendiente, no el total ordenado. Se usa el
    // valor ya derivado y no el crudo: si el servidor lo omitió, la propuesta
    // quedaría en undefined mientras el tope sí tiene número.
    detail.DeliveryQuantity = detail.PendingQuantity;
    // El precio pactado es el punto de partida; se corrige si el proveedor facturó otro.
    detail.UnitPrice = d.OrderUnitPrice ?? 0;
    // Una orden vieja puede venir sin el campo; sin seguimiento es el caso simple.
    detail.TrackingMode = d.TrackingMode ?? 'none';
    return detail;
  });

  lineErrors.value = delivery.value.Detail.map(() => false);
  lotErrors.value = delivery.value.Detail.map(() => false);
  serialErrors.value = delivery.value.Detail.map(() => false);
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

  // Sin lote el servidor rechaza la línea y se pierde toda la recepción, no solo
  // esa fila. Se corta acá para que el usuario no reescriba lo demás.
  const sinLote = delivery.value.Detail.findIndex(
    d => d.DeliveryQuantity > 0 && usesLot(d) && !d.LotCode
  );
  if (sinLote >= 0) {
    lotErrors.value[sinLote] = true;
    utils.showMessageModal({
      Description: `"${delivery.value.Detail[sinLote].ProductName}" se maneja por lotes: indique el lote recibido.`,
      MessageType: 'warning',
    });
    return;
  }

  // La cantidad de series tiene que coincidir con las unidades: el servidor lo
  // rechaza y se perdería toda la recepción, no solo esa fila.
  const seriesMal = delivery.value.Detail.findIndex(
    d => d.DeliveryQuantity > 0 && usesSerial(d) && d.SerialNumbers.length !== d.DeliveryQuantity
  );
  if (seriesMal >= 0) {
    const linea = delivery.value.Detail[seriesMal];
    serialErrors.value[seriesMal] = true;
    utils.showMessageModal({
      Description: `"${linea.ProductName}" se identifica por número de serie: indique ` +
        `${linea.DeliveryQuantity} número(s), hay ${linea.SerialNumbers.length}.`,
      MessageType: 'warning',
    });
    return;
  }

  // Un mismo número dos veces sería la misma unidad física repetida.
  const seriesRepetidas = delivery.value.Detail.findIndex(d => {
    if (!usesSerial(d) || d.DeliveryQuantity === 0) return false;
    const normalizadas = d.SerialNumbers.map(x => x.toUpperCase());
    return new Set(normalizadas).size !== normalizadas.length;
  });
  if (seriesRepetidas >= 0) {
    serialErrors.value[seriesRepetidas] = true;
    utils.showMessageModal({
      Description: `Hay números de serie repetidos en "${delivery.value.Detail[seriesRepetidas].ProductName}".`,
      MessageType: 'warning',
    });
    return;
  }

  // Recibir mercadería vencida es casi siempre un error de tipeo, pero a veces es
  // real (una devolución al proveedor pendiente). Se advierte, no se prohíbe.
  const vencidos = delivery.value.Detail.filter(d => d.DeliveryQuantity > 0 && isExpired(d));
  if (vencidos.length > 0) {
    const nombres = vencidos.map(d => d.ProductName).join(', ');
    if (!await utils.showMessageQuestion(
      `El vencimiento indicado ya pasó en: ${nombres}. ¿Registrar la recepción de todos modos?`
    )) return;
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
