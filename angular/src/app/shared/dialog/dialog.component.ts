import { Component, ElementRef, HostListener, effect, inject, viewChild } from '@angular/core';

import { DialogService, DialogType } from '../../core/services/dialog.service';

const ICONS: Record<DialogType, string> = {
  success: 'ri-checkbox-circle-line',
  error: 'ri-close-circle-line',
  warning: 'ri-error-warning-line',
  info: 'ri-information-line',
  question: 'ri-question-line',
};

/**
 * Único diálogo de la app (alertas, errores y confirmaciones). Se monta una vez en
 * AppComponent y se maneja siempre a través de `DialogService.show()`.
 */
@Component({
  selector: 'app-dialog',
  template: `
    @if (dialog.current(); as d) {
      <div class="dlg-overlay" (click)="onOverlayClick($event)">
        <div class="dlg-card shadow-lg" [class]="'dlg-' + d.type" role="dialog" aria-modal="true"
             aria-labelledby="dlg-title" aria-describedby="dlg-message">
          <div class="dlg-icon"><i [class]="icon(d.type)"></i></div>
          @if (d.title) {
            <h4 id="dlg-title" class="dlg-title">{{ d.title }}</h4>
          }
          <p id="dlg-message" class="dlg-message text-muted mb-0">{{ d.message }}</p>
          <div class="d-flex gap-2 justify-content-center mt-4">
            @if (d.showCancel) {
              <button type="button" class="btn btn-outline-secondary w-xs" (click)="dialog.cancel()">{{ d.cancelText }}</button>
            }
            <button #confirmBtn type="button" [class]="'btn ' + d.confirmClass + ' w-xs'"
                    (click)="dialog.confirm()">{{ d.confirmText }}</button>
          </div>
        </div>
      </div>
    }
  `,
  styles: `
    .dlg-overlay {
      position: fixed; inset: 0; z-index: 2100; padding: 1rem;
      display: flex; align-items: center; justify-content: center;
      background: rgba(0, 0, 0, .4);
      animation: dlg-fade .15s ease-out;
    }
    .dlg-card {
      width: 100%; max-width: 420px; padding: 1.75rem 1.5rem; text-align: center;
      background: var(--vz-secondary-bg); color: var(--vz-body-color);
      border: 1px solid var(--vz-border-color); border-radius: .75rem;
      animation: dlg-pop .2s ease-out;
    }
    .dlg-icon {
      width: 4.5rem; height: 4.5rem; margin: 0 auto 1rem; border-radius: 50%;
      display: flex; align-items: center; justify-content: center; font-size: 2.5rem;
      color: var(--dlg-color); background: color-mix(in srgb, var(--dlg-color) 12%, transparent);
    }
    .dlg-title { color: var(--vz-emphasis-color); margin-bottom: .5rem; }
    .dlg-message { font-size: 1rem; line-height: 1.5; white-space: pre-line; }
    .dlg-success { --dlg-color: var(--vz-success); }
    .dlg-error { --dlg-color: var(--vz-danger); }
    .dlg-warning { --dlg-color: var(--vz-warning); }
    .dlg-info { --dlg-color: var(--vz-info); }
    .dlg-question { --dlg-color: var(--vz-primary); }
    @keyframes dlg-fade { from { opacity: 0; } }
    @keyframes dlg-pop { from { opacity: 0; transform: scale(.92); } }
  `,
})
export class DialogComponent {
  protected readonly dialog = inject(DialogService);
  private readonly confirmBtn = viewChild<ElementRef<HTMLButtonElement>>('confirmBtn');

  constructor() {
    // Al abrirse, el foco va a "Aceptar" para que Enter confirme.
    effect(() => this.confirmBtn()?.nativeElement.focus());
  }

  protected icon(type: DialogType): string {
    return ICONS[type];
  }

  protected onOverlayClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.dialog.cancel();
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.dialog.current()) this.dialog.cancel();
  }
}
