<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Cuenta</li>
        <li class="breadcrumb-item active" aria-current="page">Registro de Formularios</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>FORMULARIOS</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            

             <!-- Botón Nuevo -->
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newForm">
                <span class="fal fa-plus-square me-1"></span>Nuevo Formulario
              </button>
            </div>

            <!-- Toolbar: búsqueda + nuevo -->
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre del formulario</label>
                <div class="input-group input-group body-bg shadow-inset-2 rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="sa sa-magnifier text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="filtro.NameForm"
                    placeholder="Ingrese el nombre del formulario..."
                    autocomplete="off"
                    @keyup.enter="getFormsData"
                  />
                  <button class="btn btn-primary" type="button" @click="getFormsData">Buscar</button>
                </div>
              </div>
              
            </div>

            <!-- Contador de resultados -->
            <div v-if="forms.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="fal fa-list me-1"></span>
                <strong>{{ forms.length }}</strong> formulario(s) encontrado(s)
              </small>
            </div>

            <!-- Estado vacío -->
            <div v-if="forms.length === 0" class="text-center py-5">
              <i class="fal fa-file-alt fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar formularios en el sistema</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newForm">
                <span class="fal fa-plus me-1"></span>Crear nuevo formulario
              </button>
            </div>

            <!-- Resultados -->
            <template v-else>

              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th>Nombre del Formulario</th>
                      <th class="d-none d-lg-table-cell">Descripción</th>
                      <th class="d-none d-lg-table-cell">Ruta</th>
                      <th class="d-none d-xl-table-cell">Controlador</th>
                      <th class="text-center">Menú</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(form, index) in forms" :key="index">
                      <td class="fw-semibold">{{ form.NameForm }}</td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ form.Description }}</small>
                      </td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ form.Route }}</small>
                      </td>
                      <td class="d-none d-xl-table-cell">
                        <small class="text-muted">{{ form.Controller }}</small>
                      </td>
                      <td class="text-center">
                        <span class="badge" :class="form.ShowMenu ? 'bg-success' : 'bg-secondary'">
                          {{ form.ShowMenu ? 'Visible' : 'Oculto' }}
                        </span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Editar"
                          @click="editForm(form)"
                        >
                          <span class="fal fa-edit"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeForm(form.Id)"
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
                  <div class="col-12 col-sm-6" v-for="(form, index) in forms" :key="index">
                    <div class="card h-100">
                      <div class="card-body d-flex flex-column">
                        <div class="d-flex justify-content-between align-items-start mb-1">
                          <h6 class="card-title mb-0">{{ form.NameForm }}</h6>
                          <span class="badge ms-2" :class="form.ShowMenu ? 'bg-success' : 'bg-secondary'">
                            {{ form.ShowMenu ? 'Visible' : 'Oculto' }}
                          </span>
                        </div>
                        <small class="text-muted mb-1">{{ form.Description }}</small>
                        <small class="text-muted mb-3">{{ form.Route }}</small>
                        <div class="mt-auto">
                          <div class="btn-group w-100" role="group">
                            <button type="button" class="btn btn-outline-primary btn-sm"
                              @click="editForm(form)">
                              <span class="fal fa-edit me-1"></span>Editar
                            </button>
                            <button type="button" class="btn btn-outline-danger btn-sm"
                              @click="removeForm(form.Id)">
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
import { useRouter } from "vue-router";
import useForm from '@/modules/user-account/composables/useForm';
import type { Form } from '@/modules/user-account/models/form.model';
import utils from '@/utils/msg';

const forms = ref<Form[]>([]);
const { getForms, deleteForm } = useForm();
const router = useRouter();

const filtro = ref({
  NameForm: '',
});

const getFormsData = async () => {
  const { Data: formsResp } = await getForms(filtro.value.NameForm);
  forms.value = formsResp;
};

const newForm = () => {
  router.push({ name: 'form-edit', params: { id: '0' } });
};

const editForm = (form: Form) => {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const formId = form.Id || (form as any).id;
  if (!formId) {
    utils.showMessageModal({ Description: 'Error: No se pudo obtener el ID del formulario.', MessageType: 'error' });
    return;
  }
  router.push({ name: 'form-edit', params: { id: formId } });
};

const removeForm = async (id: number) => {
  const respuesta = await utils.showMessageQuestion('¿Desea eliminar el formulario?');
  if (respuesta) {
    const { ok } = await deleteForm(id);
    if (ok) {
      await utils.showMessageModal({ Description: 'El formulario se eliminó correctamente.', MessageType: 'success' });
      await getFormsData();
    }
  }
};
</script>

<style scoped></style>
