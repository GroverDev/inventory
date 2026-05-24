<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Cuenta</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de Formularios</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">
          {{ localForm.Id ? 'Editar Formulario' : 'Nuevo Formulario' }}
        </li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="row">
        <div class="col">
          <div id="panel-1" class="panel panel-icon">
            <div class="panel-hdr">
              <h2>
                {{ localForm.Id ? 'Editar' : 'Nuevo' }}
                <span class="fw-300"><i> Formulario</i></span>
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
                            :disabled="isSaved" @click="saveForm">
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
                        :disabled="isSaved" @click="saveForm">
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

                  <!-- Sección 1: Identificación -->
                  <h6 class="text-muted border-bottom pb-2 mb-3">
                    <i class="fal fa-id-badge me-1"></i> Identificación del Formulario
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="NameForm">
                        Nombre del Formulario <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="NameForm"
                        name="NameForm"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.NameForm.$dirty && v$.NameForm.$invalid }"
                        placeholder="Nombre del Formulario"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.NameForm.$model"
                      />
                      <small class="invalid-feedback">Requerido.</small>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="Description">Descripción</label>
                      <input
                        type="text"
                        id="Description"
                        name="Description"
                        class="form-control form-control-sm"
                        placeholder="Descripción del formulario"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="localForm.Description"
                      />
                    </div>
                  </div>

                  <!-- Sección 2: Configuración técnica -->
                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-code me-1"></i> Configuración Técnica
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="Route">
                        Ruta <span class="text-danger">*</span>
                      </label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">
                          <i class="fal fa-link"></i>
                        </span>
                        <input
                          type="text"
                          id="Route"
                          name="Route"
                          class="form-control"
                          :class="{ 'is-invalid': v$.Route.$dirty && v$.Route.$invalid }"
                          placeholder="/ruta"
                          :disabled="isSaved"
                          autocomplete="off"
                          v-model.trim="v$.Route.$model"
                        />
                        <div class="invalid-feedback">Requerido.</div>
                      </div>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="Controller">Controlador</label>
                      <input
                        type="text"
                        id="Controller"
                        name="Controller"
                        class="form-control form-control-sm"
                        placeholder="NombreController"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.Controller.$model"
                      />
                    </div>
                    <div class="col-12 col-sm-4 col-md-3 mb-3">
                      <label class="form-label d-block" for="Orden">Orden en Menú</label>
                      <input
                        type="number"
                        id="Orden"
                        name="Orden"
                        class="form-control form-control-sm text-end"
                        placeholder="0"
                        min="0"
                        :disabled="isSaved"
                        v-model.number="localForm.Orden"
                      />
                    </div>
                    <div class="col-12 col-sm-8 col-md-5 mb-3">
                      <label class="form-label d-block" for="IconCss">Ícono CSS</label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">
                          <i :class="localForm.IconCss || 'fal fa-file'" class="text-primary"></i>
                        </span>
                        <input
                          type="text"
                          id="IconCss"
                          name="IconCss"
                          class="form-control"
                          placeholder="fal fa-file"
                          :disabled="isSaved"
                          v-model.trim="localForm.IconCss"
                        />
                      </div>
                    </div>
                  </div>

                  <!-- Sección 3: Módulo y Jerarquía -->
                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-th-large me-1"></i> Módulo y Jerarquía
                  </h6>
                  <div class="row">
                    <div class="col-12 col-md-6 mb-3">
                      <label class="form-label d-block" for="ModuleId">Módulo al que pertenece</label>
                      <v-select
                        v-model="moduleSelected"
                        :options="modules"
                        label="NameModule"
                        placeholder="— Seleccione un módulo —"
                        :disabled="isSaved"
                      />
                    </div>
                    <div class="col-12 col-md-6 mb-3">
                      <label class="form-label d-block" for="FormId">Formulario padre</label>
                      <v-select
                        v-model="parentFormSelected"
                        :options="parentForms"
                        label="NameForm"
                        placeholder="— Sin padre (formulario raíz) —"
                        :disabled="isSaved"
                        :clearable="true"
                      />
                      <small class="text-muted">Déjalo vacío si es un formulario de nivel raíz.</small>
                    </div>
                  </div>

                  <!-- Sección 4: Configuración -->
                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-sliders-h me-1"></i> Configuración
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <div class="form-check form-switch">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          id="ShowMenu"
                          role="switch"
                          :disabled="isSaved"
                          v-model="localForm.ShowMenu"
                        />
                        <label class="form-check-label" for="ShowMenu">Mostrar en Menú</label>
                      </div>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <div class="form-check form-switch">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          id="IsFormRegister"
                          role="switch"
                          :disabled="isSaved"
                          v-model="localForm.IsFormRegister"
                        />
                        <label class="form-check-label" for="IsFormRegister">Es Formulario de Registro</label>
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
import { useRouter, useRoute } from "vue-router";
import useVuelidate from '@vuelidate/core';
import { required } from '@vuelidate/validators';
import utils from '@/utils/msg';

