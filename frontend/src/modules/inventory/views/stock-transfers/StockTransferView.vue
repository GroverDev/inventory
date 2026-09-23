<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventarios</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Traspasos entre Sucursales</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">{{ title }}</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>
            {{ title }}
            <span v-if="transfer" class="badge ms-2" :class="transferStatusBadge[transfer.Status]">
              {{ transferStatusLabel[transfer.Status] }}
            </span>
          </h2>
        </div>
        <div class="panel-container show">

          <!-- Acciones -->
          <div class="panel-content pt-0">
            <div class="d-flex flex-wrap gap-2">
              <template v-if="isEditable">
                <button type="button" class="btn btn-sm btn-primary" :disabled="busy" @click="saveDraft">
                  <span class="fal fa-save me-1"></span>Guardar borrador
                </button>
                <button type="button" class="btn btn-sm btn-success" :disabled="busy || lines.length === 0" @click="send">
                  <span class="fal fa-truck me-1"></span>Enviar
                </button>
                <button v-if="transfer && canDelete" type="button" class="btn btn-sm btn-outline-danger"
                  :disabled="busy" @click="cancel">
                  <span class="fal fa-ban me-1"></span>Anular
                </button>
              </template>
              <button v-if="canReceive" type="button" class="btn btn-sm btn-success" :disabled="busy" @click="receive">
                <span class="fal fa-inbox-in me-1"></span>Confirmar recepción
              </button>
              <button type="button" class="btn btn-sm btn-danger ms-auto" @click="returnPage">
                <span class="fal fa-arrow-alt-to-left me-1"></span>Volver
              </button>
            </div>
          </div>

          <!-- Cabecera -->
          <div class="panel-content pt-0">
            <div class="row g-3">
              <div class="col-12 col-md-4">
                <label class="form-label">Origen</label>
                <input type="text" class="form-control form-control-sm" readonly
                  :value="transfer ? transfer.OriginBranchName : authStore.getUser?.BranchName" />
              </div>
              <div class="col-12 col-md-4">
                <label class="form-label">Destino <span v-if="isEditable" class="text-danger">*</span></label>
                <select v-if="isEditable" class="form-select form-select-sm" v-model="destBranchId">
                  <option value="">-- Seleccione --</option>
                  <option v-for="b in destinations" :key="b.Id" :value="b.Id">{{ b.Name }}</option>
                </select>
                <input v-else type="text" class="form-control form-control-sm" readonly :value="transfer?.DestBranchName" />
              </div>
              <div class="col-12 col-md-4">
                <label class="form-label">Notas</label>
                <input type="text" class="form-control form-control-sm" maxlength="500" v-model.trim="notes"
                  :readonly="!isEditable" placeholder="Opcional" />
              </div>
            </div>

            <div v-if="transfer" class="small text-muted mt-2">
              Creado {{ formatDateTime(transfer.Created) }} por {{ transfer.CreatedByName || '—' }}
              <span v-if="transfer.SentAt"> · Enviado {{ formatDateTime(transfer.SentAt) }} por {{ transfer.SentByName }}</span>
              <span v-if="transfer.ReceivedAt"> · Recibido {{ formatDateTime(transfer.ReceivedAt) }} por {{ transfer.ReceivedByName }}</span>
            </div>

            <div v-if="waitingElsewhere" class="alert alert-info py-2 mt-3 mb-0">
              <i class="fal fa-info-circle me-1"></i>{{ waitingElsewhere }}
            </div>
          </div>

          <!-- Borrador: armar el pedido -->
          <div v-if="isEditable" class="panel-content pt-0">
            <h6 class="text-muted border-bottom pb-2 mb-3"><i class="fal fa-boxes me-1"></i> Productos a enviar</h6>

            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-6 position-relative">
                <label class="form-label">Producto</label>
                <div class="input-group input-group-sm">
                  <input type="text" class="form-control" v-model="productSearch" placeholder="Buscar producto..."
                    autocomplete="off" @keyup.enter="searchProducts" />
                  <button class="btn btn-outline-secondary" type="button" @click="searchProducts">
                    <i class="fal fa-search"></i>
                  </button>
                </div>
                <div v-if="productResults.length > 0" class="list-group mt-1 shadow-sm position-absolute w-100" style="z-index:1000">
                  <button v-for="p in productResults" :key="p.Id" type="button"
                    class="list-group-item list-group-item-action py-1 px-2 d-flex justify-content-between"
                    @click="addProduct(p)">
                    <span>{{ p.ProductName }}</span>
                    <small :class="p.CurrentStock > 0 ? 'text-success' : 'text-danger'">stock {{ p.CurrentStock }}</small>
                  </button>
                </div>
              </div>
            </div>

            <div v-if="lines.length === 0" class="text-center py-3">
              <small class="text-muted">Busque los productos que va a enviar y agréguelos.</small>
            </div>
            <div v-else class="table-responsive">
              <table class="table table-sm align-middle mb-0">
                <thead>
                  <tr>
                    <th>Producto</th>
                    <th class="text-center">Disponible aquí</th>
                    <th class="text-center" style="width:9rem">Cantidad</th>
                    <th class="text-center">Quitar</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(l, i) in lines" :key="l.ProductId">
                    <td>
                      <div class="fw-semibold">{{ l.ProductName }}</div>
                      <small class="text-muted font-monospace">{{ l.ProductCode }}</small>
                    </td>
                    <td class="text-center">{{ l.Available }}</td>
                    <td>
                      <input type="number" min="1" class="form-control form-control-sm text-end"
                        :class="{ 'is-invalid': l.Quantity <= 0 || l.Quantity > l.Available }"
                        v-model.number="l.Quantity" />
                      <small v-if="l.Quantity > l.Available" class="text-danger">Supera lo disponible</small>
                    </td>
                    <td class="text-center">
                      <button type="button" class="btn btn-outline-danger btn-sm" @click="lines.splice(i, 1)">
                        <span class="fal fa-trash-alt"></span>
                      </button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Enviado o cerrado: lo que viajó, por lote -->
          <div v-else-if="transfer" class="panel-content pt-0">
            <h6 class="text-muted border-bottom pb-2 mb-3">
              <i class="fal fa-boxes me-1"></i>
              {{ canReceive ? 'Controle lo que llegó' : 'Detalle' }}
            </h6>
            <p v-if="canReceive" class="small text-muted">
              Si llegó menos de lo enviado, corrija la cantidad recibida: la diferencia queda registrada como
              merma de la sucursal de origen.
            </p>
            <div class="table-responsive">
              <table class="table table-sm align-middle mb-0">
                <thead>
                  <tr>
                    <th>Producto</th>
                    <th>Lote / Serie</th>
                    <th>Vence</th>
                    <th class="text-center">{{ transfer.Status === 'borrador' ? 'Cantidad' : 'Enviado' }}</th>
                    <th v-if="transfer.Status !== 'borrador' && transfer.Status !== 'anulado'" class="text-center"
                      style="width:9rem">Recibido</th>
                  </tr>
                </thead>
                <tbody>
                  <template v-for="d in transfer.Detail" :key="d.Id">
                    <!-- Borrador ajeno o anulado: todavía no hay lotes -->
                    <tr v-if="d.Lots.length === 0">
                      <td class="fw-semibold">{{ d.ProductName }}</td>
                      <td>—</td>
                      <td>—</td>
                      <td class="text-center">{{ d.Quantity }}</td>
                    </tr>
                    <tr v-for="l in d.Lots" :key="l.Id">
                      <td class="fw-semibold">{{ d.ProductName }}</td>
                      <td><small class="font-monospace">{{ l.SerialNumber || l.LotCode || 'Sin lote' }}</small></td>
                      <td><small>{{ formatDateOnly(l.ExpiryDate) }}</small></td>
                      <td class="text-center">{{ l.QuantitySent }}</td>
                      <td v-if="canReceive">
                        <input type="number" min="0" :max="l.QuantitySent" class="form-control form-control-sm text-end"
                          :class="{ 'is-invalid': !validReceived(l) }"
                          :step="l.SerialNumber ? 1 : 'any'"
                          v-model.number="received[l.Id]" />
                      </td>
                      <td v-else-if="transfer.Status === 'recibido'" class="text-center"
                        :class="{ 'text-danger fw-semibold': (l.QuantityReceived ?? 0) < l.QuantitySent }">
                        {{ l.QuantityReceived }}
                      </td>
                      <td v-else-if="transfer.Status === 'enviado'" class="text-center text-muted">en tránsito</td>
                    </tr>
                  </template>
                </tbody>
              </table>
            </div>
          </div>

        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import useStockTransfer from '@/modules/inventory/composables/useStockTransfer';
