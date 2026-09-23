<template>
  <!-- Con una sola sucursal no hay nada que elegir: no se muestra. -->
  <div v-if="branches.length > 1" :class="colClass">
    <label class="form-label">Sucursal</label>
    <select class="form-select form-select-sm" :value="modelValue"
      @change="emit('update:modelValue', ($event.target as HTMLSelectElement).value)">
      <option value="">{{ currentName }} (actual)</option>
      <option v-for="b in others" :key="b.BranchId" :value="b.BranchId">{{ b.Name }}</option>
      <option :value="ALL">Todas mis sucursales</option>
    </select>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useAuthStore } from '@/modules/auth/stores/auth.store';

/**
 * Sucursal de un reporte. '' = la activa, un id = esa, 'all' = consolidado.
 * Ofrece solo las sucursales en las que el usuario está habilitado; el backend
 * lo vuelve a validar (BranchScope).
 */
withDefaults(defineProps<{ modelValue: string; colClass?: string }>(), { colClass: 'col-12 col-md-3' });
const emit = defineEmits<{ 'update:modelValue': [value: string] }>();

const ALL = 'all';
const authStore = useAuthStore();

const branches = computed(() => authStore.getUser?.Branches ?? []);
const currentName = computed(() => authStore.getUser?.BranchName ?? '');
const others = computed(() => branches.value.filter(b => b.BranchId !== authStore.getUser?.BranchId));
</script>
