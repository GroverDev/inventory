<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de Clientes</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">
          {{ localCustomer.Id ? 'Editar Cliente' : 'Nuevo Cliente' }}
        </li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="row">
        <div class="col">
          <div class="panel panel-icon">
            <div class="panel-hdr">
              <h2>
                {{ localCustomer.Id ? 'Editar' : 'Nuevo' }}
                <span class="fw-300"><i> Cliente</i></span>
              </h2>
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
                            :disabled="isSaved" @click="saveCustomer">
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
                        :disabled="isSaved" @click="saveCustomer">
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

                  <h6 class="text-muted border-bottom pb-2 mb-3">
                    <i class="fal fa-user me-1"></i> Datos del Cliente
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label" for="FullName">
                        Nombre Completo <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="FullName"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.FullName.$dirty && v$.FullName.$invalid }"
                        placeholder="Nombre completo del cliente"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.FullName.$model"
                      />
                      <small class="invalid-feedback">Requerido.</small>
                    </div>
                    <div class="col-12 col-sm-4 mb-3">
                      <label class="form-label" for="DocumentNumber">
                        Nro. Documento <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="DocumentNumber"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.DocumentNumber.$dirty && v$.DocumentNumber.$invalid }"
                        placeholder="CI / NIT"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.DocumentNumber.$model"
                      />
                      <small class="invalid-feedback">Requerido.</small>
                    </div>
                  </div>

                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-address-book me-1"></i> Contacto
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label" for="Email">Correo Electrónico</label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">
                          <i class="fal fa-envelope"></i>
                        </span>
                        <input
                          type="email"
                          id="Email"
                          class="form-control"
                          placeholder="correo@ejemplo.com"
                          :disabled="isSaved"
                          autocomplete="off"
                          v-model.trim="localCustomer.Email"
                        />
                      </div>
                    </div>
                    <div class="col-12 col-sm-4 mb-3">
                      <label class="form-label" for="Cellphone">Celular</label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">
                          <i class="fal fa-phone"></i>
                        </span>
                        <input
                          type="text"
                          id="Cellphone"
                          class="form-control"
                          placeholder="Número de celular"
                          :disabled="isSaved"
                          autocomplete="off"
                          v-model.trim="localCustomer.Cellphone"
                        />
                      </div>
                    </div>
                    <div class="col-12 col-sm-2 mb-3">
                      <label class="form-label d-block">Activo</label>
                      <div class="form-check form-switch mt-1">
                        <input
                          class="form-check-input"
                          type="checkbox"
                          id="IsActive"
                          :disabled="isSaved"
                          v-model="localCustomer.IsActive"
                        />
                        <label class="form-check-label" for="IsActive">
                          {{ localCustomer.IsActive ? 'Sí' : 'No' }}
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
import { useRouter, useRoute } from 'vue-router';
import useVuelidate from '@vuelidate/core';
import { required } from '@vuelidate/validators';
import utils from '@/utils/msg';
import { Customer } from '@/modules/inventory/models/customer.model';
import useCustomer from '@/modules/inventory/composables/useCustomer';

const router = useRouter();
const route = useRoute();
const { getCustomerById, createCustomer, updateCustomer } = useCustomer();

const localCustomer = ref(new Customer());
const isSaved = ref(false);

const rules = computed(() => ({
  FullName: { required },
  DocumentNumber: { required },
}));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const v$ = useVuelidate(rules, localCustomer as any);

onMounted(async () => {
  const id = route.params.id as string;
  if (id && id !== '0') {
    await loadCustomer(id);
  }
});

const loadCustomer = async (id: string) => {
  const { ok, Data } = await getCustomerById(id);
  if (ok) localCustomer.value = Data;
};

const returnPage = () => {
  router.push({ name: 'customers-admin' });
};

const saveCustomer = async () => {
  const isFormCorrect = await v$.value.$validate();
  if (!isFormCorrect) return;

  const respuesta = await utils.showMessageQuestion('¿Desea guardar el cliente?');
  if (!respuesta) return;

  if (!localCustomer.value.Id) {
    const { ok } = await createCustomer(localCustomer.value);
    if (ok) {
      isSaved.value = true;
      await utils.showMessageModal({ Description: 'El cliente se creó correctamente.', MessageType: 'success' });
      returnPage();
    }
  } else {
    const { ok } = await updateCustomer(localCustomer.value);
    if (ok) {
      await utils.showMessageModal({ Description: 'El cliente se actualizó correctamente.', MessageType: 'success' });
      returnPage();
    }
  }
};
</script>

<style scoped></style>
