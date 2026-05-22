import { defineStore } from 'pinia';
import { ref } from 'vue';

export type DialogType = 'success' | 'error' | 'warning' | 'info' | 'question';

interface DialogOptions {
  title?: string;
  message: string;
  type?: DialogType;
  showCancel?: boolean;
  confirmText?: string;
  cancelText?: string;
  confirmButtonClass?: string;
  cancelButtonClass?: string;
}

export const useDialogStore = defineStore('dialog', () => {
  const isVisible = ref(false);
  const options = ref<DialogOptions>({
    title: '',
    message: '',
    type: 'info',
    showCancel: false,
    confirmText: 'Aceptar',
    cancelText: 'Cancelar',
    confirmButtonClass: 'btn-primary',
    cancelButtonClass: 'btn-outline-secondary'
  });

  let resolveCallback: ((value: boolean) => void) | null = null;

  const show = (newOptions: DialogOptions | string): Promise<boolean> => {
    return new Promise((resolve) => {
      if (typeof newOptions === 'string') {
        options.value = { ...options.value, message: newOptions, type: 'info', showCancel: false };
      } else {
        options.value = {
          title: newOptions.title || '',
          message: newOptions.message,
          type: newOptions.type || 'info',
          showCancel: newOptions.showCancel ?? false,
          confirmText: newOptions.confirmText || 'Aceptar',
          cancelText: newOptions.cancelText || 'Cancelar',
          confirmButtonClass: newOptions.confirmButtonClass || getDefaultConfirmClass(newOptions.type),
          cancelButtonClass: newOptions.cancelButtonClass || 'btn-outline-secondary'
        };
      }

      isVisible.value = true;
      resolveCallback = resolve;
    });
  };

  const confirm = () => {
    isVisible.value = false;
    if (resolveCallback) resolveCallback(true);
    reset();
  };

  const cancel = () => {
    isVisible.value = false;
    if (resolveCallback) resolveCallback(false);
    reset();
  };

  const reset = () => {
    // Timeout to clear after transition
    setTimeout(() => {
        resolveCallback = null;
    }, 300);
  };

  const getDefaultConfirmClass = (type?: DialogType): string => {
    switch (type) {
      case 'success': return 'btn-success';
      case 'error': return 'btn-danger';
      case 'warning': return 'btn-warning text-dark';
      case 'question': return 'btn-primary';
      default: return 'btn-primary';
    }
  };

  return {
    isVisible,
    options,
    show,
    confirm,
    cancel
  };
});
