<script setup lang="ts">
import { computed } from 'vue'
import { useDialogStore, type DialogType } from '@/stores/dialog'

// Diálogo global controlado por el store: `await dialog.show({...})`. Se monta una vez en App.vue.
const dialog = useDialogStore()

const ICONS: Record<DialogType, { icon: string; color: string }> = {
  success: { icon: 'ri-checkbox-circle-line', color: 'text-success' },
  error: { icon: 'ri-close-circle-line', color: 'text-danger' },
  warning: { icon: 'ri-error-warning-line', color: 'text-warning' },
  info: { icon: 'ri-information-line', color: 'text-info' },
  question: { icon: 'ri-question-line', color: 'text-primary' },
}
const visual = computed(() => ICONS[dialog.options.type])
</script>

<template>
  <Teleport to="body">
    <template v-if="dialog.isVisible">
      <div class="modal-backdrop fade show" style="z-index: 2000"></div>
      <div class="modal fade show d-block" tabindex="-1" role="dialog" aria-modal="true" style="z-index: 2001"
        @keydown.esc="dialog.cancel()">
        <div class="modal-dialog modal-dialog-centered">
          <div class="modal-content">
            <div class="modal-body text-center p-4">
              <i :class="[visual.icon, visual.color]" style="font-size: 3rem"></i>
              <h5 v-if="dialog.options.title" class="mt-2">{{ dialog.options.title }}</h5>
              <p class="text-muted mb-0 mt-2" style="white-space: pre-line">{{ dialog.options.message }}</p>
            </div>
            <div class="modal-footer justify-content-center border-0 pt-0 pb-4">
              <button v-if="dialog.options.showCancel" type="button" class="btn" :class="dialog.options.cancelButtonClass"
                @click="dialog.cancel()">{{ dialog.options.cancelText }}</button>
              <button type="button" class="btn" :class="dialog.options.confirmButtonClass" autofocus
                @click="dialog.confirm()">{{ dialog.options.confirmText }}</button>
            </div>
          </div>
        </div>
      </div>
    </template>
  </Teleport>
</template>
