<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de Proveedores</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">
          {{ localProvider.Id ? 'Editar Proveedor' : 'Nuevo Proveedor' }}
        </li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="row">
        <div class="col">
          <div class="panel panel-icon">
            <div class="panel-hdr">
              <h2>
                {{ localProvider.Id ? 'Editar' : 'Nuevo' }}
                <span class="fw-300"><i> Proveedor</i></span>
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
                            :disabled="isSaved" @click="saveProvider">
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
                        :disabled="isSaved" @click="saveProvider">
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
                    <i class="fal fa-truck me-1"></i> Datos del Proveedor
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label" for="ProviderName">
                        Nombre <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="ProviderName"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.ProviderName.$dirty && v$.ProviderName.$invalid }"
                        placeholder="Nombre del proveedor"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.ProviderName.$model"
                      />
                      <small class="invalid-feedback">Requerido.</small>
                    </div>
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label" for="Description">Descripción</label>
                      <input
                        type="text"
                        id="Description"
                        class="form-control form-control-sm"
                        placeholder="Descripción"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="localProvider.Description"
                      />
                    </div>
                  </div>

                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="fal fa-address-card me-1"></i> Contacto
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label" for="Direction">Dirección</label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">
                          <i class="fal fa-map-marker-alt"></i>
                        </span>
                        <input
                          type="text"
                          id="Direction"
                          class="form-control"
                          placeholder="Dirección"
                          :disabled="isSaved"
                          autocomplete="off"
                          v-model.trim="localProvider.Direction"
                        />
                      </div>
                    </div>
                    <div class="col-12 col-sm-4 mb-3">
                      <label class="form-label" for="Celular">Celular</label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">
                          <i class="fal fa-phone"></i>
                        </span>
                        <input
                          type="text"
                          id="Celular"
                          class="form-control"
                          placeholder="Número de celular"
                          :disabled="isSaved"
                          autocomplete="off"
                          v-model.trim="localProvider.Celular"
                        />
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
import { Provider } from '@/modules/inventory/models/provider.model';
import useProvider from '@/modules/inventory/composables/useProvider';

const router = useRouter();
const route = useRoute();
const { getProviderById, createProvider, updateProvider } = useProvider();

const localProvider = ref(new Provider());
const isSaved = ref(false);

const rules = computed(() => ({
  ProviderName: { required },
}));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const v$ = useVuelidate(rules, localProvider as any);

onMounted(async () => {
  const id = route.params.id as string;
  if (id && id !== '0') {
    await loadProvider(id);
  }
});

const loadProvider = async (id: string) => {
  const { ok, Data } = await getProviderById(id);
  if (ok) localProvider.value = Data;
};

const returnPage = () => {
  router.push({ name: 'providers-admin' });
};

const saveProvider = async () => {
  const isFormCorrect = await v$.value.$validate();
  if (!isFormCorrect) return;

  const respuesta = await utils.showMessageQuestion('¿Desea guardar el proveedor?');
  if (!respuesta) return;

  if (!localProvider.value.Id) {
    const { ok } = await createProvider(localProvider.value);
    if (ok) {
      isSaved.value = true;
      await utils.showMessageModal({ Description: 'El proveedor se creó correctamente.', MessageType: 'success' });
      returnPage();
    }
  } else {
    const { ok } = await updateProvider(localProvider.value);
    if (ok) {
      await utils.showMessageModal({ Description: 'El proveedor se actualizó correctamente.', MessageType: 'success' });
      returnPage();
    }
  }
};
</script>

<style scoped></style>
