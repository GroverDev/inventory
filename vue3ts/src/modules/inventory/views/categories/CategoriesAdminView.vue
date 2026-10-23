<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

import Layout from '@/layouts/main.vue'
import PageHeader from '@/components/page-header.vue'
import useCategory from '@/modules/inventory/composables/useCategory'
import type { Category } from '@/modules/inventory/models/category.model'
import utils from '@/utils/msg'

const categories = ref<Category[]>([])
const filtro = ref('')
const { getCategories, deleteCategory } = useCategory()
const router = useRouter()

const getCategoriesData = async () => {
  const { Data } = await getCategories(filtro.value)
  categories.value = Data ?? []
}

const newCategory = () => router.push({ name: 'category-edit', params: { id: '0' } })

const editCategory = (cat: Category) => router.push({ name: 'category-edit', params: { id: cat.Id } })

const removeCategory = async (id: string) => {
  if (!(await utils.showMessageQuestion('¿Desea eliminar la categoría?'))) return
  const { ok } = await deleteCategory(id)
  if (ok) {
    await utils.showMessageModal({ Description: 'La categoría se eliminó correctamente.', MessageType: 'success' })
    await getCategoriesData()
  }
}
</script>

<template>
  <Layout>
    <PageHeader title="Registro de Categorías" page-title="Inventario" />

    <div class="row">
      <div class="col-12">
        <div class="card">
          <div class="card-header d-flex align-items-center">
            <h5 class="card-title mb-0 flex-grow-1">Gestión de categorías</h5>
            <button type="button" class="btn btn-primary btn-sm" @click="newCategory">
              <i class="ri-add-line align-bottom me-1"></i>Nueva categoría
            </button>
          </div>

          <div class="card-body">
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre de la categoría</label>
                <div class="input-group">
                  <span class="input-group-text"><i class="ri-search-line"></i></span>
                  <input v-model.trim="filtro" type="text" class="form-control" autocomplete="off"
                    placeholder="Ingrese el nombre de la categoría..." @keyup.enter="getCategoriesData" />
                  <button class="btn btn-primary" type="button" @click="getCategoriesData">Buscar</button>
                </div>
              </div>
            </div>

            <div v-if="categories.length > 0" class="mb-2">
              <small class="text-muted">
                <i class="ri-list-check me-1"></i>
                <strong>{{ categories.length }}</strong> categoría(s) encontrada(s)
              </small>
            </div>

            <div v-if="categories.length === 0" class="text-center py-5">
              <i class="ri-price-tag-3-line display-4 text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar categorías en el sistema</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newCategory">
                <i class="ri-add-line me-1"></i>Crear nueva categoría
              </button>
            </div>

            <template v-else>
              <!-- Tabla (md en adelante) -->
              <div class="d-none d-md-block table-responsive">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th>Nombre</th>
                      <th class="d-none d-lg-table-cell">Descripción</th>
                      <th class="text-center">Activo</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="cat in categories" :key="cat.Id">
                      <td class="fw-semibold">{{ cat.CategoryName }}</td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ cat.Description }}</small>
                      </td>
                      <td class="text-center">
                        <span class="badge" :class="cat.IsActive ? 'bg-success' : 'bg-secondary'">
                          {{ cat.IsActive ? 'Sí' : 'No' }}
                        </span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button type="button" class="btn btn-outline-primary btn-sm me-1" title="Editar"
                          @click="editCategory(cat)">
                          <i class="ri-edit-2-line"></i>
                        </button>
                        <button type="button" class="btn btn-outline-danger btn-sm" title="Eliminar"
                          @click="removeCategory(cat.Id)">
                          <i class="ri-delete-bin-line"></i>
                        </button>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <!-- Tarjetas (móvil) -->
              <div class="d-md-none">
                <div class="row g-3">
                  <div v-for="cat in categories" :key="cat.Id" class="col-12 col-sm-6">
                    <div class="card h-100 shadow-sm mb-0">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <p class="fw-semibold mb-0 lh-sm">{{ cat.CategoryName }}</p>
                          <span class="badge rounded-pill" :class="cat.IsActive ? 'bg-success' : 'bg-secondary'">
                            {{ cat.IsActive ? 'Activo' : 'Inactivo' }}
                          </span>
                        </div>
                        <small class="text-muted">{{ cat.Description }}</small>
                        <div class="d-flex gap-2 mt-auto pt-1">
                          <button type="button" class="btn btn-sm btn-outline-primary flex-grow-1" @click="editCategory(cat)">
                            <i class="ri-edit-2-line me-1"></i>Editar
                          </button>
                          <button type="button" class="btn btn-sm btn-outline-danger" @click="removeCategory(cat.Id)">
                            <i class="ri-delete-bin-line"></i>
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
  </Layout>
</template>
