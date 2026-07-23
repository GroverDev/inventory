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
              <div class="col-12 col-md-5 col-lg-6">
                <div class="form-check mt-2 mt-md-0">
                  <input
                    id="chkIncludeInactive"
                    class="form-check-input"
                    type="checkbox"
                    v-model="filtro.IncludeInactive"
                    @change="getUsers"
                  />
                  <label class="form-check-label" for="chkIncludeInactive">
                    Mostrar también usuarios inactivos
                  </label>
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
                  <thead class="">
                    <tr>
                      <th>Nombre Completo</th>
                      <th>Correo Electrónico</th>
                      <th class="d-none d-lg-table-cell text-center">Último Acceso</th>
                      <th class="text-center">Estado</th>
                      <th class="text-center">TOTP</th>
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
                      <td class="text-center">
                        <span v-if="user.MfaEnabled" class="badge bg-success" title="TOTP configurado y activo">
                          <i class="fal fa-shield-check me-1"></i>Activo
                        </span>
                        <span v-else-if="user.MfaRequired" class="badge bg-warning text-dark" title="TOTP requerido, pendiente de configurar">
                          <i class="fal fa-exclamation-triangle me-1"></i>Requerido
                        </span>
                        <span v-else class="badge bg-secondary" title="Sin TOTP">
                          <i class="fal fa-shield me-1"></i>Sin TOTP
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
                          class="btn btn-outline-secondary btn-sm me-1"
                          title="Cambiar contraseña"
                          @click="openPasswordModal(user)"
                        >
                          <span class="fal fa-key"></span>
                        </button>
                        <button
                          v-if="!user.MfaEnabled && !user.MfaRequired"
                          type="button"
                          class="btn btn-outline-warning btn-sm me-1"
                          title="Requerir TOTP al usuario"
                          @click="requireMfa(user)"
                        >
                          <span class="fal fa-shield-alt"></span>
                        </button>
                        <button
                          v-if="user.MfaRequired && !user.MfaEnabled"
                          type="button"
                          class="btn btn-outline-secondary btn-sm me-1"
                          title="Quitar obligatoriedad de TOTP"
                          @click="unrequireMfa(user)"
                        >
                          <span class="fal fa-shield"></span>
                        </button>
                        <button
                          v-if="user.MfaEnabled"
                          type="button"
                          class="btn btn-outline-danger btn-sm me-1"
                          title="Resetear TOTP (deshabilita y limpia la configuración)"
                          @click="resetMfa(user)"
                        >
                          <span class="fal fa-undo"></span>
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
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <p class="fw-semibold mb-0 lh-sm">{{ user.FullName }}</p>
                          <div class="d-flex gap-1">
                            <span class="badge rounded-pill" :class="user.IsActive ? 'text-bg-success' : 'text-bg-secondary'">
                              {{ user.IsActive ? 'Activo' : 'Inactivo' }}
                            </span>
                            <span v-if="user.MfaEnabled" class="badge rounded-pill text-bg-success" title="TOTP activo">
                              <i class="fal fa-shield-check"></i>
                            </span>
                            <span v-else-if="user.MfaRequired" class="badge rounded-pill text-bg-warning" title="TOTP requerido">
                              <i class="fal fa-exclamation-triangle"></i>
                            </span>
                          </div>
                        </div>
                        <small class="text-muted">{{ user.Email }}</small>
                        <small v-if="user.LastAccess" class="text-muted">
                          <i class="fal fa-clock me-1"></i>{{ formatDate(user.LastAccess) }}
                        </small>
                        <div class="mt-auto d-flex flex-column gap-1 pt-1">
                          <div class="d-flex gap-1">
                            <button type="button" class="btn btn-sm btn-outline-primary flex-grow-1" @click="editUser(user)">
                              <span class="fal fa-edit me-1"></span>Editar
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-secondary" @click="openPasswordModal(user)" title="Cambiar contraseña">
                              <span class="fal fa-key"></span>
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-danger" @click="removeUser(user.Uuid)" title="Eliminar">
                              <span class="fal fa-trash-alt"></span>
                            </button>
                          </div>
                          <button v-if="!user.MfaEnabled && !user.MfaRequired"
                            type="button" class="btn btn-sm btn-outline-warning w-100" @click="requireMfa(user)">
                            <span class="fal fa-shield-alt me-1"></span>Requerir TOTP
                          </button>
                          <button v-if="user.MfaRequired && !user.MfaEnabled"
                            type="button" class="btn btn-sm btn-outline-secondary w-100" @click="unrequireMfa(user)">
                            <span class="fal fa-shield me-1"></span>Quitar obligatoriedad
                          </button>
                          <button v-if="user.MfaEnabled"
                            type="button" class="btn btn-sm btn-outline-danger w-100" @click="resetMfa(user)">
                            <span class="fal fa-undo me-1"></span>Resetear TOTP
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

  <!-- Modal cambio de contraseña -->
  <div v-if="passwordModal.show" class="modal d-block" tabindex="-1" style="background:rgba(0,0,0,.5)">
    <div class="modal-dialog modal-dialog-centered" style="max-width:400px">
      <div class="modal-content">
        <div class="modal-header py-2">
          <h6 class="modal-title fw-bold">
            <i class="fal fa-key me-2"></i>Cambiar contraseña — {{ passwordModal.userName }}
          </h6>
          <button type="button" class="btn-close" @click="closePasswordModal"></button>
        </div>
        <div class="modal-body">
          <div class="mb-3">
            <label class="form-label form-label-sm">Nueva contraseña <span class="text-danger">*</span></label>
            <div class="input-group input-group-sm">
              <span class="input-group-text bg-transparent"><i class="fal fa-lock"></i></span>
              <input
                :type="passwordModal.showPwd ? 'text' : 'password'"
                class="form-control"
                v-model.trim="passwordModal.newPassword"
                placeholder="Mínimo 6 caracteres"
                autocomplete="new-password"
              />
              <button type="button" class="btn btn-outline-secondary" tabindex="-1"
                @click="passwordModal.showPwd = !passwordModal.showPwd">
                <i :class="passwordModal.showPwd ? 'fal fa-eye-slash' : 'fal fa-eye'"></i>
              </button>
            </div>
          </div>
          <div class="mb-1">
            <label class="form-label form-label-sm">Confirmar contraseña <span class="text-danger">*</span></label>
            <div class="input-group input-group-sm">
              <span class="input-group-text bg-transparent"><i class="fal fa-lock"></i></span>
              <input
                :type="passwordModal.showPwd ? 'text' : 'password'"
                class="form-control"
                :class="{ 'is-invalid': passwordModal.confirm && passwordModal.newPassword !== passwordModal.confirm }"
                v-model.trim="passwordModal.confirm"
                placeholder="Repite la contraseña"
                autocomplete="new-password"
              />
              <div class="invalid-feedback">Las contraseñas no coinciden.</div>
            </div>
          </div>
        </div>
        <div class="modal-footer py-2">
          <button class="btn btn-outline-secondary btn-sm" @click="closePasswordModal">Cancelar</button>
          <button class="btn btn-primary btn-sm" @click="savePassword">
            <i class="fal fa-save me-1"></i>Guardar
          </button>
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
const { getUsersByName, deleteUser, changeUserPassword, adminResetMfa, adminRequireMfa, adminUnrequireMfa } = useUser();
const router = useRouter();

