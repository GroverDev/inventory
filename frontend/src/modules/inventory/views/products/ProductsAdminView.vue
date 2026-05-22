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

            <!-- Botón Nuevo -->
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newProduct">
                <span class="fal fa-plus-square me-1"></span>Nuevo Producto
              </button>
            </div>

            <!-- Barra de búsqueda -->
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
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
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newProduct">
                <span class="fal fa-plus me-1"></span>Crear nuevo producto
              </button>
            </div>

            <!-- Resultados -->
            <template v-else>

              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th>Código</th>
                      <th>Nombre del Producto</th>
                      <th class="d-none d-xl-table-cell">Laboratorio</th>
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
                      <td class="fw-semibold">{{ product.ProductName }}</td>
                      <td class="d-none d-xl-table-cell">
                        <small class="text-muted">{{ product.LaboratoryName }}</small>
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
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Editar"
                          @click="editProduct(product)"
                        >
                          <span class="fal fa-edit"></span>
                        </button>
                        <button
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
                    <div class="card h-100">
                      <div class="card-body d-flex flex-column">
                        <div class="d-flex justify-content-between align-items-start mb-1">
                          <small class="text-muted">{{ product.ProductCode }}</small>
                          <span class="badge" :class="product.IsActive ? 'bg-success' : 'bg-secondary'">
                            {{ product.IsActive ? 'Activo' : 'Inactivo' }}
                          </span>
                        </div>
                        <h6 class="card-title mb-1">{{ product.ProductName }}</h6>
                        <small class="text-muted mb-2">{{ product.LaboratoryName }}</small>
                        <div class="d-flex justify-content-between align-items-center mb-3">
                          <span class="fw-semibold text-success">Bs. {{ product.SalePrice.toFixed(2) }}</span>
                          <span class="d-flex align-items-center gap-1">
                            <small class="text-muted">Stock:</small>
                            <span class="badge" :class="stockBadgeClass(product)">{{ product.CurrentStock }}</span>
                          </span>
                        </div>
                        <div class="mt-auto">
                          <div class="btn-group w-100" role="group">
                            <button type="button" class="btn btn-outline-primary btn-sm" @click="editProduct(product)">
                              <span class="fal fa-edit me-1"></span>Editar
                            </button>
                            <button type="button" class="btn btn-outline-danger btn-sm" @click="deleteProduct(product)">
                              <span class="fal fa-trash-alt me-1"></span>Eliminar
                            </button>
                          </div>
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
import { ref } from 'vue';
import useProduct from '@/modules/inventory/composables/useProduct';
import type { Product } from '@/modules/inventory/models/product.model';
import utils from '@/utils/msg';
import { useRouter } from "vue-router";

const { getProductsByName } = useProduct();
const router = useRouter();

const filtro = ref({
  nombreProducto: '',
  estado: '1',
});
const products = ref<Product[]>([]);

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
