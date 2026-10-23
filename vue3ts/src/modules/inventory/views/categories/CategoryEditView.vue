<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import useVuelidate from '@vuelidate/core'
import { required } from '@vuelidate/validators'

import Layout from '@/layouts/main.vue'
import PageHeader from '@/components/page-header.vue'
import useCategory from '@/modules/inventory/composables/useCategory'
import { Category } from '@/modules/inventory/models/category.model'
import utils from '@/utils/msg'

const router = useRouter()
const route = useRoute()
const { getCategoryById, createCategory, updateCategory } = useCategory()

const localCat = ref(new Category())
const isSaved = ref(false)

const rules = computed(() => ({
  CategoryName: { required },
}))

const v$ = useVuelidate(rules, localCat as any)

const loadCategory = async (id: string) => {
  const { ok, Data } = await getCategoryById(id)
  if (ok) localCat.value = Data
}

onMounted(async () => {
  const id = route.params.id as string
  if (id && id !== '0') await loadCategory(id)
  else localCat.value.IsActive = true
})

const returnPage = () => router.push({ name: 'categories-admin' })

const saveCategory = async () => {
  if (!(await v$.value.$validate())) return
  if (!(await utils.showMessageQuestion('¿Desea guardar la categoría?'))) return

  if (!localCat.value.Id) {
    const { ok } = await createCategory(localCat.value)
    if (ok) {
      isSaved.value = true
      await utils.showMessageModal({ Description: 'La categoría se creó correctamente.', MessageType: 'success' })
      returnPage()
    }
  } else {
    const { ok } = await updateCategory(localCat.value)
    if (ok) {
      await utils.showMessageModal({ Description: 'La categoría se actualizó correctamente.', MessageType: 'success' })
      returnPage()
    }
  }
}
</script>

<template>
  <Layout>
    <PageHeader :title="localCat.Id ? 'Editar categoría' : 'Nueva categoría'" page-title="Registro de Categorías" />

    <div class="row">
      <div class="col-12">
        <div class="card">
          <!-- Barra de acciones -->
          <div class="card-header">
            <div class="row align-items-center">
              <div class="col-8">
                <div class="d-md-none">
                  <div class="btn-group">
                    <button type="button" class="btn btn-primary dropdown-toggle" data-bs-toggle="dropdown"
                      data-bs-display="static" aria-expanded="false">Opciones</button>
                    <div class="dropdown-menu">
                      <button type="button" class="dropdown-item" :disabled="isSaved" @click="saveCategory">
                        <i class="ri-save-line me-1"></i>Grabar
                      </button>
                      <button type="button" class="dropdown-item" @click="returnPage">
                        <i class="ri-prohibited-line me-1"></i>Cancelar
                      </button>
                    </div>
                  </div>
                </div>
                <div class="d-none d-md-flex gap-2">
                  <button type="button" class="btn btn-sm btn-primary" :disabled="isSaved" @click="saveCategory">
                    <i class="ri-save-line me-1"></i>Grabar
                  </button>
                  <button type="button" class="btn btn-sm btn-warning" @click="returnPage">
                    <i class="ri-prohibited-line me-1"></i>Cancelar
                  </button>
                </div>
              </div>
              <div class="col-4 text-end">
                <button type="button" class="btn btn-sm btn-danger" @click="returnPage">
                  <i class="ri-arrow-left-line me-1"></i>Volver
                </button>
              </div>
            </div>
          </div>

          <!-- Formulario -->
          <div class="card-body">
            <form novalidate @submit.prevent="saveCategory">
              <h6 class="text-muted border-bottom pb-2 mb-3">
                <i class="ri-price-tag-3-line me-1"></i>Datos de la categoría
              </h6>
              <div class="row">
                <div class="col-12 col-sm-6 mb-3">
                  <label class="form-label" for="CategoryName">Nombre <span class="text-danger">*</span></label>
                  <input id="CategoryName" v-model.trim="v$.CategoryName.$model" type="text" class="form-control form-control-sm"
                    :class="{ 'is-invalid': v$.CategoryName.$dirty && v$.CategoryName.$invalid }"
                    placeholder="Nombre de la categoría" :disabled="isSaved" autocomplete="off" />
                  <small class="invalid-feedback">Requerido.</small>
                </div>
                <div class="col-12 col-sm-6 mb-3">
                  <label class="form-label" for="Description">Descripción</label>
                  <input id="Description" v-model.trim="localCat.Description" type="text" class="form-control form-control-sm"
                    placeholder="Descripción" :disabled="isSaved" autocomplete="off" />
                </div>
                <div class="col-12 col-sm-2 mb-3">
                  <label class="form-label d-block">Activo</label>
                  <div class="form-check form-switch mt-1">
                    <input id="IsActive" v-model="localCat.IsActive" class="form-check-input" type="checkbox"
                      :disabled="isSaved" />
                    <label class="form-check-label" for="IsActive">{{ localCat.IsActive ? 'Sí' : 'No' }}</label>
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
