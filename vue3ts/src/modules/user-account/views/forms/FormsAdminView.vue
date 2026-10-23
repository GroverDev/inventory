<template>
  <Layout>
    <PageHeader title="Registro de Formularios" page-title="Cuenta" />

    <div class="card">
        <div class="card-header">
          <h5 class="card-title mb-0">Gestión de <span class="fw-light"><i>FORMULARIOS</i></span></h5>
        </div>
        <div class="pt-0 card-body">

            

             <!-- Botón Nuevo -->
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newForm">
                <span class="ri-add-box-line me-1"></span>Nuevo Formulario
              </button>
            </div>

            <!-- Toolbar: búsqueda + nuevo -->
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre del formulario</label>
                <div class="input-group rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="ri-search-line text-success"></i>
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
                <span class="ri-list-check me-1"></span>
                <strong>{{ forms.length }}</strong> formulario(s) encontrado(s)
              </small>
            </div>

            <!-- Estado vacío -->
            <div v-if="forms.length === 0" class="text-center py-5">
              <i class="ri-file-text-line ri-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar formularios en el sistema</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newForm">
                <span class="ri-add-line me-1"></span>Crear nuevo formulario
              </button>
            </div>

            <!-- Resultados -->
            <template v-else>

              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="">
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
                          <span class="ri-edit-2-line"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeForm(form.Id)"
                        >
                          <span class="ri-delete-bin-line"></span>
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
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <p class="fw-semibold mb-0 lh-sm">{{ form.NameForm }}</p>
                          <span class="badge rounded-pill" :class="form.ShowMenu ? 'text-bg-success' : 'text-bg-secondary'">
                            {{ form.ShowMenu ? 'Visible' : 'Oculto' }}
                          </span>
                        </div>
                        <small class="text-muted">{{ form.Description }}</small>
                        <code class="bg-body-secondary rounded px-2 py-1 small">{{ form.Route }}</code>
                        <div class="d-flex gap-2 mt-auto pt-1">
                          <button type="button" class="btn btn-sm btn-outline-primary flex-grow-1" @click="editForm(form)">
                            <span class="ri-edit-2-line me-1"></span>Editar
                          </button>
                          <button type="button" class="btn btn-sm btn-outline-danger" @click="removeForm(form.Id)">
                            <span class="ri-delete-bin-line"></span>
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
    
  </Layout>
</template>

<script setup lang="ts">
import Layout from '@/layouts/main.vue'
import PageHeader from '@/components/page-header.vue'
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
