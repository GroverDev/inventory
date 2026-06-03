<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item active" aria-current="page">Registro de Categorías</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>CATEGORÍAS</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newCategory">
                <span class="fal fa-plus-square me-1"></span>Nueva Categoría
              </button>
            </div>
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre de la categoría</label>
                <div class="input-group input-group body-bg shadow-inset-2 rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="sa sa-magnifier text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="filtro"
                    placeholder="Ingrese el nombre de la categoría..."
                    autocomplete="off"
                    @keyup.enter="getCategoriesData"
                  />
                  <button class="btn btn-primary" type="button" @click="getCategoriesData">Buscar</button>
                </div>
              </div>
            </div>

            <div v-if="categories.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="fal fa-list me-1"></span>
                <strong>{{ categories.length }}</strong> categoría(s) encontrada(s)
              </small>
            </div>

            <div v-if="categories.length === 0" class="text-center py-5">
              <i class="fal fa-tags fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar categorías en el sistema</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newCategory">
                <span class="fal fa-plus me-1"></span>Crear nueva categoría
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
                      <th class="text-center">Activo</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(cat, index) in categories" :key="index">
                      <td class="fw-semibold">{{ cat.CategoryName }}</td>
                      <td class="d-none d-lg-table-cell">
                        <small class="text-muted">{{ cat.Description }}</small>
                      </td>
                      <td class="text-center">
                        <span :class="cat.IsActive ? 'badge bg-success' : 'badge bg-secondary'">
                          {{ cat.IsActive ? 'Sí' : 'No' }}
                        </span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Editar"
                          @click="editCategory(cat)"
                        >
                          <span class="fal fa-edit"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeCategory(cat.Id)"
                        >
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
                  <div class="col-12 col-sm-6" v-for="(cat, index) in categories" :key="index">
                    <div class="card h-100 shadow rounded-3">
                      <div class="card-body d-flex flex-column gap-2">
                        <div class="d-flex justify-content-between align-items-center">
                          <p class="fw-semibold mb-0 lh-sm">{{ cat.CategoryName }}</p>
                          <span class="badge rounded-pill" :class="cat.IsActive ? 'text-bg-success' : 'text-bg-secondary'">
                            {{ cat.IsActive ? 'Activo' : 'Inactivo' }}
                          </span>
                        </div>
                        <small class="text-muted">{{ cat.Description }}</small>
                        <div class="d-flex gap-2 mt-auto pt-1">
                          <button type="button" class="btn btn-sm btn-outline-primary flex-grow-1" @click="editCategory(cat)">
                            <span class="fal fa-edit me-1"></span>Editar
                          </button>
                          <button type="button" class="btn btn-sm btn-outline-danger" @click="removeCategory(cat.Id)">
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
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import useCategory from '@/modules/inventory/composables/useCategory';
import type { Category } from '@/modules/inventory/models/category.model';
import utils from '@/utils/msg';

const categories = ref<Category[]>([]);
const filtro = ref('');
const { getCategories, deleteCategory } = useCategory();
const router = useRouter();

const getCategoriesData = async () => {
  const { Data } = await getCategories(filtro.value);
  categories.value = Data;
};

const newCategory = () => {
  router.push({ name: 'category-edit', params: { id: '0' } });
};

const editCategory = (cat: Category) => {
  router.push({ name: 'category-edit', params: { id: cat.Id } });
};

const removeCategory = async (id: string) => {
  const respuesta = await utils.showMessageQuestion('¿Desea eliminar la categoría?');
  if (respuesta) {
    const { ok } = await deleteCategory(id);
    if (ok) {
      await utils.showMessageModal({ Description: 'La categoría se eliminó correctamente.', MessageType: 'success' });
      await getCategoriesData();
    }
  }
};
</script>

<style scoped></style>
