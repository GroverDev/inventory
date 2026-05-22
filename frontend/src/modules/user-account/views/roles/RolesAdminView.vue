<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Seguridad</li>
        <li class="breadcrumb-item active" aria-current="page">Registro de Roles</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>ROLES</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <!-- Botón Nuevo -->
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newRole">
                <span class="fal fa-plus-square me-1"></span>Nuevo Rol
              </button>
            </div>

            <!-- Toolbar: búsqueda -->
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre del rol</label>
                <div class="input-group input-group body-bg shadow-inset-2 rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="sa sa-magnifier text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="filtro.NameRol"
                    placeholder="Ingrese el nombre del rol..."
                    autocomplete="off"
                    @keyup.enter="getRolesData"
                  />
                  <button class="btn btn-primary" type="button" @click="getRolesData">Buscar</button>
                </div>
              </div>
            </div>

            <!-- Contador de resultados -->
            <div v-if="roles.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="fal fa-list me-1"></span>
                <strong>{{ roles.length }}</strong> rol(es) encontrado(s)
              </small>
            </div>

            <!-- Estado vacío -->
            <div v-if="roles.length === 0" class="text-center py-5">
              <i class="fal fa-user-shield fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar roles en el sistema</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newRole">
                <span class="fal fa-plus me-1"></span>Crear nuevo rol
              </button>
            </div>

            <!-- Resultados -->
            <template v-else>

              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th>Nombre del Rol</th>
                      <th class="d-none d-lg-table-cell">Descripción</th>
                      <th class="text-center">Estado</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(role, index) in roles" :key="index">
                      <td class="fw-semibold">{{ role.NameRol }}</td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ role.Description }}</small>
                      </td>
                      <td class="text-center">
                        <span class="badge" :class="role.State ? 'bg-success' : 'bg-secondary'">
                          {{ role.State ? 'Activo' : 'Inactivo' }}
                        </span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Editar / Asignar Formularios"
                          @click="editRole(role)"
                        >
                          <span class="fal fa-edit"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeRole(role.Id)"
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
                  <div class="col-12 col-sm-6" v-for="(role, index) in roles" :key="index">
                    <div class="card h-100">
                      <div class="card-body d-flex flex-column">
                        <div class="d-flex justify-content-between align-items-start mb-1">
                          <h6 class="card-title mb-0">{{ role.NameRol }}</h6>
                          <span class="badge ms-2" :class="role.State ? 'bg-success' : 'bg-secondary'">
                            {{ role.State ? 'Activo' : 'Inactivo' }}
                          </span>
                        </div>
                        <small class="text-muted mb-3">{{ role.Description }}</small>
                        <div class="mt-auto">
                          <div class="btn-group w-100" role="group">
                            <button type="button" class="btn btn-outline-primary btn-sm"
                              @click="editRole(role)">
                              <span class="fal fa-edit me-1"></span>Editar
                            </button>
                            <button type="button" class="btn btn-outline-danger btn-sm"
                              @click="removeRole(role.Id)">
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
import { useRouter } from 'vue-router';
import useRole from '@/modules/user-account/composables/useRole';
import type { Role } from '@/modules/user-account/models/role.model';
import utils from '@/utils/msg';

const roles = ref<Role[]>([]);
const { getRoles, deleteRole } = useRole();
const router = useRouter();

const filtro = ref({ NameRol: '' });

const getRolesData = async () => {
  const { Data } = await getRoles(filtro.value.NameRol);
  roles.value = Data;
};

const newRole = () => {
  router.push({ name: 'role-edit', params: { id: '0' } });
};

const editRole = (role: Role) => {
  router.push({ name: 'role-edit', params: { id: role.Id } });
};

const removeRole = async (id: number) => {
  const confirmed = await utils.showMessageQuestion('¿Desea eliminar el rol?');
  if (confirmed) {
    const { ok } = await deleteRole(id);
    if (ok) {
      await utils.showMessageModal({ Description: 'El rol se eliminó correctamente.', MessageType: 'success' });
      await getRolesData();
    }
  }
};
</script>

<style scoped></style>
