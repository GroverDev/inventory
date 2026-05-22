<template>
  <div class="pos-page px-2 px-md-3 pt-2">

    <!-- ── Cabecera: volver + título ── -->
    <div class="pos-top-bar d-flex align-items-center mb-2">
      <button class="btn btn-sm btn-outline-secondary me-2" type="button" @click="router.back()">
        <i class="fal fa-arrow-left me-1"></i>Volver
      </button>
      <h6 class="mb-0 fw-semibold">Punto de Venta</h6>
    </div>

    <!-- ══ MÓVIL: cliente + tabs ══ -->
    <div class="d-md-none mb-2" style="position:relative">
      <!-- Selector de cliente -->
      <div class="card mb-2">
        <div class="card-body py-2 px-3">
          <div v-if="!selectedCustomer">
            <div class="input-group input-group-sm">
              <span class="input-group-text"><i class="fal fa-user"></i></span>
              <input
                type="text" class="form-control" placeholder="Buscar cliente..."
                v-model="customerSearch" @keyup.enter="searchCustomers"
              />
              <button class="btn btn-outline-secondary" type="button" @click="searchCustomers">
                <i class="fal fa-search"></i>
              </button>
            </div>
            <div
              v-if="customerResults.length > 0"
              class="list-group shadow position-absolute start-0 end-0 mx-3"
              style="z-index:1060; top:100%"
            >
              <button
                v-for="c in customerResults" :key="c.Id" type="button"
                class="list-group-item list-group-item-action py-2 px-3"
                @click="selectCustomer(c)"
              >
                <div class="fw-semibold">{{ c.FullName }}</div>
                <small class="text-muted">{{ c.DocumentNumber }}</small>
              </button>
            </div>
          </div>
          <div v-else class="d-flex align-items-center justify-content-between">
            <div>
              <i class="fal fa-user text-primary me-1"></i>
              <span class="fw-semibold">{{ selectedCustomer.FullName }}</span>
            </div>
            <div class="d-flex align-items-center gap-2">
              <span class="fw-bold text-primary small">Bs. {{ formatNum(total) }}</span>
              <button type="button" class="btn btn-outline-secondary btn-sm py-0 px-1" @click="clearCustomer">
                <i class="fal fa-times"></i>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Tabs productos / carrito -->
      <ul class="nav nav-pills nav-fill mb-2">
        <li class="nav-item">
          <button
            class="nav-link w-100"
            :class="activeTab === 'products' ? 'active' : ''"
            @click="activeTab = 'products'"
          >
            <i class="fal fa-th me-1"></i>Productos
          </button>
        </li>
        <li class="nav-item">
          <button
            class="nav-link w-100 position-relative"
            :class="activeTab === 'cart' ? 'active' : ''"
            @click="activeTab = 'cart'"
          >
            <i class="fal fa-shopping-cart me-1"></i>Carrito
            <span
              v-if="totalItems > 0"
              class="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger"
              style="font-size:0.65rem"
            >{{ totalItems }}</span>
          </button>
        </li>
      </ul>
    </div>

    <!-- ══ LAYOUT SPLIT ══ -->
    <div class="pos-split">

      <!-- ════ PANEL IZQUIERDO: carrito ════ -->
      <div
        class="pos-cart-panel"
        :class="activeTab === 'cart' ? '' : 'd-none d-md-flex'"
      >
        <!-- Zona superior: cliente + lista (crece y hace scroll en desktop) -->
        <div class="pos-cart-body">

        <!-- Cliente (solo desktop) -->
        <div class="card mb-2 d-none d-md-block" style="position:relative">
          <div class="card-header py-2 px-3">
            <span class="fw-semibold small"><i class="fal fa-user me-1 text-primary"></i>Cliente</span>
          </div>
          <div class="card-body py-2 px-3">
            <div v-if="!selectedCustomer">
              <div class="input-group input-group-sm">
                <input
                  type="text" class="form-control" placeholder="Buscar cliente..."
                  v-model="customerSearch" @keyup.enter="searchCustomers"
                />
                <button class="btn btn-outline-secondary" type="button" @click="searchCustomers">
                  <i class="fal fa-search"></i>
                </button>
              </div>
              <div
                v-if="customerResults.length > 0"
                class="list-group shadow position-absolute start-0 end-0 mx-2"
                style="z-index:1060; top:100%"
              >
                <button
                  v-for="c in customerResults" :key="c.Id" type="button"
                  class="list-group-item list-group-item-action py-2 px-3"
                  @click="selectCustomer(c)"
                >
                  <div class="fw-semibold">{{ c.FullName }}</div>
                  <small class="text-muted">{{ c.DocumentNumber }}</small>
                </button>
              </div>
              <small class="text-muted d-block mt-1">Ingresa nombre y presiona Enter</small>
            </div>
            <div v-else class="d-flex align-items-center justify-content-between">
              <div>
                <div class="fw-semibold">{{ selectedCustomer.FullName }}</div>
                <small class="text-muted">{{ selectedCustomer.DocumentNumber }}</small>
              </div>
              <button type="button" class="btn btn-outline-secondary btn-sm" @click="clearCustomer">
                <i class="fal fa-times"></i>
              </button>
            </div>
          </div>
        </div>

        <!-- Items del carrito -->
        <div class="card mb-2 pos-cart-items-card">
          <div class="card-header py-2 px-3">
            <span class="fw-semibold small">
              <i class="fal fa-shopping-cart me-1 text-primary"></i>Carrito
              <span v-if="totalItems > 0" class="badge bg-primary ms-1">{{ totalItems }}</span>
            </span>
          </div>

          <div v-if="cart.length === 0" class="card-body text-center py-4">
            <i class="fal fa-cart-plus fa-2x text-muted d-block mb-2"></i>
            <small class="text-muted">Sin productos.<br>Selecciona del catálogo.</small>
          </div>

          <div v-else class="list-group list-group-flush pos-cart-list">
            <div
              class="list-group-item px-3 py-2"
              v-for="(line, i) in cart" :key="i"
            >
              <div class="d-flex justify-content-between align-items-start mb-1">
                <span class="fw-semibold lh-sm" style="font-size:0.85rem">{{ line.ProductName }}</span>
                <button
                  type="button" class="btn btn-link btn-sm text-danger p-0 ms-1"
                  @click="removeFromCart(i)"
                >
                  <i class="fal fa-times-circle"></i>
                </button>
              </div>
              <div class="d-flex align-items-center justify-content-between">
                <div class="d-flex align-items-center gap-1">
                  <button
                    type="button"
                    class="btn btn-outline-secondary btn-sm py-0 px-2 lh-1"
                    @click="decreaseQty(i)"
                  >−</button>
                  <span class="fw-bold px-2">{{ line.Quantity }}</span>
                  <button
                    type="button"
                    class="btn btn-outline-secondary btn-sm py-0 px-2 lh-1"
                    @click="increaseQty(i)"
                  >+</button>
                </div>
                <div class="text-end">
                  <small class="text-muted d-block" style="font-size:0.72rem">
                    Bs. {{ formatNum(line.UnitPrice) }} × {{ line.Quantity }}
                  </small>
                  <span class="fw-bold text-primary" style="font-size:0.9rem">
                    Bs. {{ formatNum(line.LineTotal) }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        </div><!-- /pos-cart-body -->

        <!-- Zona inferior: totales + botones (pegada al fondo en desktop) -->
        <div class="pos-cart-footer">

        <!-- Totales -->
        <div class="card mb-2" v-if="cart.length > 0">
          <div class="card-body py-2 px-3">
            <div class="d-flex justify-content-between mb-1">
              <small class="text-muted">Subtotal</small>
              <small>Bs. {{ formatNum(subtotal) }}</small>
            </div>
            <div class="d-flex justify-content-between mb-1" v-if="totalDiscounts > 0">
              <small class="text-muted">Descuentos</small>
              <small class="text-danger">− Bs. {{ formatNum(totalDiscounts) }}</small>
            </div>
            <div class="d-flex justify-content-between align-items-center border-top pt-2 mt-1">
              <span class="fw-bold">TOTAL</span>
              <span class="fw-bold fs-5 text-primary">Bs. {{ formatNum(total) }}</span>
            </div>
          </div>
        </div>

        <!-- Botones de acción -->
        <div class="d-grid gap-2">
          <button
            type="button"
            class="btn btn-primary"
            :disabled="cart.length === 0 || !selectedCustomer"
            @click="confirmSale"
          >
            <i class="fal fa-check-circle me-1"></i>
            {{ selectedCustomer ? 'Cobrar — Bs. ' + formatNum(total) : 'Selecciona un cliente' }}
          </button>
          <button
            type="button"
            class="btn btn-outline-secondary btn-sm"
            :disabled="cart.length === 0 && !selectedCustomer"
            @click="resetAll"
          >
            <i class="fal fa-trash-alt me-1"></i>Limpiar todo
          </button>
        </div>

        </div><!-- /pos-cart-footer -->
      </div><!-- /pos-cart-panel -->

      <!-- ════ PANEL DERECHO: catálogo ════ -->
      <div
        class="pos-catalog-panel"
        :class="activeTab === 'products' ? '' : 'd-none d-md-flex'"
      >
        <!-- Buscador + pills (sticky en desktop) -->
        <div class="pos-catalog-head">
          <!-- Buscador de productos -->
          <div class="card mb-2">
            <div class="card-body py-2 px-3">
              <div class="input-group input-group-sm">
                <span class="input-group-text"><i class="fal fa-search"></i></span>
                <input
                  type="text" class="form-control"
                  placeholder="Buscar producto por nombre..."
                  v-model="productSearch"
                  ref="productInputRef"
                />
                <button
                  v-if="productSearch"
                  class="btn btn-outline-secondary" type="button"
                  @click="productSearch = ''"
                >
                  <i class="fal fa-times"></i>
                </button>
              </div>
            </div>
          </div>

          <!-- Pills de laboratorio -->
          <div class="mb-2 d-flex flex-wrap gap-1" v-if="labFilters.length > 0">
            <button
              type="button" class="btn btn-sm"
              :class="selectedLab === '' ? 'btn-primary' : 'btn-outline-secondary'"
              @click="selectedLab = ''"
            >Todos</button>
            <button
              v-for="lab in labFilters" :key="lab"
              type="button" class="btn btn-sm"
              :class="selectedLab === lab ? 'btn-primary' : 'btn-outline-secondary'"
              @click="selectedLab = lab === selectedLab ? '' : lab"
            >{{ lab }}</button>
          </div>
        </div>

        <!-- Grid scrollable (solo esta sección hace scroll) -->
        <div class="pos-catalog-grid">

        <!-- Cargando -->
        <div v-if="loadingProducts" class="text-center py-5">
          <i class="fal fa-spinner fa-spin fa-2x text-muted"></i>
          <p class="text-muted mt-2 mb-0">Cargando catálogo...</p>
        </div>

        <!-- Sin resultados -->
        <div v-else-if="filteredProducts.length === 0" class="card">
          <div class="card-body text-center py-5">
            <i class="fal fa-search fa-2x text-muted d-block mb-2"></i>
            <p class="text-muted mb-0">No se encontraron productos.</p>
          </div>
        </div>

        <!-- Grid de productos -->
        <div v-else class="row g-2">
          <div
            class="col-6 col-sm-4 col-lg-3"
            v-for="prod in filteredProducts" :key="prod.Id"
          >
            <div
              class="card h-100 product-card"
              :class="{
                'border-primary shadow-sm': cartQty(prod.Id) > 0,
                'product-card--disabled': prod.CurrentStock <= 0
              }"
              @click="prod.CurrentStock > 0 && addToCart(prod)"
            >
              <!-- Badge cantidad en carrito -->
              <span
                v-if="cartQty(prod.Id) > 0"
                class="position-absolute top-0 end-0 translate-middle badge rounded-pill bg-primary"
                style="font-size:0.65rem; z-index:2; margin:0.25rem"
              >{{ cartQty(prod.Id) }}</span>

              <div class="card-body p-2 d-flex flex-column">
                <!-- Nombre -->
                <div
                  class="fw-semibold mb-1 lh-sm"
                  style="font-size:0.82rem; min-height:2.5em; overflow:hidden; display:-webkit-box; -webkit-line-clamp:2; -webkit-box-orient:vertical"
                >{{ prod.ProductName }}</div>

                <!-- Lab -->
                <small class="text-muted mb-2" style="font-size:0.72rem">
                  {{ prod.LaboratoryName || '—' }}
                </small>

                <div class="mt-auto">
                  <!-- Precio + stock -->
                  <div class="d-flex justify-content-between align-items-center mb-2">
                    <span class="fw-bold text-primary" style="font-size:0.9rem">
                      Bs. {{ formatNum(prod.SalePrice) }}
                    </span>
                    <small
                      style="font-size:0.7rem"
                      :class="prod.CurrentStock > 5 ? 'text-success' : prod.CurrentStock > 0 ? 'text-warning' : 'text-danger'"
                    >
                      <i class="fal fa-box me-1"></i>
                      {{ prod.CurrentStock > 0 ? prod.CurrentStock : 'Agotado' }}
                    </small>
                  </div>

                  <!-- Sin stock -->
                  <div
                    v-if="prod.CurrentStock <= 0"
                    class="btn btn-sm btn-outline-secondary w-100 disabled"
                    style="font-size:0.75rem"
                  >Sin stock</div>

                  <!-- No está en carrito -->
                  <button
                    v-else-if="cartQty(prod.Id) === 0"
                    type="button"
                    class="btn btn-sm btn-outline-primary w-100"
                    style="font-size:0.75rem"
                    @click.stop="addToCart(prod)"
                  >
                    <i class="fal fa-plus me-1"></i>Agregar
                  </button>

                  <!-- Ya está en carrito: controles +/- -->
                  <div v-else class="d-flex align-items-center justify-content-between gap-1">
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-secondary flex-fill py-0"
                      @click.stop="decreaseQtyByProduct(prod.Id)"
                    >−</button>
                    <span class="fw-bold text-primary px-1">{{ cartQty(prod.Id) }}</span>
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-primary flex-fill py-0"
                      @click.stop="addToCart(prod)"
                    >+</button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        </div><!-- /pos-catalog-grid -->
      </div><!-- /pos-catalog-panel -->
    </div><!-- /pos-split -->
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { Sale } from '@/modules/inventory/models/sale.model';
import { SaleDetail } from '@/modules/inventory/models/saleDetail.model';
import type { Customer } from '@/modules/inventory/models/customer.model';
import type { Product } from '@/modules/inventory/models/product.model';
import useSales from '@/modules/inventory/composables/useSales';
import useCustomer from '@/modules/inventory/composables/useCustomer';
import useProduct from '@/modules/inventory/composables/useProduct';
import utils from '@/utils/msg';

