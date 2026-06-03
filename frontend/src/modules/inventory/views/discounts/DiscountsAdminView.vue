<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item active" aria-current="page">Gestión de Descuentos</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>DESCUENTOS</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="openModal()">
                <span class="fal fa-plus-square me-1"></span>Nuevo Descuento
              </button>
            </div>

            <!-- Estado vacío -->
            <div v-if="discounts.length === 0 && !loading" class="text-center py-5">
              <i class="fal fa-tags fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">No hay descuentos registrados.</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="openModal()">
                <span class="fal fa-plus me-1"></span>Crear primer descuento
              </button>
            </div>

            <div v-else-if="loading" class="text-center py-5">
              <i class="fal fa-spinner fa-spin fa-2x text-muted"></i>
            </div>

            <template v-else>
              <!-- Tabla desktop -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="">
                    <tr>
                      <th>Nombre</th>
                      <th>Tipo</th>
                      <th class="text-end">Valor</th>
                      <th class="d-none d-lg-table-cell">Descripción</th>
                      <th class="text-center">Activo</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="d in discounts" :key="d.Id">
                      <td class="fw-semibold">{{ d.Name }}</td>
                      <td>
                        <span class="badge"
                          :class="d.Type === 'Percentage' ? 'bg-info-subtle text-info' : 'bg-warning-subtle text-warning'">
                          <i :class="d.Type === 'Percentage' ? 'fal fa-percent' : 'fal fa-dollar-sign'" class="me-1"></i>
                          {{ d.Type === 'Percentage' ? 'Porcentaje' : 'Monto fijo' }}
                        </span>
                      </td>
                      <td class="text-end fw-semibold">
                        {{ d.Type === 'Percentage' ? d.Value + '%' : 'Bs. ' + formatNum(d.Value) }}
                      </td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ d.Description || '—' }}</small>
                      </td>
                      <td class="text-center">
                        <span :class="d.IsActive ? 'badge bg-success' : 'badge bg-secondary'">
                          {{ d.IsActive ? 'Sí' : 'No' }}
                        </span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button type="button" class="btn btn-outline-primary btn-sm me-1" title="Editar" @click="openModal(d)">
                          <span class="fal fa-edit"></span>
                        </button>
                        <button type="button" class="btn btn-outline-danger btn-sm" title="Eliminar" @click="remove(d.Id)">
                          <span class="fal fa-trash-alt"></span>
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- Cards móvil -->
              <div class="d-md-none">
                <div class="row g-3">
                  <div class="col-12 col-sm-6" v-for="d in discounts" :key="d.Id">
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <p class="fw-semibold mb-0 lh-sm">{{ d.Name }}</p>
                          <span class="badge rounded-pill" :class="d.IsActive ? 'text-bg-success' : 'text-bg-secondary'">
                            {{ d.IsActive ? 'Activo' : 'Inactivo' }}
                          </span>
                        </div>
                        <div class="d-flex align-items-center gap-2">
                          <span class="badge" :class="d.Type === 'Percentage' ? 'bg-info-subtle text-info' : 'bg-warning-subtle text-warning'">
                            {{ d.Type === 'Percentage' ? 'Porcentaje' : 'Monto fijo' }}
                          </span>
                          <span class="fw-bold text-primary">
                            {{ d.Type === 'Percentage' ? d.Value + '%' : 'Bs. ' + formatNum(d.Value) }}
                          </span>
                        </div>
                        <small class="text-muted">{{ d.Description || '—' }}</small>
                        <div class="d-flex gap-2 mt-auto pt-1">
                          <button type="button" class="btn btn-sm btn-outline-primary flex-grow-1" @click="openModal(d)">
                            <span class="fal fa-edit me-1"></span>Editar
                          </button>
                          <button type="button" class="btn btn-sm btn-outline-danger" @click="remove(d.Id)">
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

    <!-- ══ MODAL: Crear / Editar descuento ══ -->
    <div v-if="showModal" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,.5)">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title fw-bold">
              <i class="fal fa-tag me-2"></i>
              {{ form.Id ? 'Editar Descuento' : 'Nuevo Descuento' }}
            </h5>
            <button type="button" class="btn-close" @click="closeModal"></button>
          </div>
          <div class="modal-body">

            <div class="row g-3">
              <!-- Nombre -->
              <div class="col-12">
                <label class="form-label">Nombre <span class="text-danger">*</span></label>
                <input
                  type="text" class="form-control form-control-sm"
                  :class="{ 'is-invalid': formErrors.Name }"
                  v-model.trim="form.Name"
                  placeholder="Ej: Descuento tercera edad"
                  maxlength="100"
                />
                <div class="invalid-feedback">El nombre es requerido.</div>
              </div>

              <!-- Tipo -->
              <div class="col-12 col-sm-6">
                <label class="form-label">Tipo <span class="text-danger">*</span></label>
                <div class="d-flex gap-2">
                  <button type="button" class="btn btn-sm flex-fill"
                    :class="form.Type === 'Percentage' ? 'btn-info' : 'btn-outline-secondary'"
                    @click="form.Type = 'Percentage'">
                    <i class="fal fa-percent me-1"></i>Porcentaje
                  </button>
                  <button type="button" class="btn btn-sm flex-fill"
                    :class="form.Type === 'FixedAmount' ? 'btn-warning' : 'btn-outline-secondary'"
                    @click="form.Type = 'FixedAmount'">
                    <i class="fal fa-dollar-sign me-1"></i>Monto fijo
                  </button>
                </div>
                <small v-if="formErrors.Type" class="text-danger">Selecciona un tipo.</small>
              </div>

              <!-- Valor -->
              <div class="col-12 col-sm-6">
                <label class="form-label">
                  {{ form.Type === 'Percentage' ? 'Porcentaje (%)' : 'Monto (Bs.)' }}
                  <span class="text-danger">*</span>
                </label>
                <input
                  type="number" class="form-control form-control-sm"
                  :class="{ 'is-invalid': formErrors.Value }"
                  v-model.number="form.Value"
                  :max="form.Type === 'Percentage' ? 100 : undefined"
                  min="0.01" step="0.01" placeholder="0"
                />
                <div class="invalid-feedback">{{ valueErrorMsg }}</div>
              </div>

              <!-- Descripción -->
              <div class="col-12">
                <label class="form-label">Descripción</label>
                <input
                  type="text" class="form-control form-control-sm"
                  v-model.trim="form.Description"
                  placeholder="Descripción opcional"
                  maxlength="150"
                />
              </div>

              <!-- Activo -->
              <div class="col-12">
                <div class="form-check form-switch">
                  <input class="form-check-input" type="checkbox" id="discountActive" v-model="form.IsActive" />
                  <label class="form-check-label" for="discountActive">
                    {{ form.IsActive ? 'Activo (visible en el POS)' : 'Inactivo (oculto en el POS)' }}
                  </label>
                </div>
              </div>
            </div>

          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-outline-secondary btn-sm" @click="closeModal">Cancelar</button>
            <button type="button" class="btn btn-primary btn-sm" :disabled="saving" @click="save">
              <span v-if="saving" class="spinner-border spinner-border-sm me-1"></span>
              <i v-else class="fal fa-save me-1"></i>Guardar
            </button>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { Discount } from '@/modules/inventory/models/discount.model';
