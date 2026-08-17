<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventarios</li>
        <li class="breadcrumb-item active" aria-current="page">Registro de productos</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>PRODUCTOS</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <!-- Botones de acción -->
            <div class="mt-0 mb-4 d-flex flex-wrap gap-2">
              <button v-if="canCreate" type="button" class="btn btn-sm btn-primary" @click="newProduct">
                <span class="fal fa-plus-square me-1"></span>Nuevo Producto
              </button>
              <button type="button" class="btn btn-sm btn-success" @click="exportProducts">
                <span class="fal fa-file-excel me-1"></span>Exportar Excel
              </button>
              <label v-if="canUpdate" class="btn btn-sm btn-outline-success mb-0" style="cursor:pointer;">
                <span class="fal fa-file-import me-1"></span>Importar Excel
                <input type="file" accept=".xlsx,.xls" class="d-none" ref="fileInputRef" @change="onFileImport" />
              </label>
            </div>

            <!-- Barra de búsqueda -->
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-8 col-lg-7">
                <label class="form-label">Nombre del producto</label>
                <div class="input-group input-group body-bg shadow-inset-2 rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="sa sa-magnifier text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="filtro.nombreProducto"
                    placeholder="Ingrese mínimo 3 caracteres..."
                    autocomplete="off"
                    @keyup.enter="getProducts"
                  />
                  <button class="btn btn-primary" type="button" @click="getProducts">Buscar</button>
                  <button class="btn btn-outline-secondary" type="button" @click="listAllProducts" title="Listar todos los productos">
                    <span class="fal fa-list"></span> Todos
                  </button>
                </div>
              </div>
            </div>

            <!-- Contador de resultados -->
            <div v-if="products.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="fal fa-list me-1"></span>
                <strong>{{ products.length }}</strong> producto(s) encontrado(s)
              </small>
            </div>

            <!-- Estado vacío -->
            <div v-if="products.length === 0" class="text-center py-5">
              <i class="fal fa-box-open fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar productos en el inventario</p>
              <button v-if="canCreate" type="button" class="btn btn-sm btn-outline-primary" @click="newProduct">
                <span class="fal fa-plus me-1"></span>Crear nuevo producto
              </button>
            </div>

            <!-- Resultados -->
            <template v-else>

              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="">
                    <tr>
                      <th>Código</th>
                      <th>Nombre del Producto</th>
                      <th class="d-none d-xl-table-cell">Laboratorio</th>
                      <th class="d-none d-xl-table-cell">Categoría</th>
                      <th class="d-none d-lg-table-cell text-center">U.M.</th>
                      <th class="text-end">Precio</th>
                      <th class="d-none d-lg-table-cell text-center">Stock</th>
                      <th class="text-center">Estado</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="product in products" :key="product.Id">
                      <td><small class="text-muted">{{ product.ProductCode }}</small></td>
                      <td class="fw-semibold">
                        {{ product.ProductName }}
                        <!--
                          Va junto al nombre y no en columna propia: solo lo lleva
                          una minoría de productos, y esta tabla ya esconde
                          columnas por ancho.
                        -->
                        <span v-if="trackingLabel(product)" :class="trackingBadge(product)">
                          {{ trackingLabel(product) }}
                        </span>
                      </td>
                      <td class="d-none d-xl-table-cell">
                        <small class="text-muted">{{ product.LaboratoryName }}</small>
                      </td>
                      <td class="d-none d-xl-table-cell">
                        <small class="text-muted">{{ product.CategoryName }}</small>
                      </td>
                      <td class="d-none d-lg-table-cell text-center">
                        <span class="badge bg-light text-secondary border">{{ product.UnitName }}</span>
                      </td>
                      <td class="text-end fw-semibold text-success">
                        Bs. {{ product.SalePrice.toFixed(2) }}
                      </td>
                      <td class="d-none d-lg-table-cell text-center">
                        <span class="badge" :class="stockBadgeClass(product)">
                          {{ product.CurrentStock }}
                        </span>
                      </td>
                      <td class="text-center">
                        <span class="badge" :class="product.IsActive ? 'bg-success' : 'bg-secondary'">
                          {{ product.IsActive ? 'Activo' : 'Inactivo' }}
                        </span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-sm me-1"
                          :class="canUpdate ? 'btn-outline-primary' : 'btn-outline-secondary'"
                          :title="canUpdate ? 'Editar' : 'Ver detalle (solo lectura)'"
                          @click="editProduct(product)"
                        >
                          <span class="fal" :class="canUpdate ? 'fa-edit' : 'fa-eye'"></span>
                        </button>
                        <button
                          v-if="canDelete"
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="deleteProduct(product)"
                        >
                          <span class="fal fa-trash-alt"></span>
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- Cards (móvil <md) -->
              <div class="d-md-none">
                <div class="row g-3">
                  <div class="col-12 col-sm-6" v-for="product in products" :key="product.Id">
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">

                        <!-- Fila 1: código + estado -->
                        <div class="d-flex justify-content-between align-items-center">
                          <code class="bg-body-secondary rounded px-2 py-1 small">{{ product.ProductCode }}</code>
                          <span
                            class="badge rounded-pill"
                            :class="product.IsActive ? 'text-bg-success' : 'text-bg-secondary'"
                          >
                            {{ product.IsActive ? 'Activo' : 'Inactivo' }}
                          </span>
                        </div>

                        <!-- Fila 2: nombre -->
                        <div>
                          <p class="fw-semibold mb-0 lh-sm">
                            {{ product.ProductName }}
                            <span v-if="trackingLabel(product)" :class="trackingBadge(product)">
                              {{ trackingLabel(product) }}
                            </span>
                          </p>
                        </div>

                        <!-- Fila 3: laboratorio + categoría + unidad -->
                        <div class="d-flex flex-wrap gap-1 align-items-center">
                          <small v-if="product.LaboratoryName" class="text-muted">
                            <i class="fal fa-flask me-1"></i>{{ product.LaboratoryName }}
                          </small>
                          <small v-if="product.CategoryName" class="text-muted">
                            · {{ product.CategoryName }}
                          </small>
                          <span v-if="product.UnitName" class="badge text-bg-light border ms-1">
                            {{ product.UnitName }}
                          </span>
                        </div>

                        <!-- Fila 4: precio y stock -->
                        <div class="d-flex justify-content-between align-items-end mt-1">
                          <div>
                            <div class="fs-6 fw-bold text-success">Bs. {{ product.SalePrice.toFixed(2) }}</div>
                            <div class="text-muted" style="font-size: 0.7rem;">Precio de venta</div>
                          </div>
                          <div class="text-end">
                            <span class="badge rounded-pill px-3" :class="stockBadgeClass(product)">
                              {{ product.CurrentStock }}
                            </span>
                            <div class="text-muted" style="font-size: 0.7rem;">Stock</div>
                          </div>
                        </div>

                        <!-- Fila 5: acciones -->
                        <div class="d-flex gap-2 mt-auto pt-1">
                          <button
                            type="button"
                            class="btn btn-sm flex-grow-1"
                            :class="canUpdate ? 'btn-outline-primary' : 'btn-outline-secondary'"
                            @click="editProduct(product)"
                          >
                            <span class="fal me-1" :class="canUpdate ? 'fa-edit' : 'fa-eye'"></span>
                            {{ canUpdate ? 'Editar' : 'Ver detalle' }}
                          </button>
                          <button
                            v-if="canDelete"
                            type="button"
                            class="btn btn-sm btn-outline-danger"
                            title="Eliminar"
                            @click="deleteProduct(product)"
                          >
                            <span class="fal fa-trash-alt"></span>
                          </button>
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
import { ref, computed } from 'vue';
import useProduct from '@/modules/inventory/composables/useProduct';
import type { Product, ProductBulkUpdate } from '@/modules/inventory/models/product.model';
import utils from '@/utils/msg';
import { exportToExcel, exportTemplateToExcel, readExcel } from '@/utils/excelHelper';
import { useRouter } from "vue-router";
import usePermissions from '@/modules/common/composables/usePermissions';

