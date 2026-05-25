<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventarios</li>
        <li class="breadcrumb-item active" aria-current="page">Control de Stock</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Control de <span class="fw-300"><i>STOCK</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <!-- Filtros -->
            <div class="row align-items-end g-2 mb-3 mt-0">
              <div class="col-12 col-md-6 col-lg-5">
                <label class="form-label">Buscar producto</label>
                <div class="input-group body-bg shadow-inset-2 rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="sa sa-magnifier text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="search"
                    placeholder="Nombre o SKU..."
                    autocomplete="off"
                    @keyup.enter="onSearch"
                  />
                  <button class="btn btn-primary" type="button" @click="onSearch">Buscar</button>
                </div>
              </div>
              <div class="col-auto">
                <div class="form-check form-switch mt-1">
                  <input class="form-check-input" type="checkbox" id="showLowStock" v-model="showOnlyLow" @change="onSearch" />
                  <label class="form-check-label" for="showLowStock">Solo stock bajo</label>
                </div>
              </div>
            </div>

            <!-- Cargando -->
            <div v-if="loading" class="text-center py-5">
              <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Cargando...</span>
              </div>
              <p class="text-muted mt-2">Cargando productos...</p>
            </div>

            <!-- Sin resultados -->
            <div v-else-if="products.length === 0" class="text-center py-5">
              <i class="fal fa-box-open fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted">No se encontraron productos.</p>
            </div>

            <!-- Resultados -->
            <template v-else>

              <!-- Barra: contador + selector de página -->
              <div class="d-flex align-items-center justify-content-between mb-2 flex-wrap gap-2">
                <small class="text-muted">
                  <span class="fal fa-boxes me-1"></span>
                  Mostrando
                  <strong>{{ rangeStart }}–{{ rangeEnd }}</strong>
                  de <strong>{{ totalCount }}</strong> producto(s)
                </small>
                <div class="d-flex align-items-center gap-2">
                  <label class="form-label mb-0 text-muted" style="white-space:nowrap">Por página</label>
                  <select
                    class="form-select form-select-sm"
                    style="width:80px"
                    v-model.number="pageSize"
                    @change="onPageSizeChange"
                  >
                    <option :value="10">10</option>
                    <option :value="15">15</option>
                    <option :value="25">25</option>
                    <option :value="50">50</option>
                  </select>
                </div>
              </div>

              <!-- Tabla desktop -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th>SKU</th>
                      <th>Producto</th>
                      <th class="text-center">Stock Actual</th>
                      <th class="text-center d-none d-lg-table-cell">Mínimo</th>
                      <th class="text-center">Estado</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="product in products" :key="product.Id">
                      <td><small class="text-muted font-monospace">{{ product.ProductCode }}</small></td>
                      <td class="fw-semibold">{{ product.ProductName }}</td>
                      <td class="text-center">
                        <span class="badge fs-6" :class="stockBadgeClass(product)">
                          {{ product.CurrentStock }}
                        </span>
                      </td>
                      <td class="text-center d-none d-lg-table-cell">
                        <small class="text-muted">{{ product.MinReorderQuantity }}</small>
                      </td>
                      <td class="text-center">
                        <span class="badge" :class="product.IsActive ? 'bg-success' : 'bg-secondary'">
                          {{ product.IsActive ? 'Activo' : 'Inactivo' }}
                        </span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-info btn-sm me-1"
                          title="Ver historial de movimientos"
                          @click="goHistory(product)"
                        >
                          <span class="fal fa-history me-1"></span>Historial
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-warning btn-sm"
                          title="Ajuste de stock"
                          @click="goAdjust(product)"
                        >
                          <span class="fal fa-balance-scale me-1"></span>Ajuste
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- Cards móvil -->
              <div class="d-md-none">
                <div class="row g-3">
                  <div class="col-12 col-sm-6" v-for="product in products" :key="product.Id">
                    <div class="card h-100">
                      <div class="card-body d-flex flex-column">
                        <div class="d-flex justify-content-between align-items-start mb-1">
                          <small class="text-muted font-monospace">{{ product.ProductCode }}</small>
                          <span class="badge" :class="product.IsActive ? 'bg-success' : 'bg-secondary'">
                            {{ product.IsActive ? 'Activo' : 'Inactivo' }}
                          </span>
                        </div>
                        <h6 class="card-title mb-2">{{ product.ProductName }}</h6>
                        <div class="d-flex justify-content-between align-items-center mb-3">
                          <div>
                            <small class="text-muted">Stock actual</small>
                            <div>
                              <span class="badge fs-6" :class="stockBadgeClass(product)">{{ product.CurrentStock }}</span>
                            </div>
                          </div>
                          <div class="text-end">
                            <small class="text-muted">Mínimo</small>
                            <div><small>{{ product.MinReorderQuantity }}</small></div>
                          </div>
                        </div>
                        <div class="mt-auto d-flex gap-2">
                          <button type="button" class="btn btn-outline-info btn-sm flex-fill" @click="goHistory(product)">
                            <span class="fal fa-history me-1"></span>Historial
                          </button>
                          <button type="button" class="btn btn-outline-warning btn-sm flex-fill" @click="goAdjust(product)">
                            <span class="fal fa-balance-scale me-1"></span>Ajuste
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Paginación -->
              <nav v-if="totalPages > 1" class="d-flex justify-content-center mt-4" aria-label="Paginación">
                <ul class="pagination pagination-sm mb-0">
                  <li class="page-item" :class="{ disabled: currentPage === 1 }">
                    <button class="page-link" @click="goToPage(1)" title="Primera página">
                      <span class="fal fa-chevron-double-left"></span>
                    </button>
                  </li>
                  <li class="page-item" :class="{ disabled: currentPage === 1 }">
                    <button class="page-link" @click="goToPage(currentPage - 1)" title="Anterior">
                      <span class="fal fa-chevron-left"></span>
                    </button>
                  </li>
                  <li
                    v-for="page in pageWindow"
                    :key="page"
                    class="page-item"
                    :class="{ active: page === currentPage }"
                  >
                    <button class="page-link" @click="goToPage(page)">{{ page }}</button>
                  </li>
                  <li class="page-item" :class="{ disabled: currentPage === totalPages }">
                    <button class="page-link" @click="goToPage(currentPage + 1)" title="Siguiente">
                      <span class="fal fa-chevron-right"></span>
                    </button>
                  </li>
                  <li class="page-item" :class="{ disabled: currentPage === totalPages }">
                    <button class="page-link" @click="goToPage(totalPages)" title="Última página">
                      <span class="fal fa-chevron-double-right"></span>
                    </button>
                  </li>
                </ul>
              </nav>

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
import useProduct from '@/modules/inventory/composables/useProduct';
import type { Product } from '@/modules/inventory/models/product.model';

