<template>
  <Layout>
    <PageHeader title="Registro de Clientes" page-title="Inventario" />

    <div class="card">
        <div class="card-header">
          <h5 class="card-title mb-0">Gestión de <span class="fw-light"><i>CLIENTES</i></span></h5>
        </div>
        <div class="pt-0 card-body">
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newCustomer">
                <span class="ri-add-box-line me-1"></span>Nuevo Cliente
              </button>
            </div>
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre del cliente</label>
                <div class="input-group rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="ri-search-line text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="filtro"
                    placeholder="Ingrese el nombre del cliente..."
                    autocomplete="off"
                    @keyup.enter="getCustomersData"
                  />
                  <button class="btn btn-primary" type="button" @click="getCustomersData">Buscar</button>
                </div>
              </div>
            </div>

            <div v-if="customers.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="ri-list-check me-1"></span>
                <strong>{{ customers.length }}</strong> cliente(s) encontrado(s)
              </small>
            </div>

            <div v-if="customers.length === 0" class="text-center py-5">
              <i class="ri-group-line ri-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar clientes en el sistema</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newCustomer">
                <span class="ri-add-line me-1"></span>Crear nuevo cliente
              </button>
            </div>

            <template v-else>
              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="">
                    <tr>
                      <th>Nombre Completo</th>
                      <th>Nro. Documento</th>
                      <th class="d-none d-lg-table-cell">Correo</th>
                      <th class="d-none d-lg-table-cell">Celular</th>
                      <th class="text-center">Activo</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(customer, index) in customers" :key="index">
                      <td class="fw-semibold">{{ customer.FullName }}</td>
                      <td>{{ customer.DocumentNumber }}</td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ customer.Email }}</small>
                      </td>
                      <td class="d-none d-lg-table-cell">{{ customer.Cellphone }}</td>
                      <td class="text-center">
                        <span :class="customer.IsActive ? 'badge bg-success' : 'badge bg-secondary'">
                          {{ customer.IsActive ? 'Sí' : 'No' }}
                        </span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Editar"
                          @click="editCustomer(customer)"
                        >
                          <span class="ri-edit-2-line"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeCustomer(customer.Id)"
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
                  <div class="col-12 col-sm-6" v-for="(customer, index) in customers" :key="index">
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <p class="fw-semibold mb-0 lh-sm">{{ customer.FullName }}</p>
                          <span class="badge rounded-pill" :class="customer.IsActive ? 'text-bg-success' : 'text-bg-secondary'">
                            {{ customer.IsActive ? 'Activo' : 'Inactivo' }}
                          </span>
                        </div>
                        <small class="text-muted"><i class="ri-profile-line me-1"></i>{{ customer.DocumentNumber }}</small>
                        <small class="text-muted">{{ customer.Email }}</small>
                        <small class="text-muted"><i class="ri-phone-line me-1"></i>{{ customer.Cellphone }}</small>
                        <div class="d-flex gap-2 mt-auto pt-1">
                          <button type="button" class="btn btn-sm btn-outline-primary flex-grow-1" @click="editCustomer(customer)">
                            <span class="ri-edit-2-line me-1"></span>Editar
                          </button>
                          <button type="button" class="btn btn-sm btn-outline-danger" @click="removeCustomer(customer.Id)">
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
import useCustomer from '@/modules/inventory/composables/useCustomer';
import type { Customer } from '@/modules/inventory/models/customer.model';
import utils from '@/utils/msg';

const customers = ref<Customer[]>([]);
const filtro = ref('');
const { getCustomers, deleteCustomer } = useCustomer();
const router = useRouter();

const getCustomersData = async () => {
  const { Data } = await getCustomers(filtro.value);
  customers.value = Data;
};

const newCustomer = () => {
  router.push({ name: 'customer-edit', params: { id: '0' } });
};

const editCustomer = (customer: Customer) => {
  router.push({ name: 'customer-edit', params: { id: customer.Id } });
};

const removeCustomer = async (id: string) => {
  const respuesta = await utils.showMessageQuestion('¿Desea eliminar el cliente?');
  if (respuesta) {
    const { ok } = await deleteCustomer(id);
    if (ok) {
      await utils.showMessageModal({ Description: 'El cliente se eliminó correctamente.', MessageType: 'success' });
      await getCustomersData();
    }
  }
};
</script>

<style scoped></style>