import useDiscount from '@/modules/inventory/composables/useDiscount';
import utils from '@/utils/msg';

const { getDiscounts, createDiscount, updateDiscount, deleteDiscount } = useDiscount();

const discounts = ref<Discount[]>([]);
const loading = ref(false);
const showModal = ref(false);
const saving = ref(false);
const form = ref(new Discount());
const formErrors = ref({ Name: false, Type: false, Value: false });

const formatNum = (val: number) =>
  (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const valueErrorMsg = computed(() => {
  if (!form.value.Value || form.value.Value <= 0) return 'El valor debe ser mayor a cero.';
  if (form.value.Type === 'Percentage' && form.value.Value > 100) return 'No puede superar el 100%.';
  return '';
});

const loadDiscounts = async () => {
  loading.value = true;
  const { Data } = await getDiscounts();
  discounts.value = Data ?? [];
  loading.value = false;
};

onMounted(loadDiscounts);

const openModal = (discount?: Discount) => {
  formErrors.value = { Name: false, Type: false, Value: false };
  if (discount) {
    form.value = { ...discount };
  } else {
    form.value = new Discount();
    form.value.Type = 'Percentage';
    form.value.IsActive = true;
  }
  showModal.value = true;
};

const closeModal = () => { showModal.value = false; };

const validateForm = (): boolean => {
  formErrors.value.Name = !form.value.Name;
  formErrors.value.Type = !form.value.Type;
  formErrors.value.Value = !form.value.Value || form.value.Value <= 0
    || (form.value.Type === 'Percentage' && form.value.Value > 100);
  return !formErrors.value.Name && !formErrors.value.Type && !formErrors.value.Value;
};

const save = async () => {
  if (!validateForm()) return;
  const confirm = await utils.showMessageQuestion(
    form.value.Id ? '¿Desea guardar los cambios?' : '¿Desea crear el descuento?'
  );
  if (!confirm) return;

  saving.value = true;
  try {
    if (form.value.Id) {
      const { ok } = await updateDiscount(form.value.Id, form.value);
      if (ok) {
        await utils.showMessageModal({ Description: 'Descuento actualizado correctamente.', MessageType: 'success' });
        closeModal();
        await loadDiscounts();
      }
    } else {
      const { ok } = await createDiscount(form.value);
      if (ok) {
        await utils.showMessageModal({ Description: 'Descuento creado correctamente.', MessageType: 'success' });
        closeModal();
        await loadDiscounts();
      }
    }
  } finally {
    saving.value = false;
  }
};

const remove = async (id: string) => {
  const confirm = await utils.showMessageQuestion('¿Desea eliminar este descuento?');
  if (!confirm) return;
  const { ok } = await deleteDiscount(id);
  if (ok) {
    await utils.showMessageModal({ Description: 'Descuento eliminado correctamente.', MessageType: 'success' });
    await loadDiscounts();
  }
};
</script>

<style scoped></style>
