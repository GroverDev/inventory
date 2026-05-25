<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Cuenta</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de usuarios</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">
          {{ user.Uuid ? 'Editar Usuario' : 'Nuevo Usuario' }}
        </li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="row g-3">
        <div class="col-12" :class="user.Uuid ? 'col-xl-6' : ''">
          <div id="panel-1" class="panel panel-icon">
            <div class="panel-hdr">
              <h2>
                {{ user.Uuid ? 'Editar' : 'Nuevo' }}
                <span class="fw-300"><i> Usuario</i></span>
              </h2>
              <span
                v-if="user.Uuid"
                class="badge ms-2"
                :class="user.IsActive ? 'bg-success' : 'bg-secondary'"
              >
                {{ user.IsActive ? 'Activo' : 'Inactivo' }}
              </span>
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
                            :disabled="isSaved" @click="saveUser">
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
                        :disabled="isSaved" @click="saveUser">
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

                  <!-- Sección 1: Datos Personales -->
                  <h6 class="text-muted border-bottom pb-2 mb-3">
                    <i class="fal fa-user me-1"></i> Datos Personales
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="FullName">
                        Nombre Completo <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="FullName"
                        name="FullName"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.FullName.$dirty && v$.FullName.$invalid }"
                        placeholder="Nombre Completo"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.FullName.$model"
                      />
                      <small class="invalid-feedback">Debe ingresar el nombre completo.</small>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="Email">
                        Correo Electrónico <span class="text-danger">*</span>
                      </label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">
                          <i class="fal fa-envelope"></i>
                        </span>
                        <input
                          type="email"
                          id="Email"
                          name="Email"
                          class="form-control"
                          :class="{ 'is-invalid': v$.Email.$dirty && v$.Email.$invalid }"
                          placeholder="usuario@ejemplo.com"
                          :disabled="isSaved"
                          autocomplete="off"
                          v-model.trim="v$.Email.$model"
                        />
                        <div class="invalid-feedback" v-if="v$.Email.$dirty && v$.Email.required.$invalid">
                          Debe ingresar el correo electrónico.
                        </div>
                        <div class="invalid-feedback" v-else-if="v$.Email.$dirty && v$.Email.email.$invalid">
                          Formato de correo inválido.
                        </div>
                      </div>
                    </div>
                  </div>

                  <!-- Sección 2: Seguridad -->
                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-shield-alt me-1"></i> Seguridad
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="Password">
                        Contraseña
                        <span v-if="!user.Uuid" class="text-danger">*</span>
                      </label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">
                          <i class="fal fa-lock"></i>
                        </span>
                        <input
                          :type="showPassword ? 'text' : 'password'"
                          id="Password"
                          name="Password"
                          class="form-control"
                          :class="{ 'is-invalid': v$.Password.$dirty && v$.Password.$invalid }"
                          placeholder="Contraseña"
                          :disabled="isSaved"
                          autocomplete="new-password"
                          v-model.trim="v$.Password.$model"
                        />
                        <button
                          type="button"
                          class="btn btn-outline-secondary"
                          tabindex="-1"
                          :title="showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                          @click="showPassword = !showPassword"
                        >
                          <i :class="showPassword ? 'fal fa-eye-slash' : 'fal fa-eye'"></i>
                        </button>
                        <div class="invalid-feedback" v-if="v$.Password.$dirty && v$.Password.required.$invalid">
                          La contraseña es requerida para nuevos usuarios.
                        </div>
                      </div>
                      <small class="text-muted" v-if="user.Uuid">
                        <i class="fal fa-info-circle me-1"></i>Dejar en blanco para mantener la actual.
                      </small>
                    </div>
                    <div class="col-12 col-sm-6 mb-3 d-flex flex-column gap-3 justify-content-center">
                      <div class="form-check form-switch">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          id="IsActive"
                          role="switch"
                          :disabled="isSaved"
                          v-model="user.IsActive"
                        />
                        <label class="form-check-label" for="IsActive">Usuario activo</label>
                      </div>
                      <div class="form-check form-switch">
                        <input
                          type="checkbox"
                          class="form-check-input"
                          id="ChangePassword"
                          role="switch"
                          :disabled="isSaved"
                          v-model="user.ChangePassword"
                        />
                        <label class="form-check-label" for="ChangePassword">
                          Forzar cambio de contraseña en el próximo inicio de sesión
                        </label>
                      </div>
                    </div>
                  </div>

                </form>
              </div>

            </div>
          </div>
        </div>

        <!-- Panel 2: Asignación de Roles (solo en modo edición) -->
        <div v-if="user.Uuid" class="col-12 col-xl-6">
          <div class="panel panel-icon">
            <div class="panel-hdr">
              <h2>Asignación de <span class="fw-300"><i>Roles</i></span></h2>
            </div>
            <div class="panel-container show">

              <!-- Barra de acciones -->
              <div class="panel-content pt-0">
                <div class="row align-items-center">
                  <div class="col-8">
                    <div class="d-none d-md-flex gap-2">
                      <button type="button" class="btn btn-sm btn-primary" @click="saveRoles">
                        <span class="fal fa-save me-1"></span>Guardar Roles
                      </button>
                      <button type="button" class="btn btn-sm btn-outline-secondary" @click="toggleAllRoles">
                        <span class="fal fa-check-square me-1"></span>
                        {{ allRolesSelected ? 'Deseleccionar todo' : 'Seleccionar todo' }}
                      </button>
                    </div>
                    <div class="d-md-none">
                      <div class="btn-group">
                        <button type="button" class="btn btn-primary dropdown-toggle"
                          data-bs-toggle="dropdown" data-bs-display="static" aria-expanded="false">
                          Opciones
                        </button>
                        <div class="dropdown-menu">
                          <button type="button" class="dropdown-item border-bottom border-1" @click="saveRoles">
                            <span class="fal fa-save me-1"></span>Guardar Roles
                          </button>
                          <button type="button" class="dropdown-item" @click="toggleAllRoles">
                            <span class="fal fa-check-square me-1"></span>
                            {{ allRolesSelected ? 'Deseleccionar todo' : 'Seleccionar todo' }}
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="col-4 text-end">
                    <small class="text-muted">
                      <strong>{{ selectedRoleIds.length }}</strong> / {{ allRoles.length }} seleccionados
                    </small>
                  </div>
                </div>
              </div>

              <!-- Checklist de roles -->
              <div class="panel-content pt-0">
                <div v-if="allRoles.length === 0" class="text-center py-4">
                  <i class="fal fa-spinner fa-spin fa-2x text-muted d-block mb-2"></i>
                  <small class="text-muted">Cargando roles...</small>
                </div>
                <div class="row g-2">
                  <div
                    v-for="role in allRoles"
                    :key="role.Id"
                    class="col-12 col-sm-6"
                  >
                    <div class="form-check">
                      <input
                        type="checkbox"
                        class="form-check-input"
                        :id="`role-${role.Id}`"
                        :value="role.Id"
                        v-model="selectedRoleIds"
                      />
                      <label class="form-check-label" :for="`role-${role.Id}`">
                        {{ role.NameRol }}
                        <small v-if="role.Description" class="text-muted d-block">{{ role.Description }}</small>
                      </label>
                    </div>
                  </div>
                </div>
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
import { required, email, requiredIf } from '@vuelidate/validators';
import utils from '@/utils/msg';