const { getProductsByName, getAllProducts, bulkUpdateProducts } = useProduct();

/**
 * Qué productos se manejan por lote o por serie. Antes había que entrar a la
 * ficha de cada uno para saberlo, y es justo el dato que decide si una
 * recepción va a pedir datos extra.
 */
const trackingLabel = (product: Product): string => {
  if (product.TrackingMode === 'lot') return 'Lote';
  if (product.TrackingMode === 'serial') return 'Serie';
  return '';
};

/** `subtle` + `emphasis`: son las variantes que el tema oscuro redefine. */
const trackingBadge = (product: Product): string => {
  const base = 'badge border ms-1 fw-normal';
  return product.TrackingMode === 'serial'
    ? `${base} bg-secondary-subtle text-secondary-emphasis border-secondary-subtle`
    : `${base} bg-info-subtle text-info-emphasis border-info-subtle`;
};
const router = useRouter();

const { can } = usePermissions();
const canCreate = computed(() => can('products-admin', 'create'));
const canUpdate = computed(() => can('products-admin', 'update'));
const canDelete = computed(() => can('products-admin', 'delete'));

const filtro = ref({ nombreProducto: '', estado: '1' });
const products = ref<Product[]>([]);
const fileInputRef = ref<HTMLInputElement | null>(null);

