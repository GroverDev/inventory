<template>
  <div class="content-wrapper pt-1 px-3">
    <nav class="app-breadcrumb" aria-label="breadcrumb">
      <ol class="breadcrumb ms-0 text-muted mb-2">
        <li class="breadcrumb-item">Seguridad</li>
        <li class="breadcrumb-item">
          <a href="#" class="text-decoration-none" @click.prevent="returnPage">Registro de Roles</a>
        </li>
        <li class="breadcrumb-item active" aria-current="page">
          {{ localRole.Id ? 'Editar Rol' : 'Nuevo Rol' }}
        </li>
      </ol>
    </nav>

    <div class="main-content">
      <div class="row g-3">

        <!-- Panel 1: Datos del Rol -->
        <div class="col-12" :class="localRole.Id ? 'col-xl-5' : ''">
          <div class="panel panel-icon">
            <div class="panel-hdr">
              <h2>
                {{ localRole.Id ? 'Editar' : 'Nuevo' }}
                <span class="fw-300"><i> Rol</i></span>
              </h2>
              <div v-if="localRole.Id" class="panel-toolbar ms-auto">
                <span class="badge" :class="localRole.State ? 'bg-success' : 'bg-secondary'">
                  {{ localRole.State ? 'Activo' : 'Inactivo' }}
                </span>
              </div>
            </div>
            <div class="panel-container show">

              <!-- Barra de acciones -->
              <div class="panel-content pt-0">
                <div class="row align-items-center">
                  <div class="col-8">
                    <div class="d-md-none">
                      <div class="btn-group">
                        <button type="button" class="btn btn-primary dropdown-toggle"
                          data-bs-toggle="dropdown" data-bs-display="static" aria-expanded="false">
                          Opciones
                        </button>
                        <div class="dropdown-menu dropdown-menu-lg-right">
                          <button type="button" class="dropdown-item border-bottom border-1"
                            :disabled="isSaved" @click="saveRole">
                            <span class="fal fa-save me-1"></span>Grabar
                          </button>
                          <button type="button" class="dropdown-item"
                            @click="returnPage">
                            <span class="fal fa-ban me-1"></span>Cancelar
                          </button>
                        </div>
                      </div>
                    </div>
                    <div class="d-none d-md-flex gap-2">
                      <button type="button" class="btn btn-sm btn-primary"
                        :disabled="isSaved" @click="saveRole">
                        <span class="fal fa-save me-1"></span>Grabar
                      </button>
                      <button type="button" class="btn btn-warning btn-sm" @click="returnPage">
                        <span class="fal fa-ban me-1"></span>Cancelar
                      </button>
                    </div>
                  </div>
                  <div class="col-4 text-end">
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
                    <i class="fal fa-id-badge me-1"></i> Identificación del Rol
                  </h6>
                  <div class="row">
                    <div class="col-12 mb-3">
                      <label class="form-label" for="NameRol">
                        Nombre del Rol <span class="text-danger">*</span>
                      </label>
                      <input
                        type="text"
                        id="NameRol"
                        class="form-control form-control-sm"
                        :class="{ 'is-invalid': v$.NameRol.$dirty && v$.NameRol.$invalid }"
                        placeholder="Nombre del rol"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="v$.NameRol.$model"
                      />
                      <small class="invalid-feedback">El nombre del rol es requerido.</small>
                    </div>
                    <div class="col-12 mb-3">
                      <label class="form-label" for="Description">Descripción</label>
                      <input
                        type="text"
                        id="Description"
                        class="form-control form-control-sm"
                        placeholder="Descripción del rol"
                        :disabled="isSaved"
                        autocomplete="off"
                        v-model.trim="localRole.Description"
                      />
                    </div>
                  </div>

                </form>
              </div>

            </div>
          </div>
        </div>

        <!-- Panel 2: Asignación de Formularios (solo en modo edición) -->
        <div v-if="localRole.Id" class="col-12 col-xl-7">
          <div class="panel panel-icon">
            <div class="panel-hdr">
              <h2>Asignación de <span class="fw-300"><i>Formularios</i></span></h2>
            </div>
            <div class="panel-container show">

              <!-- Barra de acciones -->
              <div class="panel-content pt-0">
                <div class="row align-items-center">
                  <div class="col-8">
                    <div class="d-none d-md-flex gap-2">
                      <button type="button" class="btn btn-sm btn-primary" @click="saveForms">
                        <span class="fal fa-save me-1"></span>Guardar Asignación
                      </button>
                      <button type="button" class="btn btn-sm btn-outline-secondary" @click="toggleAll">
                        <span class="fal fa-check-square me-1"></span>
                        {{ allSelected ? 'Deseleccionar todo' : 'Seleccionar todo' }}
                      </button>
                    </div>
                    <div class="d-md-none">
                      <div class="btn-group">
                        <button type="button" class="btn btn-primary dropdown-toggle"
                          data-bs-toggle="dropdown" data-bs-display="static" aria-expanded="false">
                          Opciones
                        </button>
                        <div class="dropdown-menu">
                          <button type="button" class="dropdown-item border-bottom border-1"
                            @click="saveForms">
                            <span class="fal fa-save me-1"></span>Guardar Asignación
                          </button>
                          <button type="button" class="dropdown-item" @click="toggleAll">
                            <span class="fal fa-check-square me-1"></span>
                            {{ allSelected ? 'Deseleccionar todo' : 'Seleccionar todo' }}
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="col-4 text-end">
                    <small class="text-muted">
                      <strong>{{ selectedFormIds.length }}</strong> / {{ allForms.length }} seleccionados
                    </small>
                  </div>
                </div>
              </div>

              <!-- Árbol jerárquico de formularios -->
              <div class="panel-content pt-0">
                <div v-if="allForms.length === 0" class="text-center py-4">
                  <i class="fal fa-spinner fa-spin fa-2x text-muted d-block mb-2"></i>
                  <small class="text-muted">Cargando formularios...</small>
                </div>

                <FormTreeNode
                  v-for="root in formsAsTree"
                  :key="root.form.Id"
                  :node="root"
                  :level="0"
                  :selected-ids="selectedFormIds"
                  @update:selected-ids="selectedFormIds = $event"
                  class="mb-1"
                />
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

