<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Cuenta</li>
        <li class="breadcrumb-item active" aria-current="page">Registro de usuarios</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>USUARIOS</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <!-- Botón Nuevo -->
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newUser">
                <span class="fal fa-plus-square me-1"></span>Nuevo Usuario
              </button>
            </div>
            
            <!-- Toolbar: búsqueda + nuevo -->
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre del usuario</label>
                <div class="input-group input-group body-bg shadow-inset-2 rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="sa sa-magnifier text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="filtro.FullName"
                    placeholder="Ingrese el nombre del usuario..."
                    autocomplete="off"
                    @keyup.enter="getUsers"
                  />
                  <button class="btn btn-primary" type="button" @click="getUsers">Buscar</button>
                </div>
              </div>
            </div>

            <!-- Contador de resultados -->
            <div v-if="users.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="fal fa-list me-1"></span>
                <strong>{{ users.length }}</strong> usuario(s) encontrado(s)
              </small>
            </div>

            <!-- Estado vacío -->
            <div v-if="users.length === 0" class="text-center py-5">
              <i class="fal fa-users fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar usuarios en el sistema</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newUser">
                <span class="fal fa-plus me-1"></span>Crear nuevo usuario
              </button>
            </div>

            <!-- Resultados -->
            <template v-else>

              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th>Nombre Completo</th>
                      <th>Correo Electrónico</th>
                      <th class="d-none d-lg-table-cell text-center">Último Acceso</th>
                      <th class="text-center">Estado</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(user, index) in users" :key="index">
                      <td class="fw-semibold">{{ user.FullName }}</td>
                      <td><small class="text-muted">{{ user.Email }}</small></td>
                      <td class="d-none d-lg-table-cell text-center">
                        <small class="text-muted">{{ formatDate(user.LastAccess) }}</small>
                      </td>
                      <td class="text-center">
                        <span class="badge" :class="user.IsActive ? 'bg-success' : 'bg-secondary'">
                          {{ user.IsActive ? 'Activo' : 'Inactivo' }}
                        </span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Editar"
                          @click="editUser(user)"
                        >
                          <span class="fal fa-edit"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeUser(user.Uuid)"
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
                  <div class="col-12 col-sm-6" v-for="(user, index) in users" :key="index">
                    <div class="card h-100">
                      <div class="card-body d-flex flex-column">
                        <div class="d-flex justify-content-between align-items-start mb-1">
                          <h6 class="card-title mb-0">{{ user.FullName }}</h6>
                          <span class="badge ms-2" :class="user.IsActive ? 'bg-success' : 'bg-secondary'">
                            {{ user.IsActive ? 'Activo' : 'Inactivo' }}
                          </span>
                        </div>
                        <small class="text-muted mb-1">{{ user.Email }}</small>
                        <small v-if="user.LastAccess" class="text-muted mb-3">
                          <i class="fal fa-clock me-1"></i>{{ formatDate(user.LastAccess) }}
                        </small>
                        <div class="mt-auto">
                          <div class="btn-group w-100" role="group">
                            <button type="button" class="btn btn-outline-primary btn-sm"
                              @click="editUser(user)">
                              <span class="fal fa-edit me-1"></span>Editar
                            </button>
                            <button type="button" class="btn btn-outline-danger btn-sm"
                              @click="removeUser(user.Uuid)">
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
import useUser from '@/modules/user-account/composables/useUser';
import type { User } from '@/modules/user-account/models/users.model';
import utils from '@/utils/msg';

const users = ref<User[]>([]);
const { getUsersByName, deleteUser } = useUser();
const router = useRouter();

const filtro = ref({
  FullName: '',
  Email: '',
});

const formatDate = (date?: Date): string => {
  if (!date) return '—';
  return new Date(date).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const getUsers = async () => {
  const { Data: usersResp } = await getUsersByName(filtro.value.FullName, filtro.value.Email);
  users.value = usersResp;
};

const newUser = () => {
  router.push({ name: 'user-edit', params: { id: '0' } });
};

const editUser = (user: User) => {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const userId = user.Uuid || (user as any).Uuid;
  if (!userId) {
    utils.showMessageModal({ Description: 'Error: No se pudo obtener el ID del usuario.', MessageType: 'error' });
    return;
  }
  router.push({ name: 'user-edit', params: { id: userId } });
};

const removeUser = async (id: string) => {
  const respuesta = await utils.showMessageQuestion('¿Desea eliminar el usuario?');
  if (respuesta) {
    const { ok } = await deleteUser(id);
    if (ok) {
      await utils.showMessageModal({ Description: 'El usuario se eliminó correctamente.', MessageType: 'success' });
      await getUsers();
    }
  }
};
</script>

<style scoped></style>
