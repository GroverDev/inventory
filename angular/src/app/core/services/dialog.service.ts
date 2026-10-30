import { Injectable, signal } from '@angular/core';

export type DialogType = 'success' | 'error' | 'warning' | 'info' | 'question';

export interface DialogOptions {
  title?: string;
  message: string;
  type?: DialogType;
  showCancel?: boolean;
  confirmText?: string;
  cancelText?: string;
}

/** Diálogo ya normalizado, tal como lo pinta `DialogComponent`. */
export interface ActiveDialog {
  title: string;
  message: string;
  type: DialogType;
  showCancel: boolean;
  confirmText: string;
  cancelText: string;
  confirmClass: string;
}

interface Pending {
  dialog: ActiveDialog;
  resolve: (confirmed: boolean) => void;
}

/**
 * Diálogo modal basado en promesas: `await dialog.show({...})` devuelve true si se confirmó.
 * Lo pinta un único `DialogComponent` montado en AppComponent. Si llega otro mientras hay
 * uno abierto, espera su turno en vez de pisarlo.
 */
@Injectable({ providedIn: 'root' })
export class DialogService {
  private readonly _current = signal<ActiveDialog | null>(null);
  readonly current = this._current.asReadonly();

  private readonly queue: Pending[] = [];
  private resolveCurrent: ((confirmed: boolean) => void) | null = null;

  show(options: DialogOptions | string): Promise<boolean> {
    const o: DialogOptions = typeof options === 'string' ? { message: options } : options;
    const type: DialogType = o.type ?? 'info';

    return new Promise<boolean>(resolve => {
      this.queue.push({
        resolve,
        dialog: {
          title: o.title ?? '',
          message: o.message,
          type,
          showCancel: o.showCancel ?? false,
          confirmText: o.confirmText || 'Aceptar',
          cancelText: o.cancelText || 'Cancelar',
          confirmClass: this.confirmClass(type),
        },
      });
      if (!this.resolveCurrent) this.next();
    });
  }

  confirm(): void {
    this.close(true);
  }

  cancel(): void {
    this.close(false);
  }

  private close(confirmed: boolean): void {
    const resolve = this.resolveCurrent;
    this.resolveCurrent = null;
    this._current.set(null);
    resolve?.(confirmed);
    this.next();
  }

  private next(): void {
    const pending = this.queue.shift();
    if (!pending) return;
    this.resolveCurrent = pending.resolve;
    this._current.set(pending.dialog);
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
