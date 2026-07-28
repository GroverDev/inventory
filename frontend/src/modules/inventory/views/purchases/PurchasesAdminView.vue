<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item active" aria-current="page">Registro de Compras</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>COMPRAS</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <!-- Botón Nuevo -->
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newPurchase">
                <span class="fal fa-plus-square me-1"></span>Nueva Compra
              </button>
            </div>

            <!-- Filtros -->
            <div class="row align-items-end g-2 mb-3">
              <div class="col-6 col-md-3">
                <label class="form-label">Fecha Inicio</label>
                <input type="date" class="form-control form-control-sm" v-model="filtro.dateInitial" />
              </div>
              <div class="col-6 col-md-3">
                <label class="form-label">Fecha Fin</label>
                <input type="date" class="form-control form-control-sm" v-model="filtro.dateEnd" />
              </div>
              <div class="col-12 col-md-3">
                <label class="form-label">Estado</label>
                <select class="form-select form-select-sm" v-model="filtro.statusId">
                  <option :value="1">Solicitado</option>
                  <option :value="2">Parcialmente recibido</option>
                  <option :value="3">Totalmente recibido</option>
                  <option :value="5">Cerrado con faltante</option>
                  <option :value="4">Cancelado</option>
                </select>
              </div>
              <div class="col-12 col-md-3">
                <button class="btn btn-primary btn-sm w-100" @click="getPurchasesData">
                  <span class="fal fa-search me-1"></span>Buscar
                </button>
              </div>
            </div>

            <!-- Contador -->
            <div v-if="purchases.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="fal fa-list me-1"></span>
                <strong>{{ purchases.length }}</strong> compra(s) encontrada(s)
              </small>
            </div>

            <!-- Estado vacío -->
            <div v-if="purchases.length === 0" class="text-center py-5">
              <i class="fal fa-shopping-cart fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Seleccione un rango de fechas y estado para buscar compras</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newPurchase">
                <span class="fal fa-plus me-1"></span>Crear nueva compra
              </button>
            </div>

            <template v-else>
              <!-- Tabla desktop -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="">
                    <tr>
                      <th>Fecha</th>
                      <th>Proveedor</th>
                      <th class="text-center">Estado</th>
                      <th class="d-none d-lg-table-cell">Entrega Estimada</th>
                      <th class="text-end">Total</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(purchase, index) in purchases" :key="index">
                      <td>{{ formatDate(purchase.PurchaseDate) }}</td>
                      <td class="fw-semibold">{{ purchase.ProviderName }}</td>
                      <td class="text-center">
                        <span :class="statusBadge(purchase.PurchaseStatusId)">
                          {{ purchase.PurchaseStatusName || statusLabel(purchase.PurchaseStatusId) }}
                        </span>
                      </td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ formatDate(purchase.EstimatedDeliveryDate) }}</small>
                      </td>
                      <td class="text-end fw-semibold">{{ formatCurrency(purchase.Total) }}</td>
                      <td class="text-center text-nowrap">
                        <button
                          v-if="canReceive(purchase.PurchaseStatusId)"
                          type="button"
                          class="btn btn-outline-success btn-sm me-1"
                          title="Recepcionar"
                          @click="receivePurchase(purchase.Id)"
                        >
                          <span class="fal fa-box-check"></span>
                        </button>
                        <button
                          v-if="canClose(purchase.PurchaseStatusId)"
                          type="button"
                          class="btn btn-outline-secondary btn-sm me-1"
                          title="Cerrar con faltante"
                          @click="closeWithShortage(purchase.Id)"
                        >
                          <span class="fal fa-lock-alt"></span>
                        </button>
                        <button
                          v-if="canModify(purchase.PurchaseStatusId)"
                          type="button"
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Editar"
                          @click="editPurchase(purchase.Id)"
                        >
                          <span class="fal fa-edit"></span>
                        </button>
                        <button
                          v-if="canModify(purchase.PurchaseStatusId)"
                          type="button"
                          class="btn btn-outline-warning btn-sm me-1"
                          title="Anular orden"
                          @click="cancelOrder(purchase.Id)"
                        >
                          <span class="fal fa-ban"></span>
                        </button>
                        <button
                          v-if="canModify(purchase.PurchaseStatusId)"
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removePurchase(purchase.Id)"
                        >
                          <span class="fal fa-trash-alt"></span>
                        </button>
                        <span v-if="isFinal(purchase.PurchaseStatusId)" class="text-muted small">
                          <i class="fal fa-lock me-1"></i>Sin acciones
                        </span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- Cards móvil -->
              <div class="d-md-none">
                <div class="row g-3">
                  <div class="col-12" v-for="(purchase, index) in purchases" :key="index">
                    <div class="card shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <p class="fw-semibold mb-0 lh-sm">{{ purchase.ProviderName }}</p>
                          <span :class="statusBadge(purchase.PurchaseStatusId)">
                            {{ purchase.PurchaseStatusName || statusLabel(purchase.PurchaseStatusId) }}
                          </span>
                        </div>
                        <small class="text-muted"><i class="fal fa-calendar me-1"></i>{{ formatDate(purchase.PurchaseDate) }}</small>
                        <div class="fs-6 fw-bold">{{ formatCurrency(purchase.Total) }}</div>
                        <div class="d-flex gap-2 pt-1 flex-wrap">
                          <button v-if="canReceive(purchase.PurchaseStatusId)"
                            type="button" class="btn btn-sm btn-outline-success flex-fill"
                            @click="receivePurchase(purchase.Id)">
                            <span class="fal fa-box-check me-1"></span>Recepcionar
                          </button>
                          <button v-if="canClose(purchase.PurchaseStatusId)"
                            type="button" class="btn btn-sm btn-outline-secondary flex-fill"
                            @click="closeWithShortage(purchase.Id)">
                            <span class="fal fa-lock-alt me-1"></span>Cerrar
                          </button>
                          <button v-if="canModify(purchase.PurchaseStatusId)"
                            type="button" class="btn btn-sm btn-outline-primary flex-fill"
                            @click="editPurchase(purchase.Id)">
                            <span class="fal fa-edit me-1"></span>Editar
                          </button>
                          <button v-if="canModify(purchase.PurchaseStatusId)"
                            type="button" class="btn btn-sm btn-outline-warning"
                            title="Anular orden"
                            @click="cancelOrder(purchase.Id)">
                            <span class="fal fa-ban"></span>
                          </button>
                          <button v-if="canModify(purchase.PurchaseStatusId)"
                            type="button" class="btn btn-sm btn-outline-danger"
                            @click="removePurchase(purchase.Id)">
                            <span class="fal fa-trash-alt"></span>
                          </button>
                          <small v-if="isFinal(purchase.PurchaseStatusId)" class="text-muted">
                            <i class="fal fa-lock me-1"></i>Orden cerrada, sin acciones disponibles
                          </small>
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
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import usePurchase from '@/modules/inventory/composables/usePurchase';
import { PURCHASE_STATUS, type Purchase } from '@/modules/inventory/models/purchase.model';
import utils from '@/utils/msg';