const stockBadgeClass = (product: Product): string => {
  if (product.CurrentStock === 0) return 'bg-danger';
  if (product.CurrentStock <= product.MinReorderQuantity) return 'bg-danger';
  if (product.CurrentStock <= product.MinReorderQuantity * 1.5) return 'bg-warning text-dark';
  return 'bg-success';
};

const getProducts = async () => {
  if (filtro.value.nombreProducto.length >= 3) {
    const { ok, Data: productsResp } = await getProductsByName(filtro.value.nombreProducto);
    products.value = productsResp;
    if (products.value.length <= 0 && ok) {
      utils.showMessageModal({ Description: 'No se encontraron productos con ese criterio de búsqueda.', MessageType: 'info' });
    }
  } else {
    utils.showMessageModal({ Description: 'Debe ingresar como mínimo tres caracteres para realizar la búsqueda.', MessageType: 'info' });
  }
};

const BULK_HEADERS = ['Id', 'ProductCode', 'ProductName', 'SalePrice', 'MinReorderQuantity', 'AvailableInPos', 'IsActive', 'BarCode', 'CurrentStock', 'LaboratoryName'];

const exportProducts = () => {
  if (!products.value.length) {
    exportTemplateToExcel(BULK_HEADERS, 'productos_template.xlsx');
    return;
  }

  const rows = products.value.map(p => ({
    Id: p.Id,
    ProductCode: p.ProductCode,
    ProductName: p.ProductName,
    SalePrice: p.SalePrice,
    MinReorderQuantity: p.MinReorderQuantity,
    AvailableInPos: p.AvailableInPos,
    IsActive: p.IsActive,
    BarCode: p.BarCode,
    CurrentStock: p.CurrentStock,
    LaboratoryName: p.LaboratoryName,
  }));

  exportToExcel(rows, 'productos.xlsx');
};

const listAllProducts = async () => {
  filtro.value.nombreProducto = '';
  const { ok, Data } = await getAllProducts();
  if (ok) products.value = Data;
};

const onFileImport = async (event: Event) => {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!fileInputRef.value) return;
  fileInputRef.value.value = '';

  if (!file) return;

  let rows: ProductBulkUpdate[] = [];
  try {
    rows = await readExcel<ProductBulkUpdate>(file);
  } catch {
    utils.showMessageModal({ Description: 'No se pudo leer el archivo Excel.', MessageType: 'error' });
    return;
  }

  if (!rows.length) {
    utils.showMessageModal({ Description: 'El archivo no contiene datos.', MessageType: 'info' });
    return;
  }

  const invalid = rows.filter(r => !r.Id || !/^[0-9a-fA-F-]{36}$/.test(String(r.Id)));
  if (invalid.length) {
    utils.showMessageModal({ Description: `${invalid.length} fila(s) no tienen un UUID válido en la columna Id.`, MessageType: 'error' });
    return;
  }

  const confirmed = await utils.showMessageQuestion(
    `¿Confirma actualizar ${rows.length} producto(s) en masa? Esta acción sobreescribirá: nombre, precio, cantidad mínima, disponibilidad en POS, estado y código de barras.`
  );
  if (!confirmed) return;

  const { ok, Data, Message } = await bulkUpdateProducts(rows);
  if (ok) {
    utils.showMessageModal({ Description: `Se actualizaron ${Data} producto(s) correctamente.`, MessageType: 'success' });
    if (filtro.value.nombreProducto.length >= 3) await getProducts();
  } else {
    utils.showMessageModal(Message);
  }
};

const editProduct = (product: Product) => {
  router.push({ name: 'product-edit', params: { id: product.Id } });
};

const deleteProduct = async (product: Product) => {
  const confirmed = await utils.showMessageQuestion(`¿Desea eliminar el producto "${product.ProductName}"?`);
  if (confirmed) {
    // TODO: implement delete
  }
};

const newProduct = () => {
  router.push({ name: 'product-edit', params: { id: '0' } });
};
</script>

<style scoped></style>
