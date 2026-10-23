import { Component, computed, input, output } from '@angular/core';

/**
 * Paginador numérico de Bootstrap: anterior, ventana de páginas y siguiente; con `edges` agrega
 * primera y última. Con `shift` la ventana mantiene 5 números aun en los extremos.
 */
@Component({
  selector: 'app-pagination',
  template: `
    @if (totalPages() > 1) {
      <nav class="d-flex justify-content-center" [class]="spacing()" aria-label="Paginación">
        <ul class="pagination pagination-sm mb-0">
          @if (edges()) {
            <li class="page-item" [class.disabled]="page() === 1">
              <button type="button" class="page-link" title="Primera página" (click)="go(1)"><i class="ri-skip-back-mini-line"></i></button>
            </li>
          }
          <li class="page-item" [class.disabled]="page() === 1">
            <button type="button" class="page-link" title="Anterior" (click)="go(page() - 1)"><i class="ri-arrow-left-s-line"></i></button>
          </li>
          @for (p of window(); track p) {
            <li class="page-item" [class.active]="p === page()">
              <button type="button" class="page-link" (click)="go(p)">{{ p }}</button>
            </li>
          }
          <li class="page-item" [class.disabled]="page() === totalPages()">
            <button type="button" class="page-link" title="Siguiente" (click)="go(page() + 1)"><i class="ri-arrow-right-s-line"></i></button>
          </li>
          @if (edges()) {
            <li class="page-item" [class.disabled]="page() === totalPages()">
              <button type="button" class="page-link" title="Última página" (click)="go(totalPages())"><i class="ri-skip-forward-mini-line"></i></button>
            </li>
          }
        </ul>
      </nav>
    }
  `,
})
export class PaginationComponent {
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  /** Botones de primera y última página. */
  readonly edges = input(true);
  /** Ventana de 5 números que se corre en los extremos (si no, se recorta). */
  readonly shift = input(true);
  /** Margen superior (`mt-4` por defecto). */
  readonly spacing = input('mt-4');
  readonly pageChange = output<number>();

  /** Ventana de a lo sumo 5 números centrada en la página actual. */
  protected readonly window = computed<number[]>(() => {
    const total = this.totalPages();
    let start = Math.max(1, this.page() - 2);
    const end = this.shift() ? Math.min(total, start + 4) : Math.min(total, this.page() + 2);
    if (this.shift()) start = Math.max(1, end - 4);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  protected go(target: number) {
    if (target >= 1 && target <= this.totalPages() && target !== this.page()) this.pageChange.emit(target);
  }
}