const purchases = ref<Purchase[]>([]);
const { getPurchases, deletePurchase, closePurchase, cancelPurchase } = usePurchase();
const router = useRouter();

const today = new Date().toISOString().split('T')[0];
const firstOfMonth = new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().split('T')[0];

const filtro = ref({ dateInitial: firstOfMonth, dateEnd: today, statusId: 1 });

const formatDate = (val: string | Date): string => {
  if (!val) return '—';
  return new Date(val).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const formatCurrency = (val: number): string =>
  val?.toLocaleString('es-BO', { style: 'currency', currency: 'BOB' }) ?? 'Bs. 0.00';

const statusBadge = (statusId: number): string => {
  if (statusId === PURCHASE_STATUS.TOTALLY_RECEIVED) return 'badge bg-success';
  if (statusId === PURCHASE_STATUS.PARTIALLY_RECEIVED) return 'badge bg-warning text-dark';
  if (statusId === PURCHASE_STATUS.CANCELLED) return 'badge bg-danger';
  if (statusId === PURCHASE_STATUS.CLOSED) return 'badge bg-secondary';
  return 'badge bg-info text-dark';
};

const statusLabel = (statusId: number): string => {
  if (statusId === PURCHASE_STATUS.TOTALLY_RECEIVED) return 'Tot. Recibido';
  if (statusId === PURCHASE_STATUS.PARTIALLY_RECEIVED) return 'Parc. Recibido';
  if (statusId === PURCHASE_STATUS.CANCELLED) return 'Cancelado';
  if (statusId === PURCHASE_STATUS.CLOSED) return 'Cerrado';
  return 'Solicitado';
};

// Las acciones disponibles se derivan del estado, igual que en el backend.
// Ocultarlas es solo comodidad: el servidor vuelve a validar cada una.
const canReceive = (statusId: number): boolean =>
  statusId === PURCHASE_STATUS.REQUESTED || statusId === PURCHASE_STATUS.PARTIALLY_RECEIVED;

const canClose = (statusId: number): boolean =>
  statusId === PURCHASE_STATUS.PARTIALLY_RECEIVED;

/** Editar, anular o eliminar solo mientras la orden no haya recibido nada. */
const canModify = (statusId: number): boolean =>
  statusId === PURCHASE_STATUS.REQUESTED;

const isFinal = (statusId: number): boolean =>
  statusId === PURCHASE_STATUS.TOTALLY_RECEIVED
  || statusId === PURCHASE_STATUS.CANCELLED
  || statusId === PURCHASE_STATUS.CLOSED;

const getPurchasesData = async () => {
  const { Data } = await getPurchases(filtro.value.dateInitial, filtro.value.dateEnd, filtro.value.statusId);
  purchases.value = Data;
};

const newPurchase = () => router.push({ name: 'purchase-edit', params: { id: '0' } });
const editPurchase = (id: string) => router.push({ name: 'purchase-edit', params: { id } });
const receivePurchase = (id: string) => router.push({ name: 'purchase-receive', params: { id } });

const removePurchase = async (id: string) => {
  const ok = await utils.showMessageQuestion('¿Desea eliminar la compra?');
  if (ok) {
    const { ok: deleted } = await deletePurchase(id);
    if (deleted) {
      await utils.showMessageModal({ Description: 'La compra se eliminó correctamente.', MessageType: 'success' });
      await getPurchasesData();
    }
  }
};

const closeWithShortage = async (id: string) => {
  const ok = await utils.showMessageQuestion(
    '¿Cerrar la orden con faltante? El saldo pendiente ya no se podrá recibir.'
  );
  if (!ok) return;

  const { ok: closed } = await closePurchase(id);
  if (closed) {
    await utils.showMessageModal({ Description: 'La orden se cerró con faltante.', MessageType: 'success' });
    await getPurchasesData();
  }
};

const cancelOrder = async (id: string) => {
  const ok = await utils.showMessageQuestion('¿Desea anular esta orden de compra?');
  if (!ok) return;

  const { ok: cancelled } = await cancelPurchase(id);
  if (cancelled) {
    await utils.showMessageModal({ Description: 'La orden se anuló correctamente.', MessageType: 'success' });
    await getPurchasesData();
  }
};

onMounted(getPurchasesData);
</script>

<style scoped></style>