const passwordModal = ref({
  show: false,
  uuid: '',
  userName: '',
  newPassword: '',
  confirm: '',
  showPwd: false,
});

const openPasswordModal = (user: User) => {
  passwordModal.value = { show: true, uuid: user.Uuid, userName: user.FullName, newPassword: '', confirm: '', showPwd: false };
};

const closePasswordModal = () => {
  passwordModal.value.show = false;
};

const savePassword = async () => {
  if (!passwordModal.value.newPassword || passwordModal.value.newPassword.length < 6) {
    await utils.showMessageModal({ Description: 'La contraseña debe tener al menos 6 caracteres.', MessageType: 'warning' });
    return;
  }
  if (passwordModal.value.newPassword !== passwordModal.value.confirm) {
    await utils.showMessageModal({ Description: 'Las contraseñas no coinciden.', MessageType: 'warning' });
    return;
  }
  const ok = await utils.showMessageQuestion(`¿Cambiar la contraseña de ${passwordModal.value.userName}?`);
  if (!ok) return;
  const resp = await changeUserPassword(passwordModal.value.uuid, passwordModal.value.newPassword);
  if (resp.ok) {
    closePasswordModal();
    await utils.showMessageModal({ Description: 'Contraseña actualizada correctamente.', MessageType: 'success' });
  }
};

const filtro = ref({
  FullName: '',
  Email: '',
  IncludeInactive: false,
});

const formatDate = (date?: Date): string => {
  if (!date) return '—';
  return new Date(date).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const getUsers = async () => {
  const { Data: usersResp } = await getUsersByName(
    filtro.value.FullName,
    filtro.value.Email,
    filtro.value.IncludeInactive,
  );
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

const requireMfa = async (user: User) => {
  const ok = await utils.showMessageQuestion(`¿Requerir TOTP a ${user.FullName}? El usuario deberá configurarlo en su próximo inicio de sesión.`);
  if (!ok) return;
  const resp = await adminRequireMfa(user.Uuid);
  if (resp.ok) {
    user.MfaRequired = true;
    await utils.showMessageModal({ Description: 'Se ha marcado el TOTP como requerido para el usuario.', MessageType: 'success' });
  }
};

const unrequireMfa = async (user: User) => {
  const ok = await utils.showMessageQuestion(`¿Quitar la obligatoriedad de TOTP para ${user.FullName}?`);
  if (!ok) return;
  const resp = await adminUnrequireMfa(user.Uuid);
  if (resp.ok) {
    user.MfaRequired = false;
    await utils.showMessageModal({ Description: 'Se quitó la obligatoriedad de TOTP.', MessageType: 'success' });
  }
};

const resetMfa = async (user: User) => {
  const ok = await utils.showMessageQuestion(`¿Resetear el TOTP de ${user.FullName}? Esto deshabilita y elimina su configuración actual.`);
  if (!ok) return;
  const resp = await adminResetMfa(user.Uuid);
  if (resp.ok) {
    user.MfaEnabled = false;
    user.MfaRequired = false;
    await utils.showMessageModal({ Description: 'El TOTP del usuario fue reseteado correctamente.', MessageType: 'success' });
  }
};
</script>

<style scoped></style>
