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
      <div class="row">
        <div class="col">
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
import useUser from '@/modules/user-account/composables/useUser';

const router = useRouter();
const route = useRoute();

const { getUserById, createUser, updateUser } = useUser();

const user = ref(new User());
const isSaved = ref(false);
const showPassword = ref(false);

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
  if (userId && userId !== '0') {
    await getUser(userId);
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