import useProduct from '@/modules/inventory/composables/useProduct';
import useBranch from '@/modules/user-account/composables/useBranch';
import type { Branch } from '@/modules/user-account/models/branch.model';
import type { Product } from '@/modules/inventory/models/product.model';
import {
  type StockTransfer, type StockTransferLot, type TransferLine,
  transferStatusBadge, transferStatusLabel,
} from '@/modules/inventory/models/stockTransfer.model';
import { useAuthStore } from '@/modules/auth/stores/auth.store';
import { usePermissions } from '@/modules/common/composables/usePermissions';
import { formatDateOnly } from '@/utils/dateHelper';
import utils from '@/utils/msg';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const { can } = usePermissions();
const { getTransfer, createDraft, updateDraft, sendTransfer, receiveTransfer, cancelTransfer } = useStockTransfer();
const { getProductsByName } = useProduct();
const { getBranches } = useBranch();

const FORM = 'stock-transfers';

const transfer = ref<StockTransfer | null>(null);
const destBranchId = ref('');
const notes = ref('');
const lines = ref<TransferLine[]>([]);
const destinations = ref<Branch[]>([]);
const productSearch = ref('');
const productResults = ref<Product[]>([]);
/** Cantidad recibida por id de lote. */
const received = ref<Record<string, number>>({});
const busy = ref(false);

