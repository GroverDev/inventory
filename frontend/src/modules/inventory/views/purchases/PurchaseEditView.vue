<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de Compras</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">
          {{ purchase.Id ? 'Editar Compra' : 'Nueva Compra' }}
        </li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>{{ purchase.Id ? 'Editar' : 'Nueva' }} <span class="fw-300"><i>Compra</i></span></h2>
        </div>
        <div class="panel-container show">

          <!-- Acciones -->
          <div class="panel-content pt-0">
            <div class="row align-items-center">
              <div class="col-8">
                <div class="d-none d-md-flex gap-2">
                  <button type="button" class="btn btn-sm btn-primary" :disabled="isSaved" @click="savePurchase">
                    <span class="fal fa-save me-1"></span>Grabar
                  </button>
                  <button type="button" class="btn btn-warning btn-sm" @click="returnPage">
                    <span class="fal fa-ban me-1"></span>Cancelar
                  </button>
                </div>
                <div class="d-md-none">
                  <div class="btn-group">
                    <button type="button" class="btn btn-primary dropdown-toggle"
                      data-bs-toggle="dropdown" data-bs-display="static">Opciones</button>
                    <div class="dropdown-menu">
                      <button type="button" class="dropdown-item" :disabled="isSaved" @click="savePurchase">
                        <span class="fal fa-save me-1"></span>Grabar
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

          <!-- Header de la compra -->
          <div class="panel-content pt-0">
            <h6 class="text-muted border-bottom pb-2 mb-3">
              <i class="fal fa-file-invoice me-1"></i> Datos de la Orden
            </h6>
            <div class="row">
              <!-- Proveedor con búsqueda -->
              <div class="col-12 col-md-5 mb-3">
                <label class="form-label">Proveedor <span class="text-danger">*</span></label>
                <div class="input-group input-group-sm" v-if="!purchase.ProviderId">
                  <input
                    type="text"
                    class="form-control"
                    v-model="providerSearch"
                    placeholder="Buscar proveedor..."
                    autocomplete="off"
                    @keyup.enter="searchProviders"
                    :disabled="isSaved"
                  />
                  <button class="btn btn-outline-secondary" type="button" @click="searchProviders">
                    <i class="fal fa-search"></i>
                  </button>
                </div>
                <div v-else class="d-flex align-items-center gap-2">
                  <span class="fw-semibold">{{ purchase.ProviderName }}</span>
                  <button v-if="!isSaved" type="button" class="btn btn-link btn-sm p-0 text-danger"
                    @click="clearProvider">
                    <i class="fal fa-times"></i>
                  </button>
                </div>
                <!-- Dropdown resultados proveedor -->
                <div v-if="providerResults.length > 0" class="list-group mt-1 shadow-sm position-absolute" style="z-index:1000; width:350px">
                  <button
                    v-for="p in providerResults" :key="p.Id"
                    type="button"
                    class="list-group-item list-group-item-action py-1 px-2"
                    @click="selectProvider(p)"
                  >
                    {{ p.ProviderName }}
                  </button>
                </div>
              </div>

              <div class="col-6 col-md-3 mb-3">
                <label class="form-label">Fecha de Compra <span class="text-danger">*</span></label>
                <input type="date" class="form-control form-control-sm" v-model="purchase.PurchaseDate" :disabled="isSaved" />
              </div>
              <div class="col-6 col-md-3 mb-3">
                <label class="form-label">Entrega Estimada</label>
                <input type="date" class="form-control form-control-sm" v-model="purchase.EstimatedDeliveryDate" :disabled="isSaved" />
              </div>
            </div>
          </div>

          <!-- Agregar producto al detalle -->
          <div class="panel-content pt-0" v-if="!isSaved">
            <h6 class="text-muted border-bottom pb-2 mb-3">
              <i class="fal fa-plus-circle me-1"></i> Agregar Producto
            </h6>
            <div class="row align-items-end g-2 mb-2">
              <div class="col-12 col-md-4">
                <label class="form-label">Producto</label>
                <div class="input-group input-group-sm">
                  <input
                    type="text"
                    class="form-control"
                    v-model="productSearch"
                    placeholder="Buscar producto..."
                    autocomplete="off"
                    @keyup.enter="searchProducts"
                  />
                  <button class="btn btn-outline-secondary" type="button" @click="searchProducts">
                    <i class="fal fa-search"></i>
                  </button>
                </div>
                <div v-if="productResults.length > 0" class="list-group mt-1 shadow-sm position-absolute" style="z-index:1000; width:350px">
                  <button
                    v-for="prod in productResults" :key="prod.Id"
                    type="button"
                    class="list-group-item list-group-item-action py-1 px-2"
                    @click="selectProduct(prod)"
                  >
                    {{ prod.ProductName }}
                  </button>
                </div>
              </div>
              <div class="col-4 col-md-2">
                <label class="form-label">Cantidad</label>
                <input type="number" class="form-control form-control-sm text-end" min="1" v-model.number="newLine.OrderedQuantity" />
              </div>
              <div class="col-4 col-md-2">
                <label class="form-label">Precio Unit.</label>
                <input type="number" class="form-control form-control-sm text-end" min="0" step="0.01" v-model.number="newLine.OrderUnitPrice" />
              </div>
              <div class="col-4 col-md-2">
                <label class="form-label">Subtotal</label>
                <input type="number" class="form-control form-control-sm text-end" readonly :value="lineSubtotal" />
              </div>
              <div class="col-12 col-md-2">
                <button type="button" class="btn btn-success btn-sm w-100" @click="addLine" :disabled="!newLine.ProductId">
                  <span class="fal fa-plus me-1"></span>Agregar
                </button>
              </div>
            </div>
          </div>

          <!-- Detalle de la compra -->
          <div class="panel-content pt-0">
            <h6 class="text-muted border-bottom pb-2 mb-3">
              <i class="fal fa-list me-1"></i> Detalle de la Orden
            </h6>

            <div v-if="purchase.Detail.length === 0" class="text-center py-3">
              <small class="text-muted">No hay productos en esta orden. Agregue productos usando el buscador de arriba.</small>
            </div>

            <template v-else>
              <table class="table table-sm align-middle mb-0">
                <thead class="">
                  <tr>
                    <th>Producto</th>
                    <th class="text-center">Cantidad</th>
                    <th class="text-end">Precio Unit.</th>
                    <th class="text-end">Subtotal</th>
                    <th v-if="!isSaved" class="text-center">Quitar</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(line, i) in purchase.Detail" :key="i">
                    <td class="fw-semibold">{{ line.ProductName }}</td>
                    <td class="text-center">{{ line.OrderedQuantity }}</td>
                    <td class="text-end">{{ formatCurrency(line.OrderUnitPrice) }}</td>
                    <td class="text-end">{{ formatCurrency(line.OrderFinalPrice) }}</td>
                    <td v-if="!isSaved" class="text-center">
                      <button type="button" class="btn btn-outline-danger btn-sm" @click="removeLine(i)">
                        <span class="fal fa-times"></span>
                      </button>
                    </td>
                  </tr>
                </tbody>
                <tfoot>
                  <tr class="fw-bold">
                    <td colspan="3" class="text-end">TOTAL</td>
                    <td class="text-end">{{ formatCurrency(purchase.Total) }}</td>
                    <td v-if="!isSaved"></td>
                  </tr>
                </tfoot>
              </table>
            </template>
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
import { Purchase, PurchaseDetail } from '@/modules/inventory/models/purchase.model';
import usePurchase from '@/modules/inventory/composables/usePurchase';
import useProvider from '@/modules/inventory/composables/useProvider';
import useProduct from '@/modules/inventory/composables/useProduct';
import type { Provider } from '@/modules/inventory/models/provider.model';
import type { Product } from '@/modules/inventory/models/product.model';