const router = useRouter();

const { saveSaleApi } = useSales();
const { getCustomers } = useCustomer();
const { getProductsByName } = useProduct();

// ── Estado ─────────────────────────────────────────────────
const cart = ref<SaleDetail[]>([]);
const allProducts = ref<Product[]>([]);
const loadingProducts = ref(false);
const selectedCustomer = ref<Customer | null>(null);
const customerSearch = ref('');
const customerResults = ref<Customer[]>([]);
const productSearch = ref('');
const selectedLab = ref('');
const activeTab = ref<'products' | 'cart'>('products');
const productInputRef = ref<HTMLInputElement | null>(null);

// ── Computed ───────────────────────────────────────────────
const labFilters = computed(() => {
  const labs = allProducts.value
    .map((p: Product) => p.LaboratoryName)
    .filter((l: string) => l && l.trim() !== '');
  return [...new Set(labs)].sort() as string[];
});

const filteredProducts = computed(() => {
  let list = allProducts.value.filter((p: Product) => p.IsActive);
  if (productSearch.value.trim()) {
    const q = productSearch.value.toLowerCase();
    list = list.filter((p: Product) => p.ProductName.toLowerCase().includes(q));
  }
  if (selectedLab.value) {
    list = list.filter((p: Product) => p.LaboratoryName === selectedLab.value);
  }
  return list;
});