const canDelete = computed(() => can(FORM, 'delete'));

/** Nuevo, o borrador propio de la sucursal de origen. */
const isEditable = computed(() => {
  if (!transfer.value) return can(FORM, 'create');
  return transfer.value.Status === 'borrador' && transfer.value.Direction === 'salida' && can(FORM, 'update');
});

const canReceive = computed(() =>
  transfer.value?.Status === 'enviado' && transfer.value.Direction === 'entrada' && can(FORM, 'update'));

const title = computed(() => transfer.value ? `Traspaso ${transfer.value.Number}` : 'Nuevo Traspaso');

/** Qué falta y dónde, cuando desde esta sucursal no hay nada que hacer. */
const waitingElsewhere = computed(() => {
  const t = transfer.value;
  if (!t) return '';
  if (t.Status === 'enviado' && t.Direction === 'salida')
    return `En tránsito: lo confirma ${t.DestBranchName} al recibirlo.`;
  if (t.Status === 'borrador' && t.Direction === 'entrada')
    return `Todavía es un borrador de ${t.OriginBranchName}: aparecerá para recibir cuando lo envíen.`;
  return '';
});

onMounted(async () => {
  const id = route.params.id as string;
  const { ok, Data } = await getBranches();
  if (ok) destinations.value = Data.filter(b => b.Id !== authStore.getUser?.BranchId);

  if (id && id !== '0') await loadTransfer(id);
});

const loadTransfer = async (id: string) => {
  const { ok, Data } = await getTransfer(id);
  if (!ok) return;

  transfer.value = Data;
  destBranchId.value = Data.DestBranchId;
  notes.value = Data.Notes;
  lines.value = Data.Detail.map(d => ({
    ProductId: d.ProductId,
    ProductCode: d.ProductCode,
    ProductName: d.ProductName,
    Available: d.AvailableAtOrigin,
    Quantity: d.Quantity,
  }));
  received.value = Object.fromEntries(
    Data.Detail.flatMap(d => d.Lots).map(l => [l.Id, l.QuantityReceived ?? l.QuantitySent]));
};

const searchProducts = async () => {
  if (!productSearch.value.trim()) return;
  const { ok, Data } = await getProductsByName(productSearch.value.trim());
  if (ok) productResults.value = Data.filter(p => p.IsActive);
};

