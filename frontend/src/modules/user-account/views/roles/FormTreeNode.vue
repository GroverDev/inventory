<template>
  <div>
    <div
      class="d-flex align-items-center py-1 rounded tree-row"
      :style="{ paddingLeft: `${level * 16 + 4}px` }"
    >
      <span style="width: 16px; flex-shrink: 0;" class="me-1">
        <i
          v-if="hasChildren"
          class="fal fa-fw text-muted"
          :class="isOpen ? 'fa-chevron-down' : 'fa-chevron-right'"
          style="font-size: 10px; cursor: pointer;"
          @click.stop="isOpen = !isOpen"
        ></i>
      </span>

      <input
        ref="checkboxRef"
        type="checkbox"
        class="form-check-input me-2 flex-shrink-0"
        :id="`ftree-${node.form.Id}`"
        :checked="checkState === 'checked'"
        @change="toggleNode"
      />

      <label
        class="form-check-label mb-0 user-select-none"
        :class="labelClass"
        :for="`ftree-${node.form.Id}`"
        style="cursor: pointer;"
      >
        {{ node.form.NameForm }}
      </label>
    </div>

    <template v-if="hasChildren && isOpen">
      <FormTreeNode
        v-for="child in node.children"
        :key="child.form.Id"
        :node="child"
        :level="level + 1"
        :selected-ids="selectedIds"
        @update:selected-ids="$emit('update:selectedIds', $event)"
      />
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watchEffect } from 'vue'
import type { Form } from '@/modules/user-account/models/form.model'

export interface TreeNode {
  form: Form
  children: TreeNode[]
}

defineOptions({ name: 'FormTreeNode' })

const props = defineProps<{
  node: TreeNode
  level: number
  selectedIds: number[]
}>()

const emit = defineEmits<{
  'update:selectedIds': [ids: number[]]
}>()

const isOpen = ref(true)
const checkboxRef = ref<HTMLInputElement | null>(null)

const hasChildren = computed(() => props.node.children.length > 0)

const allIds = computed((): number[] => {
  const collect = (n: TreeNode): number[] => [n.form.Id, ...n.children.flatMap(c => collect(c))]
  return collect(props.node)
})

const checkState = computed(() => {
  const selected = allIds.value.filter(id => props.selectedIds.includes(id))
  if (selected.length === 0) return 'unchecked'
  if (selected.length === allIds.value.length) return 'checked'
  return 'indeterminate'
})

watchEffect(() => {
  if (checkboxRef.value) {
    checkboxRef.value.indeterminate = checkState.value === 'indeterminate'
  }
})

const labelClass = computed(() => {
  if (props.level === 0) return 'fw-bold text-uppercase small'
  if (props.level === 1) return 'fw-semibold'
  return 'text-body'
})

const toggleNode = () => {
  const ids = allIds.value
  if (checkState.value === 'checked') {
    emit('update:selectedIds', props.selectedIds.filter(id => !ids.includes(id)))
  } else {
    const toAdd = ids.filter(id => !props.selectedIds.includes(id))
    emit('update:selectedIds', [...props.selectedIds, ...toAdd])
  }
}
</script>

<style scoped>
.tree-row:hover {
  background-color: rgba(0, 0, 0, 0.04);
}
</style>
