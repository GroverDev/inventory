<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de Categorías</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">
          {{ localCat.Id ? 'Editar Categoría' : 'Nueva Categoría' }}
        </li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="row">
        <div class="col">
          <div class="panel panel-icon">
            <div class="panel-hdr">
              <h2>
                {{ localCat.Id ? 'Editar' : 'Nueva' }}
                <span class="fw-300"><i> Categoría</i></span>
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
                            :disabled="isSaved" @click="saveCategory">
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
                        :disabled="isSaved" @click="saveCategory">
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
                    <i class="fal fa-tags me-1"></i> Datos de la Categoría
                  </h6>
                  <div class="row">
                    <div class="col-12 col-sm-6 mb-3">
                      <label class="form-label" for="CategoryName">
                        Nombre <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="CategoryName"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.CategoryName.$dirty && v$.CategoryName.$invalid }"
                        placeholder="Nombre de la categoría"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.CategoryName.$model"
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
                        v-model.trim="localCat.Description"
                      />
                    </div>
                    <div class="col-12 col-sm-2 mb-3">
                      <label class="form-label d-block">Activo</label>
                      <div class="form-check form-switch mt-1">
                        <input
                          class="form-check-input"
                          type="checkbox"
                          id="IsActive"
                          :disabled="isSaved"
                          v-model="localCat.IsActive"
                        />
                        <label class="form-check-label" for="IsActive">
                          {{ localCat.IsActive ? 'Sí' : 'No' }}
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
import { Category } from '@/modules/inventory/models/category.model';
import useCategory from '@/modules/inventory/composables/useCategory';

const router = useRouter();
const route = useRoute();
const { getCategoryById, createCategory, updateCategory } = useCategory();

const localCat = ref(new Category());
const isSaved = ref(false);

const rules = computed(() => ({
  CategoryName: { required },
}));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const v$ = useVuelidate(rules, localCat as any);

onMounted(async () => {
  const id = route.params.id as string;
  if (id && id !== '0') {
    await loadCategory(id);
  } else {
    localCat.value.IsActive = true;
  }
});

const loadCategory = async (id: string) => {
  const { ok, Data } = await getCategoryById(id);
  if (ok) localCat.value = Data;
};

const returnPage = () => {
  router.push({ name: 'categories-admin' });
};

const saveCategory = async () => {
  const isFormCorrect = await v$.value.$validate();
  if (!isFormCorrect) return;

  const respuesta = await utils.showMessageQuestion('¿Desea guardar la categoría?');
  if (!respuesta) return;

  if (!localCat.value.Id) {
    const { ok } = await createCategory(localCat.value);
    if (ok) {
      isSaved.value = true;
      await utils.showMessageModal({ Description: 'La categoría se creó correctamente.', MessageType: 'success' });
      returnPage();
    }
  } else {
    const { ok } = await updateCategory(localCat.value);
    if (ok) {
      await utils.showMessageModal({ Description: 'La categoría se actualizó correctamente.', MessageType: 'success' });
      returnPage();
    }
  }
};
</script>

<style scoped></style>
