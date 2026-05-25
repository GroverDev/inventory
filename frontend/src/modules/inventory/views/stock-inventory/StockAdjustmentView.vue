<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventarios</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="router.back()">Control de Stock</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">Ajuste de Stock</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="row">
        <div class="col-12 col-lg-7">
          <div class="panel panel-icon">
            <div class="panel-hdr">
              <h2>Ajuste de <span class="fw-300"><i>STOCK</i></span></h2>
            </div>
            <div class="panel-container show">

              <!-- Barra de acciones -->
              <div class="panel-content pt-0">
                <div class="row align-items-center">
                  <div class="col-8">
                    <div class="d-none d-md-flex gap-2">
                      <button type="button" class="btn btn-sm btn-primary" :disabled="saved" @click="saveAdjustment">
                        <span class="fal fa-save me-1"></span>Aplicar Ajuste
                      </button>
                      <button type="button" class="btn btn-warning btn-sm" @click="router.back()">
                        <span class="fal fa-ban me-1"></span>Cancelar
                      </button>
                    </div>
                    <div class="d-md-none">
                      <div class="btn-group">
                        <button type="button" class="btn btn-primary dropdown-toggle"
                          data-bs-toggle="dropdown" data-bs-display="static" aria-expanded="false">
                          Opciones
                        </button>
                        <div class="dropdown-menu">
                          <button type="button" class="dropdown-item" :disabled="saved" @click="saveAdjustment">
                            <span class="fal fa-save me-1"></span>Aplicar Ajuste
                          </button>
                          <button type="button" class="dropdown-item" @click="router.back()">
                            <span class="fal fa-ban me-1"></span>Cancelar
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="col-4 text-end">
                    <button type="button" class="btn btn-danger btn-sm" @click="router.back()">
                      <span class="fal fa-arrow-alt-to-left me-1"></span>Volver
                    </button>
                  </div>
                </div>
              </div>

              <!-- Info del producto -->
              <div class="panel-content pt-0">
                <div class="alert alert-light border mb-3 py-2">
                  <div class="d-flex align-items-center gap-3">
                    <i class="fal fa-box fa-2x text-primary"></i>
                    <div>
                      <div class="fw-semibold">{{ productName }}</div>
                      <small class="text-muted font-monospace">{{ productCode }}</small>
                    </div>
                    <div class="ms-auto text-end">
                      <small class="text-muted d-block">Stock actual</small>
                      <span class="badge fs-6" :class="currentStockNum <= 0 ? 'bg-danger' : 'bg-info'">
                        {{ currentStockNum }}
                      </span>
                    </div>
                  </div>
                </div>

                <!-- Formulario -->
                <form novalidate @submit.prevent>
                  <h6 class="text-muted border-bottom pb-2 mb-3">
                    <i class="fal fa-balance-scale me-1"></i> Datos del Ajuste
                  </h6>

                  <div class="row">
                    <!-- Tipo de ajuste / Cantidad -->
                    <div class="col-12 col-sm-4 mb-3">
                      <label class="form-label">Tipo <span class="text-danger">*</span></label>
                      <select class="form-select form-select-sm" v-model="adjustType" :disabled="saved" @change="onTypeChange">
                        <option value="increase">Entrada (+)</option>
                        <option value="decrease">Salida (-)</option>
                      </select>
                    </div>

                    <div class="col-12 col-sm-4 mb-3">
                      <label class="form-label">Cantidad <span class="text-danger">*</span></label>
                      <input
                        type="number"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.Quantity.$dirty && v$.Quantity.$invalid }"
                        min="1"
                        v-model.number="v$.Quantity.$model"
                        :disabled="saved"
                      />
                      <small class="invalid-feedback">Ingrese una cantidad mayor a 0.</small>
                    </div>

                    <div class="col-12 col-sm-4 mb-3">
                      <label class="form-label">Stock resultante</label>
                      <input
                        type="number"
                        class="form-control form-control-sm"
                        :value="stockAfterPreview"
                        disabled
                        :class="stockAfterPreview < 0 ? 'text-danger border-danger' : ''"
                      />
                      <small v-if="stockAfterPreview < 0" class="text-danger">Stock insuficiente</small>
                    </div>

                    <!-- Motivo -->
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label">Motivo <span class="text-danger">*</span></label>
                      <select
                        class="form-select form-select-sm"
                        :class="{ 'is-invalid': v$.Reason.$dirty && v$.Reason.$invalid }"
                        v-model="v$.Reason.$model"
                        :disabled="saved"
                      >
                        <option value="">-- Seleccione --</option>
                        <option value="Conteo físico">Conteo físico</option>
                        <option value="Ingreso manual">Ingreso manual</option>
                        <option value="Merma/Pérdida">Merma/Pérdida</option>
                        <option value="Devolución de cliente">Devolución de cliente</option>
                        <option value="Devolución a proveedor">Devolución a proveedor</option>
                        <option value="Corrección de error">Corrección de error</option>
                        <option value="Vencimiento de producto">Vencimiento de producto</option>
                      </select>
                      <small class="invalid-feedback">Seleccione un motivo.</small>
                    </div>

                    <!-- Observación -->
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label">Observación</label>
                      <input
                        type="text"
                        class="form-control form-control-sm"
                        placeholder="Observación opcional"
                        v-model.trim="form.Observation"
                        :disabled="saved"
                        autocomplete="off"
                      />
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
import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import useVuelidate from '@vuelidate/core';
import { required, minValue } from '@vuelidate/validators';
import utils from '@/utils/msg';
import useStockMovement from '@/modules/inventory/composables/useStockMovement';
import { StockAdjustmentRequest } from '@/modules/inventory/models/stockMovement.model';

const route = useRoute();
const router = useRouter();
const { createAdjustment } = useStockMovement();

const productId = route.params.id as string;
const productName = route.query.name as string ?? '';
const productCode = route.query.code as string ?? '';
const currentStockNum = ref(Number(route.query.stock ?? 0));

const saved = ref(false);
const adjustType = ref<'increase' | 'decrease'>('increase');

const form = ref(new StockAdjustmentRequest());
form.value.ProductId = productId;

const rules = computed(() => ({
  Quantity: { required, minValue: minValue(1) },
  Reason: { required },
}));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const v$ = useVuelidate(rules, form as any);

const onTypeChange = () => {
  // keep absolute quantity in the field; sign is applied on save
};

const stockAfterPreview = computed(() => {
  const qty = form.value.Quantity > 0 ? form.value.Quantity : 0;
  return adjustType.value === 'increase'
    ? currentStockNum.value + qty
    : currentStockNum.value - qty;
});

const saveAdjustment = async () => {
  const valid = await v$.value.$validate();
  if (!valid) return;
  if (stockAfterPreview.value < 0) {
    utils.showMessageModal({ Description: 'El stock resultante no puede ser negativo.', MessageType: 'warning' });
    return;
  }

  const confirmed = await utils.showMessageQuestion('¿Desea aplicar el ajuste de stock?');
  if (!confirmed) return;

  const request = new StockAdjustmentRequest();
  request.ProductId = productId;
  request.Quantity = adjustType.value === 'increase' ? form.value.Quantity : -form.value.Quantity;
  request.Reason = form.value.Reason;
  request.Observation = form.value.Observation;

  const { ok } = await createAdjustment(request);
  if (ok) {
    saved.value = true;
    await utils.showMessageModal({ Description: 'El ajuste se aplicó correctamente.', MessageType: 'success' });
    router.push({ name: 'inventory-stock' });
  }
};
</script>

<style scoped></style>