const totalItems = computed(() =>
  cart.value.reduce((s: number, l: SaleDetail) => s + l.Quantity, 0)
);
const subtotal = computed(() =>
  +cart.value.reduce((s: number, l: SaleDetail) => s + l.LineSubtotal, 0).toFixed(2)
);
const totalDiscounts = computed(() =>
  +cart.value.reduce((s: number, l: SaleDetail) => s + l.LineTotalDiscounts, 0).toFixed(2)
);
const total = computed(() => +(subtotal.value - totalDiscounts.value).toFixed(2));

// ── Helpers ────────────────────────────────────────────────
const formatNum = (val: number) =>
  (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const cartQty = (productId: string): number => {
  const line = cart.value.find((l: SaleDetail) => l.ProductId === productId);
  return line ? line.Quantity : 0;
};

const recalcLine = (i: number) => {
  const l = cart.value[i];
  l.LineSubtotal = +(l.Quantity * l.UnitPrice).toFixed(2);
  l.LineTotal = +(l.LineSubtotal - l.LineTotalDiscounts).toFixed(2);
};

// ── Carga inicial ──────────────────────────────────────────
onMounted(async () => {
  loadingProducts.value = true;
  const { Data } = await getProductsByName('');
  allProducts.value = (Data ?? []).filter((p: Product) => p.IsActive && p.CurrentStock >= 0);
  loadingProducts.value = false;
  productInputRef.value?.focus();
});

// ── Clientes ───────────────────────────────────────────────
const searchCustomers = async () => {
  if (!customerSearch.value.trim()) return;
  const { Data } = await getCustomers(customerSearch.value.trim());
  customerResults.value = Data ?? [];
};

const selectCustomer = (c: Customer) => {
  selectedCustomer.value = c;
  customerResults.value = [];
  customerSearch.value = '';
};

const clearCustomer = () => {
  selectedCustomer.value = null;
  customerSearch.value = '';
  customerResults.value = [];
};

// ── Carrito ────────────────────────────────────────────────
const addToCart = (prod: Product) => {
  const idx = cart.value.findIndex((l: SaleDetail) => l.ProductId === prod.Id);
  if (idx >= 0) {
    cart.value[idx].Quantity++;
    recalcLine(idx);
  } else {
    const line = new SaleDetail();
    line.ProductId = prod.Id;
    line.ProductName = prod.ProductName;
    line.UnitPrice = prod.SalePrice;
    line.Quantity = 1;
    line.LineTotalDiscounts = 0;
    line.LineSubtotal = +prod.SalePrice.toFixed(2);
    line.LineTotal = line.LineSubtotal;
    cart.value.push(line);
  }
};

const increaseQty = (i: number) => {
  cart.value[i].Quantity++;
  recalcLine(i);
};

const decreaseQty = (i: number) => {
  if (cart.value[i].Quantity <= 1) {
    cart.value.splice(i, 1);
  } else {
    cart.value[i].Quantity--;
    recalcLine(i);
  }
};

const decreaseQtyByProduct = (productId: string) => {
  const idx = cart.value.findIndex((l: SaleDetail) => l.ProductId === productId);
  if (idx >= 0) decreaseQty(idx);
};

const removeFromCart = (i: number) => {
  cart.value.splice(i, 1);
};

// ── Guardar venta ──────────────────────────────────────────
const confirmSale = async () => {
  if (!selectedCustomer.value) {
    utils.showMessageModal({ Description: 'Selecciona un cliente antes de cobrar.', MessageType: 'warning' });
    return;
  }
  if (cart.value.length === 0) {
    utils.showMessageModal({ Description: 'El carrito está vacío.', MessageType: 'warning' });
    return;
  }

  const confirmed = await utils.showMessageQuestion(
    `¿Confirmar venta por Bs. ${formatNum(total.value)} a ${selectedCustomer.value.FullName}?`
  );
  if (!confirmed) return;

  const sale = new Sale();
  sale.CustomerId = selectedCustomer.value.Id;
  sale.SaleDate = new Date().toISOString();
  sale.IsActive = true;
  sale.Subtotal = subtotal.value;
  sale.TotalDiscounts = totalDiscounts.value;
  sale.Total = total.value;
  sale.Detail = cart.value.map((l: SaleDetail) => {
    const d = new SaleDetail();
    d.ProductId = l.ProductId;
    d.Quantity = l.Quantity;
    d.UnitPrice = l.UnitPrice;
    d.LineSubtotal = l.LineSubtotal;
    d.LineTotalDiscounts = l.LineTotalDiscounts;
    d.LineTotal = l.LineTotal;
    return d;
  });

  const { ok: saved, Message } = await saveSaleApi(sale);
  if (saved) {
    await utils.showMessageModal({ Description: '¡Venta registrada correctamente!', MessageType: 'success' });
    resetAll();
  } else {
    utils.showMessageModal({
      Description: Message?.Description || 'No se pudo registrar la venta.',
      MessageType: 'error',
    });
  }
};

const resetCart = () => {
  cart.value = [];
  activeTab.value = 'products';
  productInputRef.value?.focus();
};

const resetAll = () => {
  resetCart();
  clearCustomer();
  productSearch.value = '';
  selectedLab.value = '';
};
</script>

<style scoped>
/* ── Layout flex de altura fija solo en desktop ── */
@media (min-width: 768px) {
  /* Contenedor raíz: ocupa el alto disponible bajo el header de app */
  .pos-page {
    display: flex;
    flex-direction: column;
    height: calc(100vh - var(--app-header-height, 5.5rem));
    overflow: hidden;
  }
  /* Cabecera POS (volver + título): altura fija, no hace scroll */
  .pos-top-bar { flex-shrink: 0; }

  /* Cuerpo split: ocupa el espacio restante */
  .pos-split {
    flex: 1;
    display: flex;
    gap: 0.5rem;
    overflow: hidden;
    min-height: 0;
  }

  /* Panel izquierdo (carrito): ancho fijo, flex columna con 3 zonas */
  .pos-cart-panel {
    width: 33.333%;
    flex-shrink: 0;
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }
  /* Zona superior: crece y su lista interna hace scroll */
  .pos-cart-body {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    min-height: 0;
  }
  /* La card "Carrito" crece para empujar totales al fondo */
  .pos-cart-items-card {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    min-height: 0;
  }
  /* Lista de ítems: único elemento que hace scroll dentro del carrito */
  .pos-cart-list {
    flex: 1;
    overflow-y: auto;
    min-height: 0;
  }
  /* Zona inferior: totales + botones, siempre visible abajo */
  .pos-cart-footer { flex-shrink: 0; }

  /* Panel derecho (catálogo): ocupa el resto, flex columna */
  .pos-catalog-panel {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    min-height: 0;
  }
  /* Buscador + pills: altura fija arriba, NO hace scroll */
  .pos-catalog-head { flex-shrink: 0; }

  /* Grid de productos: ÚNICO elemento que hace scroll */
  .pos-catalog-grid {
    flex: 1;
    overflow-y: auto;
    overflow-x: hidden;
    min-height: 0;
    padding-bottom: 0.5rem;
  }
}

/* ── Cards de producto ── */
.product-card {
  transition: box-shadow 0.15s ease, border-color 0.15s ease;
  cursor: pointer;
  position: relative;
  overflow: visible;
}
.product-card:hover:not(.product-card--disabled) {
  box-shadow: 0 0.25rem 0.75rem rgba(0, 0, 0, 0.12) !important;
}
.product-card--disabled {
  cursor: not-allowed;
  opacity: 0.55;
}
</style>
