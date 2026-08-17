<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Administración</li>
        <li class="breadcrumb-item active" aria-current="page">Nueva Empresa</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="row">
        <div class="col">
          <div id="panel-1" class="panel panel-icon">
            <div class="panel-hdr">
              <h2>Alta de <span class="fw-300"><i>Empresa</i></span></h2>
            </div>
            <div class="panel-container show">

              <!-- Barra de acciones -->
              <div class="panel-content pt-0">
                <div class="row align-items-center">
                  <div class="col-8 col-md-8">
                    <div v-if="!creada" class="d-md-none">
                      <div class="btn-group">
                        <button type="button" class="btn btn-primary dropdown-toggle"
                          data-bs-toggle="dropdown" data-bs-display="static" aria-expanded="false">
                          Opciones
                        </button>
                        <div class="dropdown-menu dropdown-menu-lg-right">
                          <button type="button" class="dropdown-item border-bottom border-1"
                            :disabled="guardando" @click="crear">
                            <span class="fal fa-save me-1"></span>Crear empresa
                          </button>
                          <button type="button" class="dropdown-item border-bottom border-1"
                            @click="returnPage">
                            <span class="fal fa-ban me-1"></span>Cancelar
                          </button>
                        </div>
                      </div>
                    </div>
                    <div v-if="!creada" class="d-none d-md-flex gap-2">
                      <button type="button" class="btn btn-sm btn-primary"
                        :disabled="guardando" @click="crear">
                        <span v-if="guardando" class="spinner-border spinner-border-sm me-1"></span>
                        <span v-else class="fal fa-save me-1"></span>Crear empresa
                      </button>
                      <button type="button" class="btn btn-warning btn-sm" @click="returnPage">
                        <span class="fal fa-ban me-1"></span>Cancelar
                      </button>
                    </div>
                    <button v-else type="button" class="btn btn-sm btn-primary" @click="otra">
                      <span class="fal fa-plus me-1"></span>Crear otra
                    </button>
                  </div>
                  <div class="col-4 col-md-4 text-md-end">
                    <button type="button" class="btn btn-danger btn-sm" @click="returnPage">
                      <span class="fal fa-arrow-alt-to-left me-1"></span>Volver
                    </button>
                  </div>
                </div>
              </div>

              <!--
                El resultado reemplaza al formulario en vez de ser un aviso: el
                identificador y el correo del administrador hay que anotarlos o
                comunicarlos, así que no pueden desaparecer con un clic.
              -->
              <div v-if="creada" class="panel-content pt-0">
                <div class="text-center py-4">
                  <i class="fal fa-check-circle fa-3x text-success d-block mb-3"></i>
                  <h5 class="mb-3">Farmacia «{{ creada.Name }}» creada</h5>
                  <div class="d-inline-block text-start border rounded p-3 mb-3">
                    <div class="mb-1"><span class="text-muted">Identificador:</span> <code>{{ creada.Slug }}</code></div>
                    <div class="mb-1"><span class="text-muted">Administrador:</span> <strong>{{ creada.AdminEmail }}</strong></div>
                    <div><span class="text-muted">Nº de empresa:</span> {{ creada.TenantId }}</div>
                  </div>
                  <p class="text-muted small mb-0">
                    El administrador entra con ese correo y la contraseña que definiste.
                    El sistema le exigirá cambiarla en su primer ingreso.
                  </p>
                </div>
              </div>

              <!-- Formulario -->
              <div v-else class="panel-content pt-0">
                <form novalidate autocomplete="off">

                  <div class="alert alert-info py-2">
                    <i class="fal fa-info-circle me-1"></i>
                    Crea una farmacia nueva, aislada del resto, con su propio administrador
                    y sus datos iniciales. Es una operación de plataforma: si tu usuario no
                    la tiene habilitada, el servidor la rechazará.
                  </div>

                  <h6 class="text-muted border-bottom pb-2 mb-3">
                    <i class="fal fa-store me-1"></i> La farmacia
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="Name">
                        Nombre comercial <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="Name"
                        name="Name"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.Name.$dirty && v$.Name.$invalid }"
                        placeholder="Ej: Farmacia San José"
                        maxlength="150"
                        :disabled="guardando"
                        v-model.trim="v$.Name.$model"
                        @input="onNombre"
                      />
                      <small class="invalid-feedback">Debe ingresar el nombre de la farmacia.</small>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="Slug">
                        Identificador <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="Slug"
                        name="Slug"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.Slug.$dirty && v$.Slug.$invalid }"
                        placeholder="farmacia-san-jose"
                        maxlength="60"
                        :disabled="guardando"
                        v-model.trim="v$.Slug.$model"
                        @input="slugTocado = true"
                      />
                      <small class="invalid-feedback"
                        v-if="v$.Slug.$dirty && v$.Slug.required.$invalid">
                        Debe ingresar el identificador.
                      </small>
                      <small class="invalid-feedback"
                        v-else-if="v$.Slug.$dirty && v$.Slug.formatoSlug.$invalid">
                        Solo minúsculas, números y guiones (ej: farmacia-central).
                      </small>
                      <small v-else class="text-muted">
                        Se usa en URL o subdominio. Se propone a partir del nombre.
                      </small>
                    </div>
                  </div>

                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-user-shield me-1"></i> Su administrador
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="AdminFullName">
                        Nombre completo <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="AdminFullName"
                        name="AdminFullName"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.AdminFullName.$dirty && v$.AdminFullName.$invalid }"
                        maxlength="100"
                        :disabled="guardando"
                        v-model.trim="v$.AdminFullName.$model"
                      />
                      <small class="invalid-feedback">Debe ingresar el nombre del administrador.</small>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="AdminEmail">
                        Correo <span class="text-danger">*</span>
                      </label>
                      <input
                        type="email"
                        id="AdminEmail"
                        name="AdminEmail"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.AdminEmail.$dirty && v$.AdminEmail.$invalid }"
                        maxlength="150"
                        :disabled="guardando"
                        v-model.trim="v$.AdminEmail.$model"
                      />
                      <small class="invalid-feedback"
                        v-if="v$.AdminEmail.$dirty && v$.AdminEmail.required.$invalid">
                        Debe ingresar el correo.
                      </small>
                      <small class="invalid-feedback"
                        v-else-if="v$.AdminEmail.$dirty && v$.AdminEmail.email.$invalid">
                        El formato del correo no es válido.
                      </small>
                      <small v-else class="text-muted">Es también su nombre de usuario.</small>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="AdminPassword">
                        Contraseña inicial <span class="text-danger">*</span>
                      </label>
                      <div class="input-group input-group-sm">
                        <input
                          :type="verClave ? 'text' : 'password'"
                          id="AdminPassword"
                          name="AdminPassword"
                          class="form-control form-control-sm"
                          :class="{ 'is-invalid': v$.AdminPassword.$dirty && v$.AdminPassword.$invalid }"
                          maxlength="50"
                          autocomplete="new-password"
                          :disabled="guardando"
                          v-model="v$.AdminPassword.$model"
                        />
                        <button class="btn btn-outline-secondary" type="button"
                          :title="verClave ? 'Ocultar' : 'Mostrar'"
                          @click="verClave = !verClave">
                          <span class="fal" :class="verClave ? 'fa-eye-slash' : 'fa-eye'"></span>
                        </button>
                        <div class="invalid-feedback">
                          La contraseña debe tener al menos 8 caracteres.
                        </div>
                      </div>
                      <small class="text-muted">
                        Se le exigirá cambiarla al primer ingreso.
                      </small>
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
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import useVuelidate from '@vuelidate/core';
import { email, helpers, minLength, required } from '@vuelidate/validators';
import utils from '@/utils/msg';
import useAdmin, { type CreateTenantResult } from '@/modules/user-account/composables/useAdmin';

