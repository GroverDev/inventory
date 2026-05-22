<template>
  <div class="content-wrapper pt-1">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Inventario</li>
        <li class="breadcrumb-item active" aria-current="page">Unidades de Medida</li>
      </ol>
    </nav>
    <div class="main-content">
      <div class="panel panel-icon">
        <div class="panel-hdr">
          <h2>Gestión de <span class="fw-300"><i>UNIDADES DE MEDIDA</i></span></h2>
        </div>
        <div class="panel-container show">
          <div class="panel-content pt-0">
            <div class="mt-0 mb-4">
              <button type="button" class="btn btn-sm btn-primary" @click="newUom">
                <span class="fal fa-plus-square me-1"></span>Nueva Unidad
              </button>
            </div>
            <div class="row align-items-end g-2 mb-3">
              <div class="col-12 col-md-7 col-lg-6">
                <label class="form-label">Nombre de la unidad</label>
                <div class="input-group input-group body-bg shadow-inset-2 rounded">
                  <span class="input-group-text bg-transparent border-end-0 py-1 px-3">
                    <i class="sa sa-magnifier text-success"></i>
                  </span>
                  <input
                    type="text"
                    class="form-control border-start-0 bg-transparent ps-0"
                    v-model.trim="filtro"
                    placeholder="Ingrese el nombre de la unidad..."
                    autocomplete="off"
                    @keyup.enter="getUomsData"
                  />
                  <button class="btn btn-primary" type="button" @click="getUomsData">Buscar</button>
                </div>
              </div>
            </div>

            <div v-if="uoms.length > 0" class="mb-2">
              <small class="text-muted">
                <span class="fal fa-list me-1"></span>
                <strong>{{ uoms.length }}</strong> unidad(es) encontrada(s)
              </small>
            </div>

            <div v-if="uoms.length === 0" class="text-center py-5">
              <i class="fal fa-ruler fa-3x text-muted d-block mb-3"></i>
              <p class="text-muted mb-2">Ingrese un nombre para buscar unidades de medida</p>
              <button type="button" class="btn btn-sm btn-outline-primary" @click="newUom">
                <span class="fal fa-plus me-1"></span>Crear nueva unidad
              </button>
            </div>

            <template v-else>
              <!-- Tabla (desktop md+) -->
              <div class="d-none d-md-block">
                <table class="table table-hover table-sm align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th>Nombre</th>
                      <th class="text-center">Proporción</th>
                      <th class="text-center">Redondeo</th>
                      <th class="text-center">Mayor que default</th>
                      <th class="text-center">Default</th>
                      <th class="text-center">Activo</th>
                      <th class="text-center">Acciones</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(uom, index) in uoms" :key="index">
                      <td class="fw-semibold">{{ uom.UnitName }}</td>
                      <td class="text-center">{{ uom.Proportion }}</td>
                      <td class="text-center">{{ uom.PrecisionRounding }}</td>
                      <td class="text-center">
                        <span :class="uom.IsLargeThanDefault ? 'badge bg-info' : 'badge bg-light text-secondary border'">
                          {{ uom.IsLargeThanDefault ? 'Sí' : 'No' }}
                        </span>
                      </td>
                      <td class="text-center">
                        <span :class="uom.IsDefault ? 'badge bg-warning text-dark' : 'badge bg-light text-secondary border'">
                          {{ uom.IsDefault ? 'Sí' : 'No' }}
                        </span>
                      </td>
                      <td class="text-center">
                        <span :class="uom.IsActive ? 'badge bg-success' : 'badge bg-secondary'">
                          {{ uom.IsActive ? 'Sí' : 'No' }}
                        </span>
                      </td>
                      <td class="text-center text-nowrap">
                        <button
                          type="button"
                          class="btn btn-outline-primary btn-sm me-1"
                          title="Editar"
                          @click="editUom(uom)"
                        >
                          <span class="fal fa-edit"></span>
                        </button>
                        <button
                          type="button"
                          class="btn btn-outline-danger btn-sm"
                          title="Eliminar"
                          @click="removeUom(uom.Id)"
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
                  <div class="col-12 col-sm-6" v-for="(uom, index) in uoms" :key="index">
                    <div class="card h-100">
                      <div class="card-body d-flex flex-column">
                        <div class="d-flex justify-content-between align-items-start mb-1">
                          <h6 class="card-title mb-0">{{ uom.UnitName }}</h6>
                          <span :class="uom.IsActive ? 'badge bg-success ms-2' : 'badge bg-secondary ms-2'">
                            {{ uom.IsActive ? 'Activo' : 'Inactivo' }}
                          </span>
                        </div>
                        <small class="text-muted mb-1">Proporción: {{ uom.Proportion }} | Redondeo: {{ uom.PrecisionRounding }}</small>
                        <div class="d-flex gap-1 mb-2">
                          <span v-if="uom.IsDefault" class="badge bg-warning text-dark">Default</span>
                          <span v-if="uom.IsLargeThanDefault" class="badge bg-info">Mayor</span>
                        </div>
                        <div class="mt-auto">
                          <div class="btn-group w-100" role="group">
                            <button type="button" class="btn btn-outline-primary btn-sm" @click="editUom(uom)">
                              <span class="fal fa-edit me-1"></span>Editar
                            </button>
                            <button type="button" class="btn btn-outline-danger btn-sm" @click="removeUom(uom.Id)">
                              <span class="fal fa-trash-alt me-1"></span>Eliminar
                            </button>
                          </div>
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
import useUnitOfMeasurement from '@/modules/inventory/composables/useUnitOfMeasurement';
import type { UnitOfMeasurement } from '@/modules/inventory/models/unitOfMeasurement.model';
import utils from '@/utils/msg';

const uoms = ref<UnitOfMeasurement[]>([]);
const filtro = ref('');
const { getUnitsOfMeasurement, deleteUnitOfMeasurement } = useUnitOfMeasurement();
const router = useRouter();

const getUomsData = async () => {
  const { Data } = await getUnitsOfMeasurement(filtro.value);
  uoms.value = Data;
};

const newUom = () => {
  router.push({ name: 'uom-edit', params: { id: '0' } });
};

const editUom = (uom: UnitOfMeasurement) => {
  router.push({ name: 'uom-edit', params: { id: uom.Id } });
};

const removeUom = async (id: string) => {
  const respuesta = await utils.showMessageQuestion('¿Desea eliminar la unidad de medida?');
  if (respuesta) {
    const { ok } = await deleteUnitOfMeasurement(id);
    if (ok) {
      await utils.showMessageModal({ Description: 'La unidad de medida se eliminó correctamente.', MessageType: 'success' });
      await getUomsData();
    }
  }
};
</script>

<style scoped></style>
