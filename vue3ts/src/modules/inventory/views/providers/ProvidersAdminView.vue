<template>
  <Layout>
    <PageHeader title="Registro de Proveedores" page-title="Inventario" />

    <div class="card">
        <div class="card-header">
          <h5 class="card-title mb-0">Gestión de <span class="fw-light"><i>PROVEEDORES</i></span></h5>
        </div>
        <div class="pt-0 card-body">
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newProvider">
                <span class="ri-add-box-line me-1"></span>Nuevo Proveedor
              </button>
            </div>
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre del proveedor</label>
                <div class="input-group rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="ri-search-line text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="filtro"
                    placeholder="Ingrese el nombre del proveedor..."
                    autocomplete="off"
                    @keyup.enter="getProvidersData"
                  />
                  <button class="btn btn-primary" type="button" @click="getProvidersData">Buscar</button>
                </div>
              </div>
            </div>

            <div v-if="providers.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="ri-list-check me-1"></span>
                <strong>{{ providers.length }}</strong> proveedor(es) encontrado(s)
              </small>
            </div>

            <div v-if="providers.length === 0" class="text-center py-5">
              <i class="ri-truck-line ri-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar proveedores en el sistema</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newProvider">
                <span class="ri-add-line me-1"></span>Crear nuevo proveedor
              </button>
            </div>

            <template v-else>
              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="">
                    <tr>
                      <th>Nombre</th>
                      <th class="d-none d-lg-table-cell">Descripción</th>
                      <th class="d-none d-lg-table-cell">Dirección</th>
                      <th>Celular</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(provider, index) in providers" :key="index">
                      <td class="fw-semibold">{{ provider.ProviderName }}</td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ provider.Description }}</small>
                      </td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ provider.Direction }}</small>
                      </td>
                      <td>{{ provider.Celular }}</td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Editar"
                          @click="editProvider(provider)"
                        >
                          <span class="ri-edit-2-line"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeProvider(provider.Id)"
                        >
                          <span class="ri-delete-bin-line"></span>
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- Cards (móvil <md) -->
              <div class="d-md-none">
                <div class="row g-3">
                  <div class="col-12 col-sm-6" v-for="(provider, index) in providers" :key="index">
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <p class="fw-semibold mb-0 lh-sm">{{ provider.ProviderName }}</p>
                        <small class="text-muted">{{ provider.Description }}</small>
                        <small class="text-muted"><i class="ri-map-pin-line me-1"></i>{{ provider.Direction }}</small>
                        <small class="text-muted"><i class="ri-phone-line me-1"></i>{{ provider.Celular }}</small>
                        <div class="d-flex gap-2 mt-auto pt-1">
                          <button type="button" class="btn btn-sm btn-outline-primary flex-grow-1" @click="editProvider(provider)">
                            <span class="ri-edit-2-line me-1"></span>Editar
                          </button>
                          <button type="button" class="btn btn-sm btn-outline-danger" @click="removeProvider(provider.Id)">
                            <span class="ri-delete-bin-line"></span>
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
    
  </Layout>
</template>

<script setup lang="ts">
import Layout from '@/layouts/main.vue'
import PageHeader from '@/components/page-header.vue'
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import useProvider from '@/modules/inventory/composables/useProvider';
import type { Provider } from '@/modules/inventory/models/provider.model';
import utils from '@/utils/msg';

const providers = ref<Provider[]>([]);
const filtro = ref('');
const { getProviders, deleteProvider } = useProvider();
const router = useRouter();

const getProvidersData = async () => {
  const { Data } = await getProviders(filtro.value);
  providers.value = Data;
};

const newProvider = () => {
  router.push({ name: 'provider-edit', params: { id: '0' } });
};

const editProvider = (provider: Provider) => {
  router.push({ name: 'provider-edit', params: { id: provider.Id } });
};

const removeProvider = async (id: string) => {
  const respuesta = await utils.showMessageQuestion('¿Desea eliminar el proveedor?');
  if (respuesta) {
    const { ok } = await deleteProvider(id);
    if (ok) {
      await utils.showMessageModal({ Description: 'El proveedor se eliminó correctamente.', MessageType: 'success' });
      await getProvidersData();
    }
  }
};
</script>

<style scoped></style>