import { Form } from '@/modules/user-account/models/form.model';
import useForm from '@/modules/user-account/composables/useForm';

import type { Module } from '@/modules/user-account/models/module.model';
import useModule from '@/modules/user-account/composables/useModule';

import VSelect from 'vue-select';

const router = useRouter();
const route = useRoute();
const { getFormById, createForm, updateForm, getForms } = useForm();
const { getModules } = useModule();

const localForm = ref(new Form());
const isSaved = ref(false);
const modules = ref<Module[]>([]);
const moduleSelected = ref<Module>();
const parentForms = ref<Form[]>([]);
const parentFormSelected = ref<Form | null>(null);

const rules = computed(() => ({
  NameForm: { required },
  Route: { required },
  Controller: { required },
}));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const v$ = useVuelidate(rules, localForm as any);

onMounted(async () => {
  const formId = route.params.id as string;
  if (formId && formId !== '0') {
    await loadForm(parseInt(formId));
  }
  await Promise.all([getModulesOfApi(), getParentFormsOfApi()]);
  moduleSelected.value = modules.value.find(module => module.Id === localForm.value.ModuleId);
  parentFormSelected.value = parentForms.value.find(f => f.Id === localForm.value.FormId) ?? null;
});

const loadForm = async (id: number) => {
  const { ok, Data: formResp } = await getFormById(id);
  if (ok) localForm.value = formResp;
};

const getModulesOfApi = async () => {
  const { ok, Data: modulesResp } = await getModules('');
  if (ok) modules.value = modulesResp;
};

const getParentFormsOfApi = async () => {
  const { ok, Data: formsResp } = await getForms('');
  if (ok) {
    const currentId = localForm.value.Id;
    parentForms.value = formsResp.filter(f => f.Id !== currentId);
  }
};

const returnPage = () => {
  router.push({ name: 'forms-admin' });
};

const saveForm = async () => {
  localForm.value.ModuleId = moduleSelected.value?.Id ?? 0;
  localForm.value.FormId = parentFormSelected.value?.Id ?? 0;

  const isFormCorrect = await v$.value.$validate();
  if (!isFormCorrect) return;

  const respuesta = await utils.showMessageQuestion('¿Desea guardar el formulario?');

  if (respuesta) {
    if (localForm.value.Id === 0) {
      const { ok, Data: idForm } = await createForm(localForm.value);
      if (ok) {
        isSaved.value = true;
        localForm.value.Id = idForm;
        await utils.showMessageModal({ Description: 'El formulario se creó correctamente.', MessageType: 'success' });
        returnPage();
      }
    } else {
      const { ok, Data: okResp } = await updateForm(localForm.value);
      if (ok && okResp) {
        await utils.showMessageModal({ Description: 'El formulario se actualizó correctamente.', MessageType: 'success' });
        returnPage();
      }
    }
  }
};
</script>

<style scoped></style>
