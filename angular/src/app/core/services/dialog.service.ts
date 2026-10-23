import { Injectable } from '@angular/core';
import Swal, { SweetAlertIcon } from 'sweetalert2';

export type DialogType = 'success' | 'error' | 'warning' | 'info' | 'question';

export interface DialogOptions {
  title?: string;
  message: string;
  type?: DialogType;
  showCancel?: boolean;
  confirmText?: string;
  cancelText?: string;
}

/** Diálogo modal basado en promesas: `await dialog.show({...})` devuelve true si se confirmó. */
@Injectable({ providedIn: 'root' })
export class DialogService {
  async show(options: DialogOptions | string): Promise<boolean> {
    const o: DialogOptions = typeof options === 'string' ? { message: options } : options;
    const type: DialogType = o.type ?? 'info';

    const result = await Swal.fire({
      title: o.title || undefined,
      text: o.message,
      icon: type as SweetAlertIcon,
      showCancelButton: o.showCancel ?? false,
      confirmButtonText: o.confirmText || 'Aceptar',
      cancelButtonText: o.cancelText || 'Cancelar',
      buttonsStyling: false,
      customClass: {
        confirmButton: `btn ${this.confirmClass(type)} w-xs me-2 mt-2`,
        cancelButton: 'btn btn-outline-secondary w-xs mt-2',
      },
    });
    return result.isConfirmed;
  }

  private confirmClass(type: DialogType): string {
    switch (type) {
      case 'success': return 'btn-success';
      case 'error': return 'btn-danger';
      case 'warning': return 'btn-warning';
      default: return 'btn-primary';
    }
  }
}
