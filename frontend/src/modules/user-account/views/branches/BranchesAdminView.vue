<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Administración</li>
        <li class="breadcrumb-item active" aria-current="page">Sucursales</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>SUCURSALES</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">
            <div v-if="canCreate" class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newBranch">
                <span class="fal fa-plus-square me-1"></span>Nueva Sucursal
              </button>
            </div>
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre de la sucursal</label>
                <div class="input-group input-group body-bg shadow-inset-2 rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="sa sa-magnifier text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="filtro"
                    placeholder="Filtrar por nombre..."
                    autocomplete="off"
                    @keyup.enter="getBranchesData"
                  />
                  <button class="btn btn-primary" type="button" @click="getBranchesData">Buscar</button>
                </div>
              </div>
              <div class="col-12 col-md-auto">
                <div class="form-check form-switch mb-2">
                  <input class="form-check-input" type="checkbox" id="includeInactive"
                    v-model="includeInactive" @change="getBranchesData" />
                  <label class="form-check-label" for="includeInactive">Mostrar inactivas</label>
                </div>
              </div>
            </div>

            <div v-if="branches.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="fal fa-list me-1"></span>
                <strong>{{ branches.length }}</strong> sucursal(es)
              </small>
            </div>

            <div v-if="branches.length === 0" class="text-center py-5">
              <i class="fal fa-store-alt fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-0">No hay sucursales que coincidan con la búsqueda.</p>
            </div>

            <template v-else>
              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead>
                    <tr>
                      <th>Nombre</th>
                      <th>Código</th>
                      <th class="d-none d-lg-table-cell">Dirección</th>
                      <th>Teléfono</th>
                      <th class="text-center">Usuarios</th>
                      <th class="text-center">Activa</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="branch in branches" :key="branch.Id">
                      <td class="fw-semibold">
                        {{ branch.Name }}
                        <span v-if="branch.Id === currentBranchId" class="badge bg-info ms-1">Actual</span>
                      </td>
                      <td><small class="text-muted">{{ branch.Code }}</small></td>
                      <td class="d-none d-lg-table-cell"><small class="text-muted">{{ branch.Address }}</small></td>
                      <td>{{ branch.Phone }}</td>
                      <td class="text-center">{{ branch.UsersCount }}</td>
                      <td class="text-center">
                        <span :class="branch.IsActive ? 'badge bg-success' : 'badge bg-secondary'">
                          {{ branch.IsActive ? 'Sí' : 'No' }}
                        </span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button v-if="canUpdate" type="button" class="btn btn-outline-primary btn-sm me-1"
                          title="Editar" @click="editBranch(branch)">
                          <span class="fal fa-edit"></span>
                        </button>
                        <button v-if="canDelete" type="button" class="btn btn-outline-danger btn-sm"
                          title="Eliminar" @click="removeBranch(branch)">
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
                  <div class="col-12 col-sm-6" v-for="branch in branches" :key="branch.Id">
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <p class="fw-semibold mb-0 lh-sm">
                            {{ branch.Name }}
                            <span v-if="branch.Id === currentBranchId" class="badge bg-info ms-1">Actual</span>
                          </p>
                          <span class="badge rounded-pill" :class="branch.IsActive ? 'text-bg-success' : 'text-bg-secondary'">
                            {{ branch.IsActive ? 'Activa' : 'Inactiva' }}
                          </span>
                        </div>
                        <small class="text-muted">{{ branch.Address }}</small>
                        <small class="text-muted"><i class="fal fa-users me-1"></i>{{ branch.UsersCount }} usuario(s)</small>
                        <div class="d-flex gap-2 mt-auto pt-1">
                          <button v-if="canUpdate" type="button" class="btn btn-sm btn-outline-primary flex-grow-1"
                            @click="editBranch(branch)">
                            <span class="fal fa-edit me-1"></span>Editar
                          </button>
                          <button v-if="canDelete" type="button" class="btn btn-sm btn-outline-danger"
                            @click="removeBranch(branch)">
                            <span class="fal fa-trash-alt"></span>
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
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import useBranch from '@/modules/user-account/composables/useBranch';
import type { Branch } from '@/modules/user-account/models/branch.model';
import { useAuthStore } from '@/modules/auth/stores/auth.store';
import { usePermissions } from '@/modules/common/composables/usePermissions';
import utils from '@/utils/msg';

const FORM = 'branches-admin';

const branches = ref<Branch[]>([]);
const filtro = ref('');
const includeInactive = ref(false);
const { getBranches, deleteBranch } = useBranch();
const router = useRouter();
const authStore = useAuthStore();
const { can } = usePermissions();

const canCreate = computed(() => can(FORM, 'create'));
const canUpdate = computed(() => can(FORM, 'update'));
const canDelete = computed(() => can(FORM, 'delete'));
const currentBranchId = computed(() => authStore.getUser?.BranchId);

// Son pocas: se listan al entrar, sin esperar una búsqueda.
onMounted(() => getBranchesData());

const getBranchesData = async () => {
  const { ok, Data } = await getBranches(filtro.value, includeInactive.value);
  if (ok) branches.value = Data;
};

const newBranch = () => {
  router.push({ name: 'branch-edit', params: { id: '0' } });
};

const editBranch = (branch: Branch) => {
  router.push({ name: 'branch-edit', params: { id: branch.Id } });
};

const removeBranch = async (branch: Branch) => {
  const respuesta = await utils.showMessageQuestion(
    `¿Desea eliminar la sucursal «${branch.Name}»? Sus ventas y movimientos se conservan para consulta.`);
  if (!respuesta) return;

  const { ok } = await deleteBranch(branch.Id);
  if (ok) {
    await utils.showMessageModal({ Description: 'La sucursal se eliminó correctamente.', MessageType: 'success' });
    await getBranchesData();
  }
};
</script>

<style scoped></style>
