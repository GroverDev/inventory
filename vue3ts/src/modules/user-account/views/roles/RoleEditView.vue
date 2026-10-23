<template>
  <Layout>
    <PageHeader :title="localRole.Id ? 'Editar Rol' : 'Nuevo Rol'" page-title="Registro de Roles" />


    <div class="row g-3">

        <!-- Panel 1: Datos del Rol -->
        <div class="col-12" :class="localRole.Id ? 'col-xl-5' : ''">
          <div class="card">
            <div class="card-header d-flex align-items-center">
              <h5 class="card-title mb-0">
                {{ localRole.Id ? 'Editar' : 'Nuevo' }}
                <span class="fw-light"><i> Rol</i></span>
              </h5>
              <div v-if="localRole.Id" class="ms-auto">
                <span class="badge" :class="localRole.State ? 'bg-success' : 'bg-secondary'">
                  {{ localRole.State ? 'Activo' : 'Inactivo' }}
                </span>
              </div>
            </div>
            <!-- Barra de acciones -->
              <div class="pt-0 card-body">
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
                            <span class="ri-save-line me-1"></span>Grabar
                          </button>
                          <button type="button" class="dropdown-item"
                            @click="returnPage">
                            <span class="ri-prohibited-line me-1"></span>Cancelar
                          </button>
                        </div>
                      </div>
                    </div>
                    <div class="d-none d-md-flex gap-2">
                      <button type="button" class="btn btn-sm btn-primary"
                        :disabled="isSaved" @click="saveRole">
                        <span class="ri-save-line me-1"></span>Grabar
                      </button>
                      <button type="button" class="btn btn-warning btn-sm" @click="returnPage">
                        <span class="ri-prohibited-line me-1"></span>Cancelar
                      </button>
                    </div>
                  </div>
                  <div class="col-4 text-end">
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
                    <i class="ri-profile-line me-1"></i> Identificación del Rol
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

        <!-- Panel 2: Asignación de Formularios (solo en modo edición) -->
        <div v-if="localRole.Id" class="col-12 col-xl-7">
          <div class="card">
            <div class="card-header">
              <h5 class="card-title mb-0">Asignación de <span class="fw-light"><i>Formularios</i></span></h5>
            </div>
            <!-- Barra de acciones -->
              <div class="pt-0 card-body">
                <div class="row align-items-center">
                  <div class="col-8">
                    <div class="d-none d-md-flex gap-2">
                      <button type="button" class="btn btn-sm btn-primary" @click="saveForms">
                        <span class="ri-save-line me-1"></span>Guardar Asignación
                      </button>
                      <button type="button" class="btn btn-sm btn-outline-secondary" @click="toggleAll">
                        <span class="ri-checkbox-line me-1"></span>
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
                            <span class="ri-save-line me-1"></span>Guardar Asignación
                          </button>
                          <button type="button" class="dropdown-item" @click="toggleAll">
                            <span class="ri-checkbox-line me-1"></span>
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
              <div class="pt-0 card-body">
                <div v-if="allForms.length === 0" class="text-center py-4">
                  <i class="ri-loader-4-line ri-spin ri-2x text-muted d-block mb-2"></i>
                  <small class="text-muted">Cargando formularios...</small>
                </div>

                <FormTreeNode
                  v-for="root in formsAsTree"
                  :key="root.form.Id"
                  :node="root"
                  :level="0"
                  :selected-ids="selectedFormIds"
                  @update:selected-ids="selectedFormIds = normalizeSelection($event)"
                  class="mb-1"
                />
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

// El menu de la app se arma por jerarquia padre->hijo: un formulario cuyo padre no esta
// asignado al rol queda huerfano y no se muestra nunca. Esto espeja lo que hace
// AssignFormsToRole en el backend, para que el arbol refleje lo que se va a guardar: cada
// pantalla marcada arrastra a sus ancestros, y los contenedores que quedan sin ninguna
// pantalla debajo se descartan, porque solo darian un grupo vacio en el menu.
const normalizeSelection = (ids: number[]): number[] => {
  const byId = new Map<number, Form>(allForms.value.map(f => [f.Id, f]));
  const result = new Set<number>();

  ids.forEach(id => {
    const form = byId.get(id);
    if (!form) return;
    if (!form.IsFormRegister && form.Route === 'ninguno') return; // contenedor: entra solo como ancestro

    let current: Form | undefined = form;
    while (current && !result.has(current.Id)) {
      result.add(current.Id);
      current = byId.get(current.FormId);
    }
  });

  return [...result];
};

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
    // El backend normaliza la seleccion (agrega ancestros, descarta contenedores vacios):
    // recargar para que el arbol muestre lo que realmente quedo guardado.
    await loadAssignedForms(localRole.value.Id);
    await utils.showMessageModal({ Description: 'Los formularios se asignaron correctamente.', MessageType: 'success' });
  }
};
</script>

<style scoped></style>