const router = useRouter();
const route = useRoute();
const { getPurchaseById, createPurchase, updatePurchase } = usePurchase();
const { getProviders } = useProvider();
const { getProductsByName } = useProduct();

const purchase = ref(new Purchase());
const isSaved = ref(false);

// Provider search
const providerSearch = ref('');
const providerResults = ref<Provider[]>([]);

// Product line search
const productSearch = ref('');
const productResults = ref<Product[]>([]);
const newLine = ref(new PurchaseDetail());

const lineSubtotal = computed(() =>
  +(newLine.value.OrderedQuantity * newLine.value.OrderUnitPrice).toFixed(2)
);

const formatCurrency = (val: number): string =>
  val?.toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '0.00';

onMounted(async () => {
  const id = route.params.id as string;
  const today = new Date().toISOString().split('T')[0];
  purchase.value.PurchaseDate = today;
  purchase.value.EstimatedDeliveryDate = today;
  if (id && id !== '0') await loadPurchase(id);
});

const loadPurchase = async (id: string) => {
  const { ok, Data } = await getPurchaseById(id);
  if (ok) {
    purchase.value = Data;
    // Normalize dates to yyyy-MM-dd for the date inputs
    if (Data.PurchaseDate) purchase.value.PurchaseDate = Data.PurchaseDate.toString().substring(0, 10);
    if (Data.EstimatedDeliveryDate) purchase.value.EstimatedDeliveryDate = Data.EstimatedDeliveryDate.toString().substring(0, 10);
  }
};

