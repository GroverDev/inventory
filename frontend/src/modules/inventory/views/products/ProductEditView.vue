<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventarios</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de productos</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">
          {{ product.Id !== '0' ? 'Editar Producto' : 'Nuevo Producto' }}
        </li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="row">
        <div class="col">
          <div id="panel-1" class="panel panel-icon">
            <div class="panel-hdr">
              <h2>
                {{ product.Id !== '0' ? 'Editar' : 'Nuevo' }}
                <span class="fw-300"><i> Producto</i></span>
              </h2>
              <span
                v-if="product.Id !== '0'"
                class="badge ms-2"
                :class="product.IsActive ? 'bg-success' : 'bg-secondary'"
              >
                {{ product.IsActive ? 'Activo' : 'Inactivo' }}
              </span>
            </div>
            <div class="panel-container show">

              <!-- Barra de acciones -->
              <div class="panel-content pt-0">
                <div class="row align-items-center">
                  <div class="col-8 col-md-8">
                    <div class="d-md-none">
                      <div class="btn-group">
                        <button type="button" class="btn btn-primary dropdown-toggle"
                          data-bs-toggle="dropdown" data-bs-display="static" aria-expanded="false">
                          Opciones
                        </button>
                        <div class="dropdown-menu dropdown-menu-lg-right">
                          <button v-if="canSave" type="button" class="dropdown-item border-bottom border-1"
                            :disabled="isSaved" @click="saveProduct">
                            <span class="fal fa-save me-1"></span>Grabar
                          </button>
                          <button type="button" class="dropdown-item border-bottom border-1"
                            @click="returnPage">
                            <span class="fal fa-ban me-1"></span>Cancelar
                          </button>
                        </div>
                      </div>
                    </div>
                    <div class="d-none d-md-flex gap-2">
                      <button v-if="canSave" type="button" class="btn btn-sm btn-primary"
                        :disabled="isSaved" @click="saveProduct">
                        <span class="fal fa-save me-1"></span>Grabar
                      </button>
                      <button type="button" class="btn btn-warning btn-sm"
                        @click="returnPage">
                        <span class="fal fa-ban me-1"></span>Cancelar
                      </button>
                    </div>
                  </div>
                  <div class="col-4 col-md-4 text-md-end">
                    <button type="button" class="btn btn-danger btn-sm" @click="returnPage">
                      <span class="fal fa-arrow-alt-to-left me-1"></span>Volver
                    </button>
                  </div>
                </div>
              </div>

              <!-- Formulario -->
              <div class="panel-content pt-0">
                <form novalidate>

                  <!-- Sección 1: Identificación -->
                  <h6 class="text-muted border-bottom pb-2 mb-3">
                    <i class="fal fa-id-badge me-1"></i> Identificación del Producto
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="ProductCode">
                        Código del Producto <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="ProductCode"
                        name="ProductCode"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.ProductCode.$dirty && v$.ProductCode.$invalid }"
                        placeholder="Ej: PROD-001"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.ProductCode.$model"
                      />
                      <small class="invalid-feedback">Debe ingresar el código del Producto.</small>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="BarCode">
                        Código de Barras <span class="text-danger">*</span>
                      </label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">
                          <i class="fal fa-barcode"></i>
                        </span>
                        <input
                          type="text"
                          id="BarCode"
                          name="BarCode"
                          class="form-control"
                          :class="{ 'is-invalid': v$.BarCode.$dirty && v$.BarCode.$invalid }"
                          placeholder="Código de barras"
                          :disabled="isSaved"
                          autocomplete="off"
                          v-model.trim="v$.BarCode.$model"
                        />
                        <div class="invalid-feedback">Debe ingresar el código de barra.</div>
                      </div>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="ProductName">
                        Nombre del Producto <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="ProductName"
                        name="ProductName"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.ProductName.$dirty && v$.ProductName.$invalid }"
                        placeholder="Nombre del Producto"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.ProductName.$model"
                      />
                      <small class="invalid-feedback">Debe ingresar el nombre del Producto.</small>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="Description">
                        Descripción <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="Description"
                        name="Description"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.Description.$dirty && v$.Description.$invalid }"
                        placeholder="Descripción del producto"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.Description.$model"
                      />
                      <small class="invalid-feedback">Debe ingresar la descripción del Producto.</small>
                    </div>
                  </div>

                  <!-- Sección 2: Clasificación -->
                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-tags me-1"></i> Clasificación
                  </h6>
                  <div class="row">
                    <div class="col-md-4 mb-3">
                      <label class="form-label" for="laboratories">
                        Laboratorio / Proveedor <span class="text-danger">*</span>
                      </label>
                      <select
                        class="form-select form-select-sm"
                        :class="{ 'is-invalid': v$.LaboratoryId.$dirty && v$.LaboratoryId.$invalid }"
                        id="laboratories"
                        name="laboratories"
                        :disabled="isSaved"
                        v-model.trim="v$.LaboratoryId.$model"
                      >
                        <option value="">— Seleccione un laboratorio —</option>
                        <option v-for="lab in laboratories" :value="lab.Id" :key="lab.Id">
                          {{ lab.LaboratoryName }}
                        </option>
                      </select>
                      <small class="invalid-feedback">Debe seleccionar un Laboratorio.</small>
                    </div>
                    <div class="col-md-4 mb-3">
                      <label class="form-label" for="categories">
                        Categoría
                      </label>
                      <select
                        class="form-select form-select-sm"
                        id="categories"
                        name="categories"
                        :disabled="isSaved"
                        v-model.trim="product.CategoryId"
                      >
                        <option value="">— Seleccione una categoría —</option>
                        <option v-for="cat in categories" :value="cat.Id" :key="cat.Id">
                          {{ cat.CategoryName }}
                        </option>
                      </select>
                    </div>
                    <div class="col-md-4 mb-3">
                      <label class="form-label" for="units">
                        Unidad de Medida <span class="text-danger">*</span>
                      </label>
                      <select
                        class="form-select form-select-sm"
                        :class="{ 'is-invalid': v$.UomId.$dirty && v$.UomId.$invalid }"
                        id="units"
                        name="units"
                        :disabled="isSaved"
                        v-model.trim="v$.UomId.$model"
                      >
                        <option value="">— Seleccione una unidad —</option>
                        <option v-for="unit in unitsOfMeasurement" :value="unit.Id" :key="unit.Id">
                          {{ unit.UnitName }}
                        </option>
                      </select>
                      <small class="invalid-feedback">Debe seleccionar una unidad de medida.</small>
                    </div>
                  </div>

                  <!-- Sección 3: Precio y Stock -->
                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-coins me-1"></i> Precio y Stock
                  </h6>
                  <div class="row">
                    <div class="col-sm-6 col-md-4 mb-3">
                      <label class="form-label d-block" for="precio">
                        Precio de Venta <span class="text-danger">*</span>
                      </label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">Bs.</span>
                        <input
                          type="number"
                          id="precio"
                          name="precio"
                          class="form-control text-end"
                          :class="{ 'is-invalid': v$.SalePrice.$dirty && v$.SalePrice.$invalid }"
                          placeholder="0.00"
                          step="0.01"
                          min="0"
                          :disabled="isSaved"
                          v-model.trim="v$.SalePrice.$model"
                        />
                        <div class="invalid-feedback"
                          v-if="v$.SalePrice.$dirty && v$.SalePrice.required.$invalid">
                          {{ v$.SalePrice.required.$message }}
                        </div>
                        <div class="invalid-feedback"
                          v-else-if="v$.SalePrice.$dirty && v$.SalePrice.greaterThanZero.$invalid">
                          {{ v$.SalePrice.greaterThanZero.$message }}
                        </div>
                      </div>
                    </div>
                    <div class="col-sm-6 col-md-4 mb-3">
                      <label class="form-label d-block" for="cantidad">
                        Stock Actual
                        <i class="fal fa-lock ms-1 text-muted small"
                          title="Solo lectura — se actualiza por movimientos de inventario"></i>
                      </label>
                      <input
                        type="number"
                        id="cantidad"
                        name="cantidad"
                        class="form-control form-control-sm text-end"
                        :class="stockClass"
                        placeholder="0"
                        :disabled="true"
                        v-model.trim="v$.CurrentStock.$model"
                      />
                      <small
                        v-if="product.CurrentStock <= product.MinReorderQuantity && product.MinReorderQuantity > 0"
                        class="text-danger"
                      >
                        <i class="fal fa-exclamation-triangle me-1"></i>Por debajo del stock mínimo
                      </small>
                    </div>
                    <div class="col-sm-6 col-md-4 mb-3">
                      <label class="form-label d-block" for="cantidadReposicion">
                        Stock Mínimo de Reposición <span class="text-danger">*</span>
                      </label>
                      <input
                        type="number"
                        id="cantidadReposicion"
                        name="cantidadReposicion"
                        class="form-control form-control-sm text-end"
                        :class="{ 'is-invalid': v$.MinReorderQuantity.$dirty && v$.MinReorderQuantity.$invalid }"
                        placeholder="0"
                        min="0"
                        :disabled="isSaved"
                        v-model.trim="v$.MinReorderQuantity.$model"
                      />
                      <small class="invalid-feedback"
                        v-if="v$.MinReorderQuantity.$dirty && v$.MinReorderQuantity.required.$invalid">
                        {{ v$.MinReorderQuantity.required.$message }}
                      </small>
                      <small class="invalid-feedback"
                        v-else-if="v$.MinReorderQuantity.$dirty && v$.MinReorderQuantity.greaterThanZero.$invalid">
                        {{ v$.MinReorderQuantity.greaterThanZero.$message }}
                      </small>
                    </div>
                  </div>

                  <!-- Sección 4: Configuración -->
                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-sliders-h me-1"></i> Configuración
                  </h6>
                  <div class="row">
                    <div class="col-sm-6 mb-3">
                      <div class="form-check form-switch">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          id="AvailableInPos"
                          role="switch"
                          :disabled="isSaved"
                          v-model="v$.AvailableInPos.$model"
                        />
                        <label class="form-check-label" for="AvailableInPos">
                          Disponible en los puntos de venta
                        </label>
                      </div>
                    </div>
                    <div class="col-sm-6 mb-3">
                      <div class="form-check form-switch">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          id="IsActive"
                          role="switch"
                          :disabled="isSaved"
                          v-model="product.IsActive"
                        />
                        <label class="form-check-label" for="IsActive">
                          Producto activo
                        </label>
                      </div>
                    </div>
                  </div>

                </form>
              </div>

            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from "vue-router";
