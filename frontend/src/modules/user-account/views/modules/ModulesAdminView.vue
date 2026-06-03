<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Cuenta</li>
        <li class="breadcrumb-item active" aria-current="page">Registro de Módulos</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>MÓDULOS</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">
            <!-- Botón Nuevo -->
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newModule">
                <span class="fal fa-plus-square me-1"></span>Nuevo Módulo
              </button>
            </div>
            <!-- Toolbar: búsqueda + nuevo -->
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre del módulo</label>
                <div class="input-group input-group body-bg shadow-inset-2 rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="sa sa-magnifier text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="filtro.NameModule"
                    placeholder="Ingrese el nombre del módulo..."
                    autocomplete="off"
                    @keyup.enter="getModulesData"
                  />
                  <button class="btn btn-primary" type="button" @click="getModulesData">Buscar</button>
                </div>
              </div>
              
            </div>

            <!-- Contador de resultados -->
            <div v-if="modules.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="fal fa-list me-1"></span>
                <strong>{{ modules.length }}</strong> módulo(s) encontrado(s)
              </small>
            </div>

            <!-- Estado vacío -->
            <div v-if="modules.length === 0" class="text-center py-5">
              <i class="fal fa-th-large fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar módulos en el sistema</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newModule">
                <span class="fal fa-plus me-1"></span>Crear nuevo módulo
              </button>
            </div>

            <!-- Resultados -->
            <template v-else>

              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="">
                    <tr>
                      <th>Nombre del Módulo</th>
                      <th class="d-none d-lg-table-cell">Ruta</th>
                      <th class="text-center">Ícono</th>
                      <th class="text-center">Orden</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(module, index) in modules" :key="index">
                      <td class="fw-semibold">{{ module.NameModule }}</td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ module.Route }}</small>
                      </td>
                      <td class="text-center">
                        <i :class="module.IconCss" class="text-primary" :title="module.IconCss"></i>
                      </td>
                      <td class="text-center">
                        <span class="badge bg-light text-secondary border">{{ module.ShowOrder }}</span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Editar"
                          @click="editModule(module)"
                        >
                          <span class="fal fa-edit"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeModule(module.Id)"
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
                  <div class="col-12 col-sm-6" v-for="(module, index) in modules" :key="index">
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <p class="fw-semibold mb-0 lh-sm">{{ module.NameModule }}</p>
                          <span class="badge bg-body-secondary text-secondary border">Orden {{ module.ShowOrder }}</span>
                        </div>
                        <small class="text-muted">
                          <i :class="module.IconCss" class="text-primary me-1"></i>{{ module.Route }}
                        </small>
                        <div class="d-flex gap-2 mt-auto pt-1">
                          <button type="button" class="btn btn-sm btn-outline-primary flex-grow-1" @click="editModule(module)">
                            <span class="fal fa-edit me-1"></span>Editar
                          </button>
                          <button type="button" class="btn btn-sm btn-outline-danger" @click="removeModule(module.Id)">
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
import { useRouter } from "vue-router";
import useModule from '@/modules/user-account/composables/useModule';
import type { Module } from '@/modules/user-account/models/module.model';
import utils from '@/utils/msg';

const modules = ref<Module[]>([]);
const { getModules, deleteModule } = useModule();
const router = useRouter();

const filtro = ref({
  NameModule: '',
});

const getModulesData = async () => {
  const { Data: modulesResp } = await getModules(filtro.value.NameModule);
  modules.value = modulesResp;
};

const newModule = () => {
  router.push({ name: 'module-edit', params: { id: '0' } });
};

const editModule = (module: Module) => {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const moduleId = module.Id || (module as any).id;
  if (!moduleId) {
    utils.showMessageModal({ Description: 'Error: No se pudo obtener el ID del módulo.', MessageType: 'error' });
    return;
  }
  router.push({ name: 'module-edit', params: { id: moduleId } });
};

const removeModule = async (id: number) => {
  const respuesta = await utils.showMessageQuestion('¿Desea eliminar el módulo?');
  if (respuesta) {
    const { ok } = await deleteModule(id);
    if (ok) {
      await utils.showMessageModal({ Description: 'El módulo se eliminó correctamente.', MessageType: 'success' });
      await getModulesData();
    }
  }
};
</script>

<style scoped></style>
