<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Administración</li>
        <li class="breadcrumb-item active" aria-current="page">Nueva Empresa</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Alta de <span class="fw-300"><i>Empresa</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content">

            <!-- Resultado: reemplaza al formulario, porque el alta no se repite. -->
            <div v-if="creada" class="text-center py-4">
              <i class="fal fa-check-circle fa-3x text-success d-block mb-3"></i>
              <h5 class="mb-3">Farmacia «{{ creada.Name }}» creada</h5>
              <div class="d-inline-block text-start border rounded p-3 mb-3">
                <div class="mb-1"><span class="text-muted">Identificador:</span> <code>{{ creada.Slug }}</code></div>
                <div class="mb-1"><span class="text-muted">Administrador:</span> <strong>{{ creada.AdminEmail }}</strong></div>
                <div><span class="text-muted">Nº de empresa:</span> {{ creada.TenantId }}</div>
              </div>
              <p class="text-muted small mb-3">
                El administrador entra con ese correo y la contraseña que definiste.
                El sistema le exigirá cambiarla en su primer ingreso.
              </p>
              <button type="button" class="btn btn-primary btn-sm" @click="otra">
                <span class="fal fa-plus me-1"></span>Crear otra
              </button>
            </div>

            <form v-else @submit.prevent="crear" autocomplete="off">
              <div class="alert alert-info py-2">
                <i class="fal fa-info-circle me-1"></i>
                Crea una farmacia nueva, aislada del resto, con su propio
                administrador y sus datos iniciales. Es una operación de
                plataforma: si tu usuario no la tiene habilitada, el servidor la
                rechazará.
              </div>

              <h6 class="text-muted border-bottom pb-2 mb-3">
                <i class="fal fa-store me-1"></i> La farmacia
              </h6>
              <div class="row">
                <div class="col-12 col-md-6 mb-3">
                  <label class="form-label d-block" for="nombre">
                    Nombre comercial <span class="text-danger">*</span>
                  </label>
                  <input id="nombre" type="text" class="form-control form-control-sm"
                    maxlength="150" v-model.trim="form.Name" :disabled="guardando"
                    @input="onNombre" />
                </div>
                <div class="col-12 col-md-6 mb-3">
                  <label class="form-label d-block" for="slug">
                    Identificador <span class="text-danger">*</span>
                  </label>
                  <input id="slug" type="text" class="form-control form-control-sm"
                    maxlength="60" v-model.trim="form.Slug" :disabled="guardando"
                    :class="{ 'is-invalid': form.Slug !== '' && !slugValido }"
                    @input="slugTocado = true" />
                  <small v-if="form.Slug !== '' && !slugValido" class="invalid-feedback d-block">
                    Solo minúsculas, números y guiones (ej: <code>farmacia-central</code>).
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
                <div class="col-12 col-md-6 mb-3">
                  <label class="form-label d-block" for="admin-nombre">
                    Nombre completo <span class="text-danger">*</span>
                  </label>
                  <input id="admin-nombre" type="text" class="form-control form-control-sm"
                    maxlength="100" v-model.trim="form.AdminFullName" :disabled="guardando" />
                </div>
                <div class="col-12 col-md-6 mb-3">
                  <label class="form-label d-block" for="admin-email">
                    Correo <span class="text-danger">*</span>
                  </label>
                  <input id="admin-email" type="email" class="form-control form-control-sm"
                    maxlength="150" v-model.trim="form.AdminEmail" :disabled="guardando" />
                  <small class="text-muted">Es también su nombre de usuario.</small>
                </div>
                <div class="col-12 col-md-6 mb-3">
                  <label class="form-label d-block" for="admin-pass">
                    Contraseña inicial <span class="text-danger">*</span>
                  </label>
                  <div class="input-group input-group-sm">
                    <input id="admin-pass" :type="verClave ? 'text' : 'password'"
                      class="form-control form-control-sm" maxlength="50"
                      autocomplete="new-password"
                      v-model="form.AdminPassword" :disabled="guardando" />
                    <button class="btn btn-outline-secondary" type="button"
                      :title="verClave ? 'Ocultar' : 'Mostrar'"
                      @click="verClave = !verClave">
                      <span class="fal" :class="verClave ? 'fa-eye-slash' : 'fa-eye'"></span>
                    </button>
                  </div>
                  <small class="text-muted">
                    Mínimo 8 caracteres. Se le exigirá cambiarla al primer ingreso.
                  </small>
                </div>
              </div>

              <div class="d-flex gap-2 mt-3">
                <button type="submit" class="btn btn-sm btn-success" :disabled="guardando || !completo">
                  <span v-if="guardando" class="spinner-border spinner-border-sm me-1"></span>
                  <span v-else class="fal fa-save me-1"></span>
                  Crear empresa
                </button>
                <button type="button" class="btn btn-sm btn-warning" :disabled="guardando" @click="volver">
                  <span class="fal fa-ban me-1"></span>Cancelar
                </button>
              </div>
            </form>

          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter } from 'vue-router';
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

const guardando = ref(false);
const verClave = ref(false);
const creada = ref<CreateTenantResult | null>(null);
/** El slug deja de seguir al nombre en cuanto el usuario lo edita a mano. */
const slugTocado = ref(false);

/** Mismo patrón que exige el validador del servidor. */
const slugValido = computed(() => /^[a-z0-9]+(-[a-z0-9]+)*$/.test(form.value.Slug));

const completo = computed(() =>
  form.value.Name !== '' &&
  slugValido.value &&
  form.value.AdminFullName !== '' &&
  form.value.AdminEmail !== '' &&
  form.value.AdminPassword.length >= 8
);

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
  if (!completo.value) return;

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
};

const volver = () => router.push({ name: 'inventory-dashboard' });
</script>

<style scoped></style>
