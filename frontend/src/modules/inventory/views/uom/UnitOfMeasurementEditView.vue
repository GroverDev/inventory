<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Unidades de Medida</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">
          {{ localUom.Id ? 'Editar Unidad' : 'Nueva Unidad' }}
        </li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="row">
        <div class="col">
          <div class="panel panel-icon">
            <div class="panel-hdr">
              <h2>
                {{ localUom.Id ? 'Editar' : 'Nueva' }}
                <span class="fw-300"><i> Unidad de Medida</i></span>
              </h2>
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
                          <button type="button" class="dropdown-item border-bottom border-1"
                            :disabled="isSaved" @click="saveUom">
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
                      <button type="button" class="btn btn-sm btn-primary"
                        :disabled="isSaved" @click="saveUom">
                        <span class="fal fa-save me-1"></span>Grabar
                      </button>
                      <button type="button" class="btn btn-warning btn-sm" @click="returnPage">
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

                  <h6 class="text-muted border-bottom pb-2 mb-3">
                    <i class="fal fa-ruler me-1"></i> Datos de la Unidad de Medida
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label" for="UnitName">
                        Nombre <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="UnitName"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.UnitName.$dirty && v$.UnitName.$invalid }"
                        placeholder="Nombre de la unidad"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.UnitName.$model"
                      />
                      <small class="invalid-feedback">Requerido (mínimo 3 caracteres).</small>
                    </div>
                  </div>

                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-cogs me-1"></i> Configuración
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-4 col-md-3 mb-3">
                      <label class="form-label" for="Proportion">Proporción</label>
                      <input
                        type="number"
                        id="Proportion"
                        class="form-control form-control-sm text-end"
                        placeholder="0"
                        min="0"
                        :disabled="isSaved"
                        v-model.number="localUom.Proportion"
                      />
                    </div>
                    <div class="col-12 col-sm-4 col-md-3 mb-3">
                      <label class="form-label" for="PrecisionRounding">Redondeo</label>
                      <input
                        type="number"
                        id="PrecisionRounding"
                        class="form-control form-control-sm text-end"
                        placeholder="0"
                        min="0"
                        :disabled="isSaved"
                        v-model.number="localUom.PrecisionRounding"
                      />
                    </div>
                    <div class="col-12 col-sm-4 col-md-2 mb-3">
                      <label class="form-label d-block">Mayor que default</label>
                      <div class="form-check form-switch mt-1">
                        <input
                          class="form-check-input"
                          type="checkbox"
                          id="IsLargeThanDefault"
                          :disabled="isSaved"
                          v-model="localUom.IsLargeThanDefault"
                        />
                        <label class="form-check-label" for="IsLargeThanDefault">
                          {{ localUom.IsLargeThanDefault ? 'Sí' : 'No' }}
                        </label>
                      </div>
                    </div>
                    <div class="col-12 col-sm-4 col-md-2 mb-3">
                      <label class="form-label d-block">Default</label>
                      <div class="form-check form-switch mt-1">
                        <input
                          class="form-check-input"
                          type="checkbox"
                          id="IsDefault"
                          :disabled="isSaved"
                          v-model="localUom.IsDefault"
                        />
                        <label class="form-check-label" for="IsDefault">
                          {{ localUom.IsDefault ? 'Sí' : 'No' }}
                        </label>
                      </div>
                    </div>
                    <div class="col-12 col-sm-4 col-md-2 mb-3">
                      <label class="form-label d-block">Activo</label>
                      <div class="form-check form-switch mt-1">
                        <input
                          class="form-check-input"
                          type="checkbox"
                          id="IsActive"
                          :disabled="isSaved"
                          v-model="localUom.IsActive"
                        />
                        <label class="form-check-label" for="IsActive">
                          {{ localUom.IsActive ? 'Sí' : 'No' }}
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
import { onMounted, ref, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import useVuelidate from '@vuelidate/core';
import { required, minLength } from '@vuelidate/validators';
import utils from '@/utils/msg';
import { UnitOfMeasurement } from '@/modules/inventory/models/unitOfMeasurement.model';
import useUnitOfMeasurement from '@/modules/inventory/composables/useUnitOfMeasurement';

const router = useRouter();
const route = useRoute();
const { getUnitOfMeasurementById, createUnitOfMeasurement, updateUnitOfMeasurement } = useUnitOfMeasurement();

const localUom = ref(new UnitOfMeasurement());
const isSaved = ref(false);

const rules = computed(() => ({
  UnitName: { required, minLength: minLength(3) },
}));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const v$ = useVuelidate(rules, localUom as any);

onMounted(async () => {
  const id = route.params.id as string;
  if (id && id !== '0') {
    await loadUom(id);
  } else {
    localUom.value.IsActive = true;
  }
});

const loadUom = async (id: string) => {
  const { ok, Data } = await getUnitOfMeasurementById(id);
  if (ok) localUom.value = Data;
};

const returnPage = () => {
  router.push({ name: 'uom-admin' });
};

const saveUom = async () => {
  const isFormCorrect = await v$.value.$validate();
  if (!isFormCorrect) return;

  const respuesta = await utils.showMessageQuestion('¿Desea guardar la unidad de medida?');
  if (!respuesta) return;

  if (!localUom.value.Id) {
    const { ok } = await createUnitOfMeasurement(localUom.value);
    if (ok) {
      isSaved.value = true;
      await utils.showMessageModal({ Description: 'La unidad de medida se creó correctamente.', MessageType: 'success' });
      returnPage();
    }
  } else {
    const { ok } = await updateUnitOfMeasurement(localUom.value);
    if (ok) {
      await utils.showMessageModal({ Description: 'La unidad de medida se actualizó correctamente.', MessageType: 'success' });
      returnPage();
    }
  }
};
</script>

<style scoped></style>