import useVuelidate from '@vuelidate/core';
import { helpers, required } from '@vuelidate/validators';
import utils from '@/utils/msg';

import { Product } from '@/modules/inventory/models/product.model';
import { Laboratory } from '@/modules/inventory/models/laboratory.model';
import { Category } from '@/modules/inventory/models/category.model';
import { UnitOfMeasurement } from '@/modules/inventory/models/unitOfMeasurement.model';

import useProduct from '@/modules/inventory/composables/useProduct';
import useLaboratory from '@/modules/inventory/composables/useLaboratory';
import useCategory from '@/modules/inventory/composables/useCategory';
import useUnitOfMeasurement from '@/modules/inventory/composables/useUnitOfMeasurement';
import usePermissions from '@/modules/common/composables/usePermissions';

const router = useRouter();

const { getProductById, updateProduct, createProduct } = useProduct();
const { getLaboratories: fetchLaboratories } = useLaboratory();
const { getCategories: fetchCategories } = useCategory();
const { getUnitsOfMeasurement: fetchUnitsOfMeasurement } = useUnitOfMeasurement();

const product = ref(new Product());
const laboratories = ref<Laboratory[]>([]);
const categories = ref<Category[]>([]);
const unitsOfMeasurement = ref<UnitOfMeasurement[]>([]);

