<template>
  <Layout>
    <PageHeader :title="localBranch.Id ? 'Editar Sucursal' : 'Nueva Sucursal'" page-title="Sucursales" />


    <div class="row">
        <div class="col">
          <div class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">
                {{ localBranch.Id ? 'Editar' : 'Nueva' }}
                <span class="fw-light"><i> Sucursal</i></span>
              </h5>
            </div>
            <!-- Barra de acciones -->
              <div class="pt-0 card-body">
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
                            :disabled="isSaved" @click="saveBranch">
                            <span class="ri-save-line me-1"></span>Grabar
                          </button>
                          <button type="button" class="dropdown-item border-bottom border-1"
                            @click="returnPage">
                            <span class="ri-prohibited-line me-1"></span>Cancelar
                          </button>
                        </div>
                      </div>
                    </div>
                    <div class="d-none d-md-flex gap-2">
                      <button type="button" class="btn btn-sm btn-primary"
                        :disabled="isSaved" @click="saveBranch">
                        <span class="ri-save-line me-1"></span>Grabar
                      </button>
                      <button type="button" class="btn btn-warning btn-sm" @click="returnPage">
                        <span class="ri-prohibited-line me-1"></span>Cancelar
                      </button>
                    </div>
                  </div>
                  <div class="col-4 col-md-4 text-md-end">
                    <button type="button" class="btn btn-danger btn-sm" @click="returnPage">
                      <span class="ri-arrow-left-line me-1"></span>Volver
                    </button>
                  </div>
                </div>
              </div>

              <!-- Formulario -->
              <div class="pt-0 card-body">
                <form novalidate>

                  <h6 class="text-muted border-bottom pb-2 mb-3">
                    <i class="ri-store-2-line me-1"></i> Datos de la Sucursal
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label" for="Name">
                        Nombre <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="Name"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.Name.$dirty && v$.Name.$invalid }"
                        placeholder="Ej.: Sucursal Centro"
                        maxlength="150"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.Name.$model"
                      />
                      <small class="invalid-feedback">Requerido, mínimo 2 caracteres.</small>
                    </div>
                    <div class="col-12 col-sm-3 mb-3">
                      <label class="form-label" for="Code">Código</label>
                      <input
                        type="text"
                        id="Code"
                        class="form-control form-control-sm"
                        placeholder="Ej.: CENTRO"
                        maxlength="20"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="localBranch.Code"
                      />
                    </div>
                    <div class="col-12 col-sm-3 mb-3">
                      <label class="form-label d-block">Activa</label>
                      <div class="form-check form-switch mt-1">
                        <input
                          class="form-check-input"
                          type="checkbox"
                          id="IsActive"
                          :disabled="isSaved"
                          v-model="localBranch.IsActive"
                        />
                        <label class="form-check-label" for="IsActive">
                          {{ localBranch.IsActive ? 'Sí' : 'No' }}
                        </label>
                      </div>
                    </div>
                    <div class="col-12 col-sm-8 mb-3">
                      <label class="form-label" for="Address">Dirección</label>
                      <input
                        type="text"
                        id="Address"
                        class="form-control form-control-sm"
                        placeholder="Dirección"
                        maxlength="300"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="localBranch.Address"
                      />
                    </div>
                    <div class="col-12 col-sm-4 mb-3">
                      <label class="form-label" for="Phone">Teléfono</label>
                      <input
                        type="text"
                        id="Phone"
                        class="form-control form-control-sm"
                        placeholder="Teléfono"
                        maxlength="50"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="localBranch.Phone"
                      />
                    </div>
                  </div>

                  <p v-if="!localBranch.Id" class="text-muted small mb-0">
                    <i class="ri-information-line me-1"></i>
                    Quedará habilitado en la sucursal nueva para poder entrar a ella. Al resto de los usuarios
                    se los habilita desde su ficha en <strong>Usuarios</strong>.
                  </p>

                </form>
              </div>

            
          </div>
        </div>
      </div>
    
  </Layout>
</template>

<script setup lang="ts">
import Layout from '@/layouts/main.vue'
import PageHeader from '@/components/page-header.vue'
import { onMounted, ref, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import useVuelidate from '@vuelidate/core';
import { required, minLength } from '@vuelidate/validators';
import utils from '@/utils/msg';
import { Branch } from '@/modules/user-account/models/branch.model';
import useBranch from '@/modules/user-account/composables/useBranch';
import { useAuthStore } from '@/stores/auth';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const { getBranchById, createBranch, updateBranch } = useBranch();

const localBranch = ref(new Branch());
const isSaved = ref(false);

const rules = computed(() => ({
  Name: { required, minLength: minLength(2) },
}));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const v$ = useVuelidate(rules, localBranch as any);

onMounted(async () => {
  const id = route.params.id as string;
  if (id && id !== '0') {
    const { ok, Data } = await getBranchById(id);
    if (ok) localBranch.value = Data;
  }
});

const returnPage = () => {
  router.push({ name: 'branches-admin' });
};

const saveBranch = async () => {
  const isFormCorrect = await v$.value.$validate();
  if (!isFormCorrect) return;

  const respuesta = await utils.showMessageQuestion('¿Desea guardar la sucursal?');
  if (!respuesta) return;

  if (!localBranch.value.Id) {
    const { ok } = await createBranch(localBranch.value);
    if (ok) {
      isSaved.value = true;
      // El creador quedó habilitado en la sucursal nueva: se renueva la sesión
      // para que aparezca en el selector de sucursal sin volver a entrar.
      await authStore.refreshBranches();
      await utils.showMessageModal({ Description: 'La sucursal se creó correctamente.', MessageType: 'success' });
      returnPage();
    }
  } else {
    const { ok } = await updateBranch(localBranch.value);
    if (ok) {
      await authStore.refreshBranches();
      await utils.showMessageModal({ Description: 'La sucursal se actualizó correctamente.', MessageType: 'success' });
      returnPage();
    }
  }
};
</script>

<style scoped></style>
