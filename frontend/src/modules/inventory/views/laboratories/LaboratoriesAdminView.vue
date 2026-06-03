<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item active" aria-current="page">Registro de Laboratorios</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>LABORATORIOS</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newLaboratory">
                <span class="fal fa-plus-square me-1"></span>Nuevo Laboratorio
              </button>
            </div>
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre del laboratorio</label>
                <div class="input-group input-group body-bg shadow-inset-2 rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="sa sa-magnifier text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="filtro"
                    placeholder="Ingrese el nombre del laboratorio..."
                    autocomplete="off"
                    @keyup.enter="getLaboratoriesData"
                  />
                  <button class="btn btn-primary" type="button" @click="getLaboratoriesData">Buscar</button>
                </div>
              </div>
            </div>

            <div v-if="laboratories.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="fal fa-list me-1"></span>
                <strong>{{ laboratories.length }}</strong> laboratorio(s) encontrado(s)
              </small>
            </div>

            <div v-if="laboratories.length === 0" class="text-center py-5">
              <i class="fal fa-flask fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar laboratorios en el sistema</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newLaboratory">
                <span class="fal fa-plus me-1"></span>Crear nuevo laboratorio
              </button>
            </div>

            <template v-else>
              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="">
                    <tr>
                      <th>Nombre</th>
                      <th class="d-none d-lg-table-cell">Descripción</th>
                      <th class="d-none d-lg-table-cell">Dirección</th>
                      <th>Celular</th>
                      <th class="text-center">Activo</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(lab, index) in laboratories" :key="index">
                      <td class="fw-semibold">{{ lab.LaboratoryName }}</td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ lab.Description }}</small>
                      </td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ lab.Direction }}</small>
                      </td>
                      <td>{{ lab.Celular }}</td>
                      <td class="text-center">
                        <span :class="lab.IsActive ? 'badge bg-success' : 'badge bg-secondary'">
                          {{ lab.IsActive ? 'Sí' : 'No' }}
                        </span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Editar"
                          @click="editLaboratory(lab)"
                        >
                          <span class="fal fa-edit"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeLaboratory(lab.Id)"
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
                  <div class="col-12 col-sm-6" v-for="(lab, index) in laboratories" :key="index">
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <p class="fw-semibold mb-0 lh-sm">{{ lab.LaboratoryName }}</p>
                          <span class="badge rounded-pill" :class="lab.IsActive ? 'text-bg-success' : 'text-bg-secondary'">
                            {{ lab.IsActive ? 'Activo' : 'Inactivo' }}
                          </span>
                        </div>
                        <small class="text-muted">{{ lab.Description }}</small>
                        <small class="text-muted"><i class="fal fa-phone me-1"></i>{{ lab.Celular }}</small>
                        <div class="d-flex gap-2 mt-auto pt-1">
                          <button type="button" class="btn btn-sm btn-outline-primary flex-grow-1" @click="editLaboratory(lab)">
                            <span class="fal fa-edit me-1"></span>Editar
                          </button>
                          <button type="button" class="btn btn-sm btn-outline-danger" @click="removeLaboratory(lab.Id)">
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
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import useLaboratory from '@/modules/inventory/composables/useLaboratory';
import type { Laboratory } from '@/modules/inventory/models/laboratory.model';
import utils from '@/utils/msg';

const laboratories = ref<Laboratory[]>([]);
const filtro = ref('');
const { getLaboratories, deleteLaboratory } = useLaboratory();
const router = useRouter();

const getLaboratoriesData = async () => {
  const { Data } = await getLaboratories(filtro.value);
  laboratories.value = Data;
};

const newLaboratory = () => {
  router.push({ name: 'laboratory-edit', params: { id: '0' } });
};

const editLaboratory = (lab: Laboratory) => {
  router.push({ name: 'laboratory-edit', params: { id: lab.Id } });
};

const removeLaboratory = async (id: string) => {
  const respuesta = await utils.showMessageQuestion('¿Desea eliminar el laboratorio?');
  if (respuesta) {
    const { ok } = await deleteLaboratory(id);
    if (ok) {
      await utils.showMessageModal({ Description: 'El laboratorio se eliminó correctamente.', MessageType: 'success' });
      await getLaboratoriesData();
    }
  }
};
</script>

<style scoped></style>
