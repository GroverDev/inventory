<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Administración</li>
        <li class="breadcrumb-item active" aria-current="page">Resetear Empresa</li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr bg-danger-50">
          <h2 class="text-danger">
            <i class="fal fa-exclamation-triangle me-2"></i>
            Reiniciar base de datos <span class="fw-300"><i>(empresa nueva)</i></span>
          </h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content">

            <!-- Advertencia -->
            <div class="alert alert-danger d-flex align-items-start" role="alert">
              <i class="fal fa-radiation-alt fa-2x me-3 mt-1"></i>
              <div>
                <strong>Esta acción es irreversible.</strong>
                Se eliminarán <u>todos los datos</u> de la empresa actual: productos, clientes,
                proveedores, ventas, compras, inventario, caja, facturación y <u>usuarios</u>.
                Se conservan únicamente la estructura de menús y permisos. Al finalizar,
                deberá iniciar sesión con el nuevo administrador que defina aquí.
              </div>
            </div>

            <form @submit.prevent="onSubmit" autocomplete="off">
              <div class="row g-4">
                <!-- Nuevo administrador -->
                <div class="col-12 col-lg-6">
                  <div class="card h-100">
                    <div class="card-header fw-semibold">
                      <i class="fal fa-user-shield me-1"></i> Nuevo administrador
                    </div>
                    <div class="card-body">
                      <div class="mb-3">
                        <label class="form-label">Nombre completo</label>
                        <input type="text" class="form-control" v-model.trim="form.NewAdminFullName"
                               placeholder="Nombre del administrador" autocomplete="off" />
                      </div>
                      <div class="mb-3">
                        <label class="form-label">Correo electrónico</label>
                        <input type="email" class="form-control" v-model.trim="form.NewAdminEmail"
                               placeholder="admin@empresa.com" autocomplete="off" />
                      </div>
                      <div class="mb-3">
                        <label class="form-label">Contraseña</label>
                        <input type="password" class="form-control" v-model="form.NewAdminPassword"
                               placeholder="Mínimo 6 caracteres" autocomplete="new-password" />
                      </div>
                      <div class="mb-0">
                        <label class="form-label">Repetir contraseña</label>
                        <input type="password" class="form-control" v-model="confirmPassword"
                               placeholder="Repita la contraseña" autocomplete="new-password" />
                        <small v-if="confirmPassword && confirmPassword !== form.NewAdminPassword"
                               class="text-danger">Las contraseñas no coinciden.</small>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Confirmación de seguridad -->
                <div class="col-12 col-lg-6">
                  <div class="card h-100 border-danger">
                    <div class="card-header fw-semibold text-danger">
                      <i class="fal fa-shield-check me-1"></i> Confirmación de seguridad
                    </div>
                    <div class="card-body">
                      <div class="mb-3">
                        <label class="form-label">Su contraseña actual (SuperAdmin)</label>
                        <input type="password" class="form-control" v-model="form.CurrentPassword"
                               placeholder="Contraseña de su usuario" autocomplete="current-password" />
                      </div>
                      <div class="mb-3">
                        <label class="form-label">
                          Escriba <code class="text-danger">{{ expectedPhrase }}</code> para confirmar
                        </label>
                        <input type="text" class="form-control"
                               :class="phraseOk ? 'is-valid' : (form.ConfirmationPhrase ? 'is-invalid' : '')"
                               v-model="form.ConfirmationPhrase" placeholder="RESETEAR EMPRESA" autocomplete="off" />
                      </div>
                      <div class="form-check form-switch mb-0">
                        <input class="form-check-input" type="checkbox" id="chkBackup" v-model="createBackup" />
                        <label class="form-check-label" for="chkBackup">
                          Generar respaldo antes de reiniciar
                          <small class="text-muted d-block">
                            Copia todos los datos a un esquema <code>backup_&lt;fecha&gt;</code> en la misma base.
                          </small>
                        </label>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div class="d-flex justify-content-end gap-2 mt-4">
                <button type="button" class="btn btn-outline-secondary" @click="goBack">Cancelar</button>
                <button type="submit" class="btn btn-danger" :disabled="!canSubmit">
                  <i class="fal fa-trash-restore-alt me-1"></i> Reiniciar empresa
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
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import useAdmin from '@/modules/user-account/composables/useAdmin';
import { useAuthStore } from '@/modules/auth/stores/auth.store';
import utils from '@/utils/msg';

const router = useRouter();
const authStore = useAuthStore();
const { resetCompany } = useAdmin();

const expectedPhrase = 'RESETEAR EMPRESA';

const form = ref({
  NewAdminFullName: '',
  NewAdminEmail: '',
  NewAdminPassword: '',
  CurrentPassword: '',
  ConfirmationPhrase: '',
});
const confirmPassword = ref('');
const createBackup = ref(true);

const phraseOk = computed(() => form.value.ConfirmationPhrase.trim() === expectedPhrase);

const canSubmit = computed(() =>
  form.value.NewAdminFullName.length > 0 &&
  form.value.NewAdminEmail.length > 0 &&
  form.value.NewAdminPassword.length >= 6 &&
  confirmPassword.value === form.value.NewAdminPassword &&
  form.value.CurrentPassword.length > 0 &&
  phraseOk.value
);

const goBack = () => router.back();

const onSubmit = async () => {
  if (!canSubmit.value) return;

  const confirmed = await utils.showMessageQuestion(
    '¿Está TOTALMENTE seguro? Se eliminarán todos los datos de la empresa actual de forma irreversible.'
  );
  if (!confirmed) return;

  const { ok, Message } = await resetCompany({
    CurrentPassword: form.value.CurrentPassword,
    ConfirmationPhrase: form.value.ConfirmationPhrase.trim(),
    NewAdminEmail: form.value.NewAdminEmail,
    NewAdminFullName: form.value.NewAdminFullName,
    NewAdminPassword: form.value.NewAdminPassword,
    SkipBackup: !createBackup.value,
  });

  if (ok) {
    await utils.showMessage(Message);
    // El usuario actual fue eliminado: cerrar sesión y volver al login.
    authStore.logout();
    router.push({ name: 'login' });
  }
};
</script>

<style scoped></style>
