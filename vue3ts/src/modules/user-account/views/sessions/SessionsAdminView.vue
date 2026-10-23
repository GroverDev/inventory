<template>
  <Layout>
    <PageHeader title="Sesiones Activas" page-title="Seguridad" />

    <div class="card">
        <div class="card-header">
          <h5 class="card-title mb-0">Usuarios <span class="fw-light"><i>Conectados</i></span></h5>
        </div>
        <div class="pt-0 card-body">

            <div class="d-flex justify-content-between align-items-center mb-3">
              <small v-if="sessions.length > 0" class="text-muted">
                <span class="ri-base-station-line me-1"></span>
                <strong>{{ sessions.length }}</strong> sesión(es) activa(s)
              </small>
              <button type="button" class="btn btn-sm btn-outline-secondary ms-auto" @click="loadSessions">
                <span class="ri-refresh-line me-1"></span>Actualizar
              </button>
            </div>

            <!-- Estado vacío -->
            <div v-if="sessions.length === 0" class="text-center py-5">
              <i class="ri-user-unfollow-line ri-3x text-muted d-block mb-3"></i>
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
                      <th class="text-center">Duración</th>
                      <th class="text-center">Vence</th>
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
                        <span v-if="isLongSession(session)" class="badge bg-info" title="El usuario pidió mantener la sesión 30 días">30 días</span>
                        <span v-else class="badge bg-secondary" title="Termina al cerrar el navegador">Sesión</span>
                      </td>
                      <td class="text-center"><small class="text-muted">{{ formatDate(session.ExpiresAt) }}</small></td>
                      <td class="text-center">
                        <button
                          v-if="canDelete"
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Cerrar esta sesión"
                          @click="close(session)"
                        >
                          <span class="ri-logout-box-r-line me-1"></span>Cerrar sesión
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
                          <i class="ri-computer-line me-1"></i>{{ session.Device || 'Dispositivo desconocido' }}
                          <span class="badge bg-secondary ms-1">{{ session.LoginFrom }}</span>
                        </small>
                        <small class="text-muted">
                          <i class="ri-time-line me-1"></i>{{ formatDate(session.CreatedAt) }}
                          <span v-if="isLongSession(session)" class="badge bg-info ms-1">30 días</span>
                          <span v-else class="badge bg-secondary ms-1">Sesión</span>
                        </small>
                        <small class="text-muted">
                          <i class="ri-hourglass-2-line me-1"></i>Vence {{ formatDate(session.ExpiresAt) }}
                        </small>
                        <button
                          v-if="canDelete"
                          type="button"
                          class="btn btn-sm btn-outline-danger mt-auto"
                          @click="close(session)"
                        >
                          <span class="ri-logout-box-r-line me-1"></span>Cerrar sesión
                        </button>
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
import { onMounted, ref, computed } from 'vue';
import useSessions from '@/modules/user-account/composables/useSessions';
import { isLongSession, type ConnectedUser } from '@/modules/user-account/models/session.model';
import usePermissions from '@/composables/usePermissions';
import utils from '@/utils/msg';

const { getConnectedUsers, closeSession } = useSessions();
const { can } = usePermissions();
const canDelete = computed(() => can('active-sessions', 'delete'));

const sessions = ref<ConnectedUser[]>([]);

const formatDate = (date: string): string => {
  if (!date) return '—';
  return new Date(date).toLocaleString('es-BO', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false,
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