const searchProviders = async () => {
  if (!providerSearch.value.trim()) return;
  const { Data } = await getProviders(providerSearch.value);
  providerResults.value = Data;
};

const selectProvider = (p: Provider) => {
  purchase.value.ProviderId = p.Id;
  purchase.value.ProviderName = p.ProviderName;
  providerResults.value = [];
  providerSearch.value = '';
};

const clearProvider = () => {
  purchase.value.ProviderId = '';
  purchase.value.ProviderName = '';
};

const searchProducts = async () => {
  if (!productSearch.value.trim()) return;
  const { Data } = await getProductsByName(productSearch.value);
  productResults.value = Data;
};

const selectProduct = (prod: Product) => {
  newLine.value.ProductId = prod.Id;
  newLine.value.ProductName = prod.ProductName;
  newLine.value.OrderUnitPrice = prod.SalePrice ?? 0;
  productResults.value = [];
  productSearch.value = '';
};

const addLine = () => {
  if (!newLine.value.ProductId) return;
  const line = { ...newLine.value };
  line.OrderFinalPrice = lineSubtotal.value;
  purchase.value.Detail.push(line);
  recalcTotal();
  newLine.value = new PurchaseDetail();
};

const removeLine = (index: number) => {
  purchase.value.Detail.splice(index, 1);
  recalcTotal();
};

const recalcTotal = () => {
  purchase.value.Total = +purchase.value.Detail.reduce((sum, d) => sum + d.OrderFinalPrice, 0).toFixed(2);
};

const returnPage = () => router.push({ name: 'purchases-admin' });

const savePurchase = async () => {
  if (!purchase.value.ProviderId) {
    utils.showMessageModal({ Description: 'Seleccione un proveedor.', MessageType: 'warning' });
    return;
  }
  if (!purchase.value.PurchaseDate) {
    utils.showMessageModal({ Description: 'Ingrese la fecha de compra.', MessageType: 'warning' });
    return;
  }
  if (purchase.value.Detail.length === 0) {
    utils.showMessageModal({ Description: 'Agregue al menos un producto al detalle.', MessageType: 'warning' });
    return;
  }

  const ok = await utils.showMessageQuestion('¿Desea guardar la compra?');
  if (!ok) return;

  if (!purchase.value.Id) {
    const { ok: saved } = await createPurchase(purchase.value);
    if (saved) {
      isSaved.value = true;
      await utils.showMessageModal({ Description: 'La compra se registró correctamente.', MessageType: 'success' });
      returnPage();
    }
  } else {
    const { ok: updated } = await updatePurchase(purchase.value);
    if (updated) {
      await utils.showMessageModal({ Description: 'La compra se actualizó correctamente.', MessageType: 'success' });
      returnPage();
    }
  }
};
</script>

<style scoped></style>