// Validador personalizado para mayor a cero
const greaterThanZero = (value: number) => value > 0;

const rules = {
  ProductCode: { required },
  BarCode: { required },
  ProductName: { required },
  Description: { required },
  SalePrice: {
    required: helpers.withMessage('Debe ingresar el precio del Producto.', required),
    greaterThanZero: helpers.withMessage('El valor debe ser mayor a cero', greaterThanZero),
  },
  CurrentStock: { required },
  MinReorderQuantity: {
    required: helpers.withMessage('Debe ingresar la cantidad mínima de reposición.', required),
    greaterThanZero: helpers.withMessage('El valor debe ser mayor a cero', greaterThanZero),
  },
  LaboratoryId: { required },
  UomId: { required },
  AvailableInPos: { required },
};
const v$ = useVuelidate(rules, product);

try {
  product.value.Id = router.currentRoute.value.params.id
    ? router.currentRoute.value.params.id.toString()
    : '0';
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
} catch (error) {
  product.value.Id = '0';
}

const isSaved = ref(false);

const { can } = usePermissions();
// Permiso efectivo para grabar: crear si es nuevo, actualizar si es edición.
const canSave = computed(() =>
  product.value.Id === '0'
    ? can('products-admin', 'create')
    : can('products-admin', 'update')
);