const router = useRouter();
const { createTenant } = useAdmin();

const form = ref({
  Name: '',
  Slug: '',
  AdminEmail: '',
  AdminFullName: '',
  AdminPassword: '',
});

/** Mismo patrón que exige el validador del servidor. */
const formatoSlug = helpers.regex(/^[a-z0-9]+(-[a-z0-9]+)*$/);

const rules = {
  Name: { required },
  Slug: { required, formatoSlug },
  AdminFullName: { required },
  AdminEmail: { required, email },
  AdminPassword: { required, minLength: minLength(8) },
};

const v$ = useVuelidate(rules, form);

const guardando = ref(false);
const verClave = ref(false);
const creada = ref<CreateTenantResult | null>(null);
/** El identificador deja de seguir al nombre en cuanto se edita a mano. */
const slugTocado = ref(false);

/** Propone el identificador a partir del nombre: nadie quiere tipearlo dos veces. */
const onNombre = () => {
  if (slugTocado.value) return;
  form.value.Slug = form.value.Name
    .toLowerCase()
    .normalize('NFD').replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
};

const crear = async () => {
  // Se marcan todos los campos para que el que falta se pinte de rojo con su
  // motivo, en vez de dejar el botón apagado sin explicar por qué.
  const valido = await v$.value.$validate();
  if (!valido) return;

  const ok = await utils.showMessageQuestion(
    `¿Crear la farmacia "${form.value.Name}" con administrador ${form.value.AdminEmail}?`
  );
  if (!ok) return;

  guardando.value = true;
  try {
    const { ok: creado, Data } = await createTenant({ ...form.value });
    if (creado) creada.value = Data;
  } finally {
    guardando.value = false;
  }
};

const otra = () => {
  form.value = { Name: '', Slug: '', AdminEmail: '', AdminFullName: '', AdminPassword: '' };
  slugTocado.value = false;
  creada.value = null;
  v$.value.$reset();
};

const returnPage = () => router.push({ name: 'inventory-dashboard' });
</script>

<style scoped></style>
