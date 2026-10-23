<template>
  <Layout>
    <PageHeader title="Registro de Roles" page-title="Seguridad" />

    <div class="card">
        <div class="card-header">
          <h5 class="card-title mb-0">Gestión de <span class="fw-light"><i>ROLES</i></span></h5>
        </div>
        <div class="pt-0 card-body">

            <!-- Botón Nuevo -->
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newRole">
                <span class="ri-add-box-line me-1"></span>Nuevo Rol
              </button>
            </div>

            <!-- Toolbar: búsqueda -->
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre del rol</label>
                <div class="input-group rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="ri-search-line text-success"></i>
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
                <span class="ri-list-check me-1"></span>
                <strong>{{ roles.length }}</strong> rol(es) encontrado(s)
              </small>
            </div>

            <!-- Estado vacío -->
            <div v-if="roles.length === 0" class="text-center py-5">
              <i class="ri-shield-user-line ri-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar roles en el sistema</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newRole">
                <span class="ri-add-line me-1"></span>Crear nuevo rol
              </button>
            </div>

            <!-- Resultados -->
            <template v-else>

              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead>
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
                          <span class="ri-edit-2-line"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeRole(role.Id)"
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
                  <div class="col-12 col-sm-6" v-for="(role, index) in roles" :key="index">
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <p class="fw-semibold mb-0 lh-sm">{{ role.NameRol }}</p>
                          <span class="badge rounded-pill" :class="role.State ? 'text-bg-success' : 'text-bg-secondary'">
                            {{ role.State ? 'Activo' : 'Inactivo' }}
                          </span>
                        </div>
                        <small class="text-muted">{{ role.Description }}</small>
                        <div class="d-flex gap-2 mt-auto pt-1">
                          <button type="button" class="btn btn-sm btn-outline-primary flex-grow-1" @click="editRole(role)">
                            <span class="ri-edit-2-line me-1"></span>Editar
                          </button>
                          <button type="button" class="btn btn-sm btn-outline-danger" @click="removeRole(role.Id)">
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