// Clase dinámica para el campo Stock Actual según nivel vs mínimo
const stockClass = computed((): string => {
  if (!product.value.MinReorderQuantity) return '';
  if (product.value.CurrentStock === 0) return 'text-danger fw-bold';
  if (product.value.CurrentStock <= product.value.MinReorderQuantity) return 'text-danger';
  if (product.value.CurrentStock <= product.value.MinReorderQuantity * 1.5) return 'text-warning';
  return 'text-success';
});

onMounted(async () => {
  if (product.value.Id !== '0') {
    await getProductXId(product.value.Id);
  }
  await getLaboratories();
  await getCategories();
  await getUnitsOfMeasurement();
});

const getProductXId = async (productId: string) => {
  const { ok, Data: productResp } = await getProductById(productId);
  if (ok) product.value = productResp;
};

const getLaboratories = async () => {
  const { ok, Data: laboratoriesResp } = await fetchLaboratories('');
  if (ok) laboratories.value = laboratoriesResp;
};

const getCategories = async () => {
  const { ok, Data: categoriesResp } = await fetchCategories('');
  if (ok) categories.value = categoriesResp;
};

const getUnitsOfMeasurement = async () => {
  const { ok, Data: unitsResp } = await fetchUnitsOfMeasurement('');
  if (ok) unitsOfMeasurement.value = unitsResp;
};

const returnPage = () => {
  router.push({ name: 'products-admin' });
};

const saveProduct = async () => {
  if (!canSave.value) {
    await utils.showMessageModal({ Description: 'No tiene permiso para guardar productos.', MessageType: 'warning' });
    return;
  }
  if (!v$.value.$invalid) {
    const respuesta = await utils.showMessageQuestion('¿Desea guardar el producto?');
    if (respuesta) {
      if (product.value.Id === '0') {
        const { ok, Data: idProduct } = await createProduct(product.value);
        if (ok) {
          isSaved.value = ok;
          product.value.Id = idProduct;
          await utils.showMessageModal({ Description: 'El producto se creó correctamente.', MessageType: 'success' });
        }
      } else {
        const { ok, Data: okResp } = await updateProduct(product.value);
        if (ok) {
          isSaved.value = okResp;
          if (okResp) {
            await utils.showMessageModal({ Description: 'El producto se actualizó correctamente.', MessageType: 'success' });
          }
        }
      }
    }
  } else {
    v$.value.$touch();
  }
};
</script>

<style scoped></style>
