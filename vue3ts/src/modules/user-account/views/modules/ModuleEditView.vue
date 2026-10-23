<template>
  <Layout>
    <PageHeader :title="localModule.Id ? 'Editar Módulo' : 'Nuevo Módulo'" page-title="Registro de Módulos" />


    <div class="row">
        <div class="col">
          <div id="panel-1" class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">
                {{ localModule.Id ? 'Editar' : 'Nuevo' }}
                <span class="fw-light"><i> Módulo</i></span>
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
                            :disabled="isSaved" @click="saveModule">
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
                        :disabled="isSaved" @click="saveModule">
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

                  <!-- Sección 1: Identificación -->
                  <h6 class="text-muted border-bottom pb-2 mb-3">
                    <i class="ri-profile-line me-1"></i> Identificación del Módulo
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="NameModule">
                        Nombre del Módulo <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="NameModule"
                        name="NameModule"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.NameModule.$dirty && v$.NameModule.$invalid }"
                        placeholder="Nombre del Módulo"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.NameModule.$model"
                      />
                      <small class="invalid-feedback">Requerido.</small>
                    </div>
                  </div>

                  <!-- Sección 2: Configuración técnica -->
                  <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
                    <i class="ri-code-line me-1"></i> Configuración Técnica
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label d-block" for="Route">
                        Ruta <span class="text-danger">*</span>
                      </label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">
                          <i class="ri-link"></i>
                        </span>
                        <input
                          type="text"
                          id="Route"
                          name="Route"
                          class="form-control"
                          :class="{ 'is-invalid': v$.Route.$dirty && v$.Route.$invalid }"
                          placeholder="/ruta"
                          :disabled="isSaved"
                          autocomplete="off"
                          v-model.trim="v$.Route.$model"
                        />
                        <div class="invalid-feedback">Requerido.</div>
                      </div>
                    </div>
                    <div class="col-12 col-sm-4 col-md-3 mb-3">
                      <label class="form-label d-block" for="ShowOrder">Orden en Menú</label>
                      <input
                        type="number"
                        id="ShowOrder"
                        name="ShowOrder"
                        class="form-control form-control-sm text-end"
                        placeholder="0"
                        min="0"
                        :disabled="isSaved"
                        v-model.number="localModule.ShowOrder"
                      />
                    </div>
                    <div class="col-12 col-sm-8 col-md-5 mb-3">
                      <label class="form-label d-block" for="IconCss">Ícono CSS</label>
                      <div class="input-group input-group-sm">
                        <span class="input-group-text bg-transparent">
                          <i :class="localModule.IconCss || 'ri-layout-grid-line'" class="text-primary"></i>
                        </span>
                        <input
                          type="text"
                          id="IconCss"
                          name="IconCss"
                          class="form-control"
                          placeholder="ri-settings-3-line"
                          :disabled="isSaved"
                          v-model.trim="localModule.IconCss"
                        />
                      </div>
                    </div>
                  </div>

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
import { useRouter, useRoute } from "vue-router";
import useVuelidate from '@vuelidate/core';
import { required } from '@vuelidate/validators';
import utils from '@/utils/msg';

import { Module } from '@/modules/user-account/models/module.model';
import useModule from '@/modules/user-account/composables/useModule';

const router = useRouter();
const route = useRoute();
const { getModuleById, createModule, updateModule } = useModule();

const localModule = ref(new Module());
const isSaved = ref(false);

const rules = computed(() => ({
  NameModule: { required },
  Route: { required },
}));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const v$ = useVuelidate(rules, localModule as any);

onMounted(async () => {
  const moduleId = route.params.id as string;
  if (moduleId && moduleId !== '0') {
    await loadModule(parseInt(moduleId));
  }
});

const loadModule = async (id: number) => {
  const { ok, Data: moduleResp } = await getModuleById(id);
  if (ok) localModule.value = moduleResp;
};

const returnPage = () => {
  router.push({ name: 'modules-admin' });
};

const saveModule = async () => {
  const isFormCorrect = await v$.value.$validate();
  if (!isFormCorrect) return;

  const respuesta = await utils.showMessageQuestion('¿Desea guardar el módulo?');

  if (respuesta) {
    if (localModule.value.Id === 0) {
      const { ok, Data: idModule } = await createModule(localModule.value);
      if (ok) {
        isSaved.value = true;
        localModule.value.Id = idModule;
        await utils.showMessageModal({ Description: 'El módulo se creó correctamente.', MessageType: 'success' });
        returnPage();
      }
    } else {
      const { ok, Data: okResp } = await updateModule(localModule.value);
      if (ok && okResp) {
        await utils.showMessageModal({ Description: 'El módulo se actualizó correctamente.', MessageType: 'success' });
        returnPage();
      }
    }
  }
};
</script>

<style scoped></style>
