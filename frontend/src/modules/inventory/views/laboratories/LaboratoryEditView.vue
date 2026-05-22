<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de Laboratorios</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">
          {{ localLab.Id ? 'Editar Laboratorio' : 'Nuevo Laboratorio' }}
        </li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="row">
        <div class="col">
          <div class="panel panel-icon">
            <div class="panel-hdr">
              <h2>
                {{ localLab.Id ? 'Editar' : 'Nuevo' }}
                <span class="fw-300"><i> Laboratorio</i></span>
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
                            :disabled="isSaved" @click="saveLaboratory">
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
                        :disabled="isSaved" @click="saveLaboratory">
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
                    <i class="fal fa-flask me-1"></i> Datos del Laboratorio
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label" for="LaboratoryName">
                        Nombre <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="LaboratoryName"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.LaboratoryName.$dirty && v$.LaboratoryName.$invalid }"
                        placeholder="Nombre del laboratorio"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.LaboratoryName.$model"
                      />
                      <small class="invalid-feedback">Requerido.</small>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label" for="Description">Descripción</label>
                      <input
                        type="text"
                        id="Description"
                        class="form-control form-control-sm"
                        placeholder="Descripción"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="localLab.Description"
                      />
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label" for="Direction">Dirección</label>
                      <input
                        type="text"
                        id="Direction"
                        class="form-control form-control-sm"
                        placeholder="Dirección"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="localLab.Direction"
                      />
                    </div>
                    <div class="col-12 col-sm-4 mb-3">
                      <label class="form-label" for="Celular">Celular</label>
                      <input
                        type="text"
                        id="Celular"
                        class="form-control form-control-sm"
                        placeholder="Número de celular"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="localLab.Celular"
                      />
                    </div>
                    <div class="col-12 col-sm-2 mb-3">
                      <label class="form-label d-block">Activo</label>
                      <div class="form-check form-switch mt-1">
                        <input
                          class="form-check-input"
                          type="checkbox"
                          id="IsActive"
                          :disabled="isSaved"
                          v-model="localLab.IsActive"
                        />
                        <label class="form-check-label" for="IsActive">
                          {{ localLab.IsActive ? 'Sí' : 'No' }}
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
import { required } from '@vuelidate/validators';
import utils from '@/utils/msg';
import { Laboratory } from '@/modules/inventory/models/laboratory.model';
import useLaboratory from '@/modules/inventory/composables/useLaboratory';

const router = useRouter();
const route = useRoute();
const { getLaboratoryById, createLaboratory, updateLaboratory } = useLaboratory();

const localLab = ref(new Laboratory());
const isSaved = ref(false);

const rules = computed(() => ({
  LaboratoryName: { required },
}));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const v$ = useVuelidate(rules, localLab as any);

onMounted(async () => {
  const id = route.params.id as string;
  if (id && id !== '0') {
    await loadLaboratory(id);
  } else {
    localLab.value.IsActive = true;
  }
});

const loadLaboratory = async (id: string) => {
  const { ok, Data } = await getLaboratoryById(id);
  if (ok) localLab.value = Data;
};

const returnPage = () => {
  router.push({ name: 'laboratories-admin' });
};

const saveLaboratory = async () => {
  const isFormCorrect = await v$.value.$validate();
  if (!isFormCorrect) return;

  const respuesta = await utils.showMessageQuestion('¿Desea guardar el laboratorio?');
  if (!respuesta) return;

  if (!localLab.value.Id) {
    const { ok } = await createLaboratory(localLab.value);
    if (ok) {
      isSaved.value = true;
      await utils.showMessageModal({ Description: 'El laboratorio se creó correctamente.', MessageType: 'success' });
      returnPage();
    }
  } else {
    const { ok } = await updateLaboratory(localLab.value);
    if (ok) {
      await utils.showMessageModal({ Description: 'El laboratorio se actualizó correctamente.', MessageType: 'success' });
      returnPage();
    }
  }
};
</script>

<style scoped></style>