const { getProductsStock } = useProduct();
const router = useRouter();

const products = ref<Product[]>([]);
const search = ref('');
const showOnlyLow = ref(false);
const loading = ref(false);
const currentPage = ref(1);
const pageSize = ref(15);
const totalCount = ref(0);

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)));
const rangeStart = computed(() => totalCount.value === 0 ? 0 : (currentPage.value - 1) * pageSize.value + 1);
const rangeEnd = computed(() => Math.min(currentPage.value * pageSize.value, totalCount.value));

const pageWindow = computed<number[]>(() => {
  const total = totalPages.value;
  const cur = currentPage.value;
  let start = Math.max(1, cur - 2);
  let end = Math.min(total, start + 4);
  start = Math.max(1, end - 4);
  const pages: number[] = [];
  for (let i = start; i <= end; i++) pages.push(i);
  return pages;
});

const stockBadgeClass = (product: Product): string => {
  if (product.CurrentStock === 0) return 'bg-danger';
  if (product.CurrentStock <= product.MinReorderQuantity) return 'bg-danger';
  if (product.CurrentStock <= product.MinReorderQuantity * 1.5) return 'bg-warning text-dark';
  return 'bg-success';
};

const loadPage = async () => {
  loading.value = true;
  const resp = await getProductsStock(search.value, currentPage.value, pageSize.value);
  if (resp.ok) {
    products.value = resp.Data ?? [];
    totalCount.value = resp.TotalCount;
  }
  loading.value = false;
};

const onSearch = () => {
  currentPage.value = 1;
  loadPage();
};

const onPageSizeChange = () => {
  currentPage.value = 1;
  loadPage();
};

const goToPage = (page: number) => {
  if (page < 1 || page > totalPages.value || page === currentPage.value) return;
  currentPage.value = page;
  loadPage();
};

const goHistory = (product: Product) => {
  router.push({ name: 'stock-history', params: { id: product.Id }, query: { name: product.ProductName, code: product.ProductCode } });
};

const goAdjust = (product: Product) => {
  router.push({ name: 'stock-adjustment', params: { id: product.Id }, query: { name: product.ProductName, code: product.ProductCode, stock: product.CurrentStock } });
};

onMounted(loadPage);
</script>

<style scoped></style>
