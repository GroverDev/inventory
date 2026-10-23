import { Component, computed, input, output } from '@angular/core';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';

/** Una acción de la barra de una pantalla de edición (Grabar, Cancelar, etc.). */
export interface FormAction {
  label: string;
  /** Ícono (Remixicon). */
  icon: string;
  /** Clase del botón en escritorio, p. ej. `btn-primary`. */
  btnClass: string;
  run: () => void;
  /** Se evalúa en cada render: es una función para que lea el estado actual de la pantalla. */
  disabled?: () => boolean;
  visible?: () => boolean;
}

/**
 * Barra de acciones de las pantallas de edición. Igual que en el Vue: en
 * escritorio los botones van sueltos y en móvil se agrupan en un desplegable
 * «Opciones»; «Volver» queda siempre a la derecha.
 */
@Component({
  selector: 'app-form-actions',
  imports: [NgbDropdownModule],
  template: `
    <div class="row align-items-center mb-3">
      <div class="col-8">
        <!-- Móvil: desplegable -->
        <div class="d-md-none" ngbDropdown>
          <button type="button" class="btn dropdown-toggle" [class]="toggleClass()" ngbDropdownToggle>Opciones</button>
          <div ngbDropdownMenu>
            @for (a of actions(); track a.label) {
              @if (a.visible?.() ?? true) {
                <button type="button" ngbDropdownItem [disabled]="a.disabled?.() ?? false" (click)="a.run()">
                  <i [class]="a.icon + ' align-middle me-1'"></i>{{ a.label }}
                </button>
              }
            }
          </div>
        </div>
        <!-- Escritorio: botones sueltos -->
        <div class="d-none d-md-flex flex-wrap gap-2">
          @for (a of actions(); track a.label) {
            @if (a.visible?.() ?? true) {
              <button type="button" class="btn btn-sm" [class]="a.btnClass" [disabled]="a.disabled?.() ?? false" (click)="a.run()">
                <i [class]="a.icon + ' align-middle me-1'"></i>{{ a.label }}
              </button>
            }
          }
        </div>
      </div>
      <div class="col-4 text-end">
        <button type="button" class="btn btn-danger btn-sm" (click)="back.emit()">
          <i class="ri-arrow-left-line align-middle me-1"></i>Volver
        </button>
      </div>
    </div>
  `,
})
export class FormActionsComponent {
  readonly actions = input.required<FormAction[]>();
  /** Clase del botón «Opciones» en móvil. */
  readonly toggleClass = input('btn-primary');
  readonly back = output<void>();

  protected readonly count = computed(() => this.actions().length);
}