const addProduct = (p: Product) => {
  productResults.value = [];
  productSearch.value = '';
  if (lines.value.some(l => l.ProductId === p.Id)) {
    utils.showMessageModal({ Description: 'El producto ya está en la lista: corrija su cantidad.', MessageType: 'info' });
    return;
  }
  lines.value.push({
    ProductId: p.Id,
    ProductCode: p.ProductCode,
    ProductName: p.ProductName,
    Available: p.CurrentStock,
    Quantity: 1,
  });
};

const validDraft = async (): Promise<boolean> => {
  let problema = '';
  if (!destBranchId.value) problema = 'Elija la sucursal de destino.';
  else if (lines.value.length === 0) problema = 'Agregue al menos un producto.';
  else if (lines.value.some(l => !(l.Quantity > 0))) problema = 'Las cantidades deben ser mayores a cero.';
  if (problema) await utils.showMessageModal({ Description: problema, MessageType: 'warning' });
  return !problema;
};

/** Guarda el borrador y devuelve su id, o '' si no se pudo. */
const persistDraft = async (): Promise<string> => {
  if (transfer.value) {
    const { ok } = await updateDraft(transfer.value.Id, destBranchId.value, notes.value, lines.value);
    return ok ? transfer.value.Id : '';
  }
  const { ok, Data } = await createDraft(destBranchId.value, notes.value, lines.value);
  return ok ? Data : '';
};

const saveDraft = async () => {
  if (!(await validDraft())) return;
  busy.value = true;
  const id = await persistDraft();
  busy.value = false;
  if (!id) return;

  await utils.showMessageModal({ Description: 'Borrador guardado. Envíelo cuando la mercadería salga.', MessageType: 'success' });
  if (!transfer.value) await router.replace({ name: 'stock-transfer', params: { id } });
  await loadTransfer(id);
};

const send = async () => {
  if (!(await validDraft())) return;
  const destino = destinations.value.find(b => b.Id === destBranchId.value)?.Name ?? 'el destino';
  const confirmed = await utils.showMessageQuestion(
    `¿Enviar a ${destino}? El stock sale ahora de esta sucursal y queda en tránsito hasta que lo reciban.`);
  if (!confirmed) return;

  busy.value = true;
  const id = await persistDraft();
  const ok = id ? (await sendTransfer(id)).ok : false;
  busy.value = false;

  if (id && !transfer.value) await router.replace({ name: 'stock-transfer', params: { id } });
  if (id) await loadTransfer(id);
  if (ok) await utils.showMessageModal({ Description: 'Traspaso enviado.', MessageType: 'success' });
};

const validReceived = (l: StockTransferLot) => {
  const q = received.value[l.Id];
  if (typeof q !== 'number' || q < 0 || q > l.QuantitySent) return false;
  return !l.SerialNumber || q === 0 || q === 1;
};

const receive = async () => {
  const t = transfer.value!;
  const lots = t.Detail.flatMap(d => d.Lots);
  if (lots.some(l => !validReceived(l))) {
    await utils.showMessageModal({ Description: 'Revise las cantidades recibidas: van de 0 a lo enviado.', MessageType: 'warning' });
    return;
  }

  const faltante = lots.reduce((s, l) => s + (l.QuantitySent - (received.value[l.Id] ?? 0)), 0);
  const confirmed = await utils.showMessageQuestion(faltante > 0
    ? `Faltan ${faltante} unidad(es) respecto de lo enviado: quedarán como merma de ${t.OriginBranchName}. ¿Confirmar la recepción?`
    : '¿Confirmar que llegó todo lo enviado? El stock entra a esta sucursal.');
  if (!confirmed) return;

  busy.value = true;
  const { ok } = await receiveTransfer(t.Id, received.value);
  busy.value = false;
  if (!ok) return;

  await utils.showMessageModal({ Description: 'Recepción registrada.', MessageType: 'success' });
  await loadTransfer(t.Id);
};

const cancel = async () => {
  const confirmed = await utils.showMessageQuestion('¿Anular este borrador? No se movió stock todavía.');
  if (!confirmed) return;

  const { ok } = await cancelTransfer(transfer.value!.Id);
  if (ok) returnPage();
};

const returnPage = () => router.push({ name: 'stock-transfers' });

const formatDateTime = (value: string | null) =>
  value ? new Date(value).toLocaleString('es-BO', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false,
  }) : '—';
</script>

<style scoped></style>
