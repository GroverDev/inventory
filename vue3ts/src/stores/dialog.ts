import { defineStore } from 'pinia'
import { ref } from 'vue'

export type DialogType = 'success' | 'error' | 'warning' | 'info' | 'question'

interface DialogOptions {
  title?: string
  message: string
  type?: DialogType
  showCancel?: boolean
  confirmText?: string
  cancelText?: string
  confirmButtonClass?: string
  cancelButtonClass?: string
}

const getDefaultConfirmClass = (type?: DialogType): string => {
  switch (type) {
    case 'success': return 'btn-success'
    case 'error': return 'btn-danger'
    case 'warning': return 'btn-warning'
    default: return 'btn-primary'
  }
}

/** Diálogo modal basado en promesas: `await dialog.show({ message, type, showCancel })` devuelve el botón elegido. */
export const useDialogStore = defineStore('dialog', () => {
  const isVisible = ref(false)
  const options = ref<Required<DialogOptions>>({
    title: '',
    message: '',
    type: 'info',
    showCancel: false,
    confirmText: 'Aceptar',
    cancelText: 'Cancelar',
    confirmButtonClass: 'btn-primary',
    cancelButtonClass: 'btn-outline-secondary',
  })

  let resolveCallback: ((value: boolean) => void) | null = null

  const show = (newOptions: DialogOptions | string): Promise<boolean> =>
    new Promise((resolve) => {
      const o: DialogOptions = typeof newOptions === 'string' ? { message: newOptions } : newOptions
      options.value = {
        title: o.title || '',
        message: o.message,
        type: o.type || 'info',
        showCancel: o.showCancel ?? false,
        confirmText: o.confirmText || 'Aceptar',
        cancelText: o.cancelText || 'Cancelar',
        confirmButtonClass: o.confirmButtonClass || getDefaultConfirmClass(o.type),
        cancelButtonClass: o.cancelButtonClass || 'btn-outline-secondary',
      }
      isVisible.value = true
      resolveCallback = resolve
    })

  const close = (result: boolean) => {
    isVisible.value = false
    resolveCallback?.(result)
    resolveCallback = null
  }

  return { isVisible, options, show, confirm: () => close(true), cancel: () => close(false) }
})
