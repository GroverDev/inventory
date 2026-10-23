<template>
  <Layout>
    <PageHeader title="Dar de Baja" page-title="Vencimientos" />


    <div class="row">
        <div class="col-12 col-lg-7">
          <div class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">Dar de <span class="fw-light"><i>BAJA</i></span></h5>
            </div>
            <!-- Barra de acciones -->
              <div class="pt-0 card-body">
                <div class="row align-items-center">
                  <div class="col-8">
                    <div class="d-none d-md-flex gap-2">
                      <button type="button" class="btn btn-sm btn-danger" :disabled="saved" @click="save">
                        <span class="ri-delete-bin-line me-1"></span>Dar de Baja
                      </button>
                      <button type="button" class="btn btn-warning btn-sm" @click="router.back()">
                        <span class="ri-prohibited-line me-1"></span>Cancelar
                      </button>
                    </div>
                    <div class="d-md-none">
                      <div class="btn-group">
                        <button type="button" class="btn btn-primary dropdown-toggle"
                          data-bs-toggle="dropdown" data-bs-display="static" aria-expanded="false">
                          Opciones
                        </button>
                        <div class="dropdown-menu">
                          <button type="button" class="dropdown-item" :disabled="saved" @click="save">
                            <span class="ri-delete-bin-line me-1"></span>Dar de Baja
                          </button>
                          <button type="button" class="dropdown-item" @click="router.back()">
                            <span class="ri-prohibited-line me-1"></span>Cancelar
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="col-4 text-end">
                    <button type="button" class="btn btn-danger btn-sm" @click="router.back()">
                      <span class="ri-arrow-left-line me-1"></span>Volver
                    </button>
                  </div>
                </div>
              </div>

              <!-- Info del lote -->
              <div class="pt-0 card-body">
                <div class="alert alert-warning border mb-3 py-2">
                  <div class="d-flex align-items-center gap-3">
                    <i class="ri-error-warning-line ri-2x"></i>
                    <div>
                      <div class="fw-semibold">{{ productName }}</div>
                      <small class="text-muted font-monospace">{{ productCode }}</small>
                      <div>
                        <code class="bg-body-secondary rounded px-2 py-1 small">{{ lot || '(sin lote)' }}</code>
                        <small class="ms-2 text-muted">vence {{ formatDate(expiry) }}</small>
                      </div>
                    </div>
                    <div class="ms-auto text-end">
                      <small class="text-muted d-block">Cantidad disponible</small>
                      <span class="badge fs-6 bg-danger">{{ availableQty }}</span>
                    </div>
                  </div>
                </div>

                <!-- Formulario -->
                <form novalidate @submit.prevent>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label">Cantidad a dar de baja <span class="text-danger">*</span></label>
                      <input
                        type="number"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.Quantity.$dirty && v$.Quantity.$invalid }"
                        min="1"
                        :max="availableQty"
                        v-model.number="v$.Quantity.$model"
                        :disabled="saved"
                      />
                      <small class="invalid-feedback">Ingrese una cantidad entre 1 y {{ availableQty }}.</small>
                    </div>

                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label">Motivo <span class="text-danger">*</span></label>
                      <select
                        class="form-select form-select-sm"
                        :class="{ 'is-invalid': v$.Reason.$dirty && v$.Reason.$invalid }"
                        v-model="v$.Reason.$model"
                        :disabled="saved"
                      >
                        <option value="">-- Seleccione --</option>
                        <option value="Vencimiento">Vencimiento</option>
                        <option value="Producto dañado">Producto dañado</option>
                        <option value="Retiro de lote">Retiro de lote</option>
                      </select>
                      <small class="invalid-feedback">Seleccione un motivo.</small>
                    </div>

                    <div class="col-12 mb-3">
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
    
  </Layout>
</template>

<script setup lang="ts">
import Layout from '@/layouts/main.vue'
import PageHeader from '@/components/page-header.vue'
import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import useVuelidate from '@vuelidate/core';
import { required, minValue, maxValue } from '@vuelidate/validators';
import utils from '@/utils/msg';
import useStockMovement from '@/modules/inventory/composables/useStockMovement';
import { StockWriteOffRequest } from '@/modules/inventory/models/stockMovement.model';

const route = useRoute();
const router = useRouter();
const { createWriteOff } = useStockMovement();

// :id es el StockItemId (la existencia/lote puntual), no el ProductId.
const stockItemId = route.params.id as string;
const productId = route.query.productId as string ?? '';
const productName = route.query.name as string ?? '';
const productCode = route.query.code as string ?? '';
const lot = route.query.lot as string ?? '';
const expiry = route.query.expiry as string ?? '';
const availableQty = Number(route.query.quantity ?? 0);

const saved = ref(false);
const form = ref(new StockWriteOffRequest());
form.value.ProductId = productId;
form.value.StockItemId = stockItemId;
form.value.Quantity = availableQty;

const rules = computed(() => ({
  Quantity: { required, minValue: minValue(1), maxValue: maxValue(availableQty) },
  Reason: { required },
}));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const v$ = useVuelidate(rules, form as any);

const formatDate = (date: string): string => {
  if (!date) return '—';
  return new Date(date).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const save = async () => {
  const valid = await v$.value.$validate();
  if (!valid) return;

  const confirmed = await utils.showMessageQuestion(
    `¿Dar de baja ${form.value.Quantity} unidad(es) del lote "${lot || 'sin lote'}"? Esta acción no se puede deshacer.`
  );
  if (!confirmed) return;

  const { ok } = await createWriteOff(form.value);
  if (ok) {
    saved.value = true;
    await utils.showMessageModal({ Description: 'La baja se registró correctamente.', MessageType: 'success' });
    router.push({ name: 'stock-expiry' });
  }
};
</script>

<style scoped></style>
