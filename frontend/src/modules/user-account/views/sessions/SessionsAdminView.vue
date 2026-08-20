<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Cuenta</li>
        <li class="breadcrumb-item">Seguridad</li>
        <li class="breadcrumb-item active" aria-current="page">Sesiones Activas</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Usuarios <span class="fw-300"><i>Conectados</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">

            <div class="d-flex justify-content-between align-items-center mb-3">
              <small v-if="sessions.length > 0" class="text-muted">
                <span class="fal fa-broadcast-tower me-1"></span>
                <strong>{{ sessions.length }}</strong> sesión(es) activa(s)
              </small>
              <button type="button" class="btn btn-sm btn-outline-secondary ms-auto" @click="loadSessions">
                <span class="fal fa-sync-alt me-1"></span>Actualizar
              </button>
            </div>

            <!-- Estado vacío -->
            <div v-if="sessions.length === 0" class="text-center py-5">
              <i class="fal fa-user-slash fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-0">No hay usuarios conectados en este momento.</p>
            </div>

            <template v-else>
              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead>
                    <tr>
                      <th>Usuario</th>
                      <th>Correo Electrónico</th>
                      <th>Dispositivo</th>
                      <th class="text-center">Origen</th>
                      <th class="text-center">Conectado desde</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="session in sessions" :key="session.Id">
                      <td class="fw-semibold">{{ session.FullName }}</td>
                      <td><small class="text-muted">{{ session.Email }}</small></td>
                      <td><small class="text-muted">{{ session.Device || '—' }}</small></td>
                      <td class="text-center"><span class="badge bg-secondary">{{ session.LoginFrom }}</span></td>
                      <td class="text-center"><small class="text-muted">{{ formatDate(session.CreatedAt) }}</small></td>
                      <td class="text-center">
                        <button
                          v-if="canDelete"
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Cerrar esta sesión"
                          @click="close(session)"
                        >
                          <span class="fal fa-sign-out-alt me-1"></span>Cerrar sesión
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- Cards (móvil <md) -->
              <div class="d-md-none">
                <div class="row g-3">
                  <div class="col-12 col-sm-6" v-for="session in sessions" :key="session.Id">
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <p class="fw-semibold mb-0 lh-sm">{{ session.FullName }}</p>
                        <small class="text-muted">{{ session.Email }}</small>
                        <small class="text-muted">
                          <i class="fal fa-desktop me-1"></i>{{ session.Device || 'Dispositivo desconocido' }}
                          <span class="badge bg-secondary ms-1">{{ session.LoginFrom }}</span>
                        </small>
                        <small class="text-muted">
                          <i class="fal fa-clock me-1"></i>{{ formatDate(session.CreatedAt) }}
                        </small>
                        <button
                          v-if="canDelete"
                          type="button"
                          class="btn btn-sm btn-outline-danger mt-auto"
                          @click="close(session)"
                        >
                          <span class="fal fa-sign-out-alt me-1"></span>Cerrar sesión
                        </button>
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
import { onMounted, ref, computed } from 'vue';
import useSessions from '@/modules/user-account/composables/useSessions';
import type { ConnectedUser } from '@/modules/user-account/models/session.model';
import usePermissions from '@/modules/common/composables/usePermissions';
import utils from '@/utils/msg';

const { getConnectedUsers, closeSession } = useSessions();
const { can } = usePermissions();
const canDelete = computed(() => can('active-sessions', 'delete'));

const sessions = ref<ConnectedUser[]>([]);

const formatDate = (date: string): string => {
  if (!date) return '—';
  return new Date(date).toLocaleString('es-BO', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
};

const loadSessions = async () => {
  const { ok, Data } = await getConnectedUsers();
  if (ok) sessions.value = Data;
};

const close = async (session: ConnectedUser) => {
  const confirmed = await utils.showMessageQuestion(
    `¿Cerrar la sesión de ${session.FullName} en "${session.Device || 'este dispositivo'}"? Se le pedirá iniciar sesión de nuevo.`
  );
  if (!confirmed) return;

  const { ok } = await closeSession(session.Id);
  if (ok) {
    await utils.showMessageModal({ Description: 'La sesión se cerró correctamente.', MessageType: 'success' });
    await loadSessions();
  }
};

onMounted(loadSessions);
</script>

<style scoped></style>