import { User } from '@/modules/user-account/models/users.model';
import { Role } from '@/modules/user-account/models/role.model';
import useUser from '@/modules/user-account/composables/useUser';
import useRole from '@/modules/user-account/composables/useRole';

const router = useRouter();
const route = useRoute();

const { getUserById, createUser, updateUser, getUserRoles, assignRolesToUser } = useUser();
const { getRoles } = useRole();

const user = ref(new User());
const isSaved = ref(false);
const showPassword = ref(false);
const allRoles = ref<Role[]>([]);
const selectedRoleIds = ref<number[]>([]);

const allRolesSelected = computed(() =>
  selectedRoleIds.value.length === allRoles.value.length && allRoles.value.length > 0
);

const toggleAllRoles = () => {
  if (allRolesSelected.value) {
    selectedRoleIds.value = [];
  } else {
    selectedRoleIds.value = allRoles.value.map(r => r.Id);
  }
};

const rules = computed(() => ({
  FullName: { required },
  Email: { required, email },
  Password: {
    required: requiredIf(() => !user.value.Uuid || user.value.Uuid === ''),
  },
}));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const v$ = useVuelidate(rules, user as any);

onMounted(async () => {
  const userId = route.params.id as string;
  await loadAllRoles();
  if (userId && userId !== '0') {
    await getUser(userId);
    await loadUserRoles(userId);
  } else {
    user.value.Uuid = '';
  }
});

const getUser = async (userId: string) => {
  const { ok, Data: userResp } = await getUserById(userId);
  if (ok) {
    user.value = userResp;
    user.value.Password = '';
  }
};

const loadAllRoles = async () => {
  const { ok, Data } = await getRoles('', '');
  if (ok) allRoles.value = Data;
};

const loadUserRoles = async (uuid: string) => {
  const { ok, Data } = await getUserRoles(uuid);
  if (ok) selectedRoleIds.value = Data.map((r: Role) => r.Id);
};

const saveRoles = async () => {
  const confirmed = await utils.showMessageQuestion('¿Desea guardar la asignación de roles?');
  if (!confirmed) return;

  const { ok } = await assignRolesToUser(user.value.Uuid, selectedRoleIds.value);
  if (ok) {
    await utils.showMessageModal({ Description: 'Los roles se asignaron correctamente.', MessageType: 'success' });
  }
};

const returnPage = () => {
  router.push({ name: 'users-admin' });
};

const saveUser = async () => {
  const isFormCorrect = await v$.value.$validate();
  if (!isFormCorrect) return;

  const respuesta = await utils.showMessageQuestion('¿Desea guardar el usuario?');

  if (respuesta) {
    if (!user.value.Uuid || user.value.Uuid === '0') {
      const { ok, Data: idUser } = await createUser(user.value);
      if (ok) {
        isSaved.value = true;
        user.value.Uuid = idUser;
        await utils.showMessageModal({ Description: 'El usuario se creó correctamente.', MessageType: 'success' });
        returnPage();
      }
    } else {
      const { ok, Data: okResp } = await updateUser(user.value);
      if (ok && okResp) {
        await utils.showMessageModal({ Description: 'El usuario se actualizó correctamente.', MessageType: 'success' });
        returnPage();
      }
    }
  }
};
</script>

<style scoped></style>
