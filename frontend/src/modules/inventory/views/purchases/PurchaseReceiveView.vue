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
                  <button type="button" class="btn btn-sm btn-success" :disabled="isSaved" @click="saveReceive">
                    <span class="fal fa-box-check me-1"></span>Confirmar Recepción
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
                      <button type="button" class="dropdown-item" :disabled="isSaved" @click="saveReceive">
                        <span class="fal fa-box-check me-1"></span>Confirmar Recepción
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
              <div class="col-6 col-md-3 mb-2">
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
                <input type="date" class="form-control form-control-sm" v-model="delivery.DeliveryDate" :disabled="isSaved" />
              </div>
              <div class="col-12 col-md-3 mb-2">
                <label class="form-label mb-1">Estado de la recepción</label>
                <select class="form-select form-select-sm" v-model.number="delivery.PurchaseStatusId" :disabled="isSaved">
                  <option :value="2">Parcialmente recibido</option>
                  <option :value="3">Totalmente recibido</option>
                </select>
              </div>
            </div>

            <!-- Detalle de recepción -->
            <h6 class="text-muted border-bottom pb-2 mb-3">
              <i class="fal fa-boxes me-1"></i> Cantidades Recibidas
            </h6>
            <table class="table table-sm align-middle">
              <thead class="">
                <tr>
                  <th>Producto</th>
                  <th class="text-center">Cantidad Ordenada</th>
                  <th class="text-center" style="width:160px">Cantidad Recibida</th>
                  <th class="text-end" style="width:150px">Precio Unit. Recibido</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(line, i) in delivery.Detail" :key="i">
                  <td class="fw-semibold">{{ line.ProductName }}</td>
                  <td class="text-center">{{ line.OrderedQuantity }}</td>
                  <td class="text-center">
                    <input
                      type="number"
                      class="form-control form-control-sm text-center"
                      min="0"
                      :max="line.OrderedQuantity"
                      v-model.number="line.DeliveryQuantity"
                      :disabled="isSaved"
                    />
                  </td>
                  <td class="text-end">
                    <input
                      type="number"
                      class="form-control form-control-sm text-end"
                      min="0"
                      step="0.01"
                      v-model.number="deliveryPrices[i]"
                      :disabled="isSaved"
                    />
                  </td>
                </tr>
              </tbody>
            </table>
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
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import utils from '@/utils/msg';
import { Purchase, PurchaseDelivery, PurchaseDeliveryDetail } from '@/modules/inventory/models/purchase.model';
import usePurchase from '@/modules/inventory/composables/usePurchase';

const router = useRouter();
const route = useRoute();
const { getPurchaseById, receivePurchase } = usePurchase();

const purchase = ref(new Purchase());
const delivery = ref(new PurchaseDelivery());
const deliveryPrices = ref<number[]>([]);
const isSaved = ref(false);

const formatDate = (val: string | Date): string => {
  if (!val) return '—';
  return new Date(val).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const statusBadge = (id: number) => id === 3 ? 'badge bg-success' : id === 2 ? 'badge bg-warning text-dark' : 'badge bg-info text-dark';
const statusLabel = (id: number) => id === 3 ? 'Tot. Recibido' : id === 2 ? 'Parc. Recibido' : 'Solicitado';

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

  delivery.value.PurchaseId = Data.Id;
  delivery.value.DeliveryDate = new Date().toISOString().split('T')[0];
  delivery.value.PurchaseStatusId = 3;

  delivery.value.Detail = (Data.Detail ?? []).map(d => {
    const detail = new PurchaseDeliveryDetail();
    detail.ProductId = d.ProductId ?? (d as any).ProductId;
    detail.OrderedQuantity = d.OrderedQuantity;
    detail.DeliveryQuantity = d.OrderedQuantity;
    detail.ProductName = d.ProductName;
    return detail;
  });

  deliveryPrices.value = (Data.Detail ?? []).map(d => d.OrderUnitPrice ?? 0);
};

const returnPage = () => router.push({ name: 'purchases-admin' });

const saveReceive = async () => {
  if (!delivery.value.DeliveryDate) {
    utils.showMessageModal({ Description: 'Ingrese la fecha de recepción.', MessageType: 'warning' });
    return;
  }
  const ok = await utils.showMessageQuestion('¿Confirma la recepción de esta orden?');
  if (!ok) return;

  const { ok: saved } = await receivePurchase(purchase.value.Id, delivery.value);
  if (saved) {
    isSaved.value = true;
    await utils.showMessageModal({ Description: 'La recepción se registró correctamente.', MessageType: 'success' });
    returnPage();
  }
};
</script>

<style scoped></style>