import { Role } from '@/modules/user-account/models/role.model';
import useRole from '@/modules/user-account/composables/useRole';
import useForm from '@/modules/user-account/composables/useForm';
import type { Form } from '@/modules/user-account/models/form.model';
import FormTreeNode from './FormTreeNode.vue';
import type { TreeNode } from './FormTreeNode.vue';

const router = useRouter();
const route = useRoute();
const { getRoleById, createRole, updateRole, getFormsByRole, assignForms } = useRole();
const { getForms } = useForm();

const localRole = ref(new Role());
const isSaved = ref(false);
const allForms = ref<Form[]>([]);
const selectedFormIds = ref<number[]>([]);

const rules = computed(() => ({
  NameRol: { required },
}));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const v$ = useVuelidate(rules, localRole as any);

const formsAsTree = computed((): TreeNode[] => {
  const nodeMap = new Map<number, TreeNode>();
  allForms.value.forEach(form => nodeMap.set(form.Id, { form, children: [] }));

  const roots: TreeNode[] = [];
  allForms.value.forEach(form => {
    const node = nodeMap.get(form.Id)!;
    const parent = nodeMap.get(form.FormId);
    if (parent) parent.children.push(node);
    else roots.push(node);
  });

  const sortNode = (n: TreeNode) => {
    n.children.sort((a, b) => a.form.Orden - b.form.Orden);
    n.children.forEach(sortNode);
  };
  roots.sort((a, b) => a.form.Orden - b.form.Orden);
  roots.forEach(sortNode);

  return roots;
});

const allSelected = computed(() => selectedFormIds.value.length === allForms.value.length && allForms.value.length > 0);

const toggleAll = () => {
  if (allSelected.value) {
    selectedFormIds.value = [];
  } else {
    selectedFormIds.value = allForms.value.map(f => f.Id);
  }
};

onMounted(async () => {
  const id = route.params.id as string;
  const roleId = parseInt(id);

  await loadAllForms();

  if (roleId > 0) {
    await loadRole(roleId);
    await loadAssignedForms(roleId);
  }
});

const loadRole = async (id: number) => {
  const { ok, Data } = await getRoleById(id);
  if (ok) localRole.value = Data;
};

const loadAllForms = async () => {
  const { ok, Data } = await getForms('');
  if (ok) allForms.value = Data;
};

const loadAssignedForms = async (rolId: number) => {
  const { ok, Data } = await getFormsByRole(rolId);
  if (ok) selectedFormIds.value = Data.map((f: Form) => f.Id);
};

const returnPage = () => {
  router.push({ name: 'roles-admin' });
};

const saveRole = async () => {
  const isValid = await v$.value.$validate();
  if (!isValid) return;

  const confirmed = await utils.showMessageQuestion('¿Desea guardar el rol?');
  if (!confirmed) return;

  if (localRole.value.Id === 0) {
    const { ok, Data: newId } = await createRole(localRole.value);
    if (ok) {
      localRole.value.Id = newId;
      await utils.showMessageModal({ Description: 'El rol se creó correctamente. Ahora puede asignar formularios.', MessageType: 'success' });
      await loadAllForms();
    }
  } else {
    const { ok } = await updateRole(localRole.value);
    if (ok) {
      await utils.showMessageModal({ Description: 'El rol se actualizó correctamente.', MessageType: 'success' });
    }
  }
};

const saveForms = async () => {
  const confirmed = await utils.showMessageQuestion('¿Desea guardar la asignación de formularios?');
  if (!confirmed) return;

  const { ok } = await assignForms({ RolId: localRole.value.Id, FormIds: selectedFormIds.value });
  if (ok) {
    await utils.showMessageModal({ Description: 'Los formularios se asignaron correctamente.', MessageType: 'success' });
  }
};
</script>

<style scoped></style>
