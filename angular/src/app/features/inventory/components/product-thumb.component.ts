import { Component, computed, input, signal, effect } from '@angular/core';

import { mediaUrl } from '../../../core/utils/media-url';

/** Miniatura de un producto, con marcador si no tiene imagen o no se pudo cargar. */
@Component({
  selector: 'app-product-thumb',
  template: `
    <span class="thumb d-inline-flex align-items-center justify-content-center flex-shrink-0 overflow-hidden rounded bg-body-secondary text-muted"
          [style.width.px]="fill() ? null : size()" [style.height.px]="fill() ? null : size()"
          [style.width]="fill() ? '100%' : null" [style.aspect-ratio]="fill() ? '1 / 1' : null">
      @if (src() && !failed()) {
        <img [src]="src()" [alt]="alt()" loading="lazy" decoding="async" class="w-100 h-100" style="object-fit: cover" (error)="failed.set(true)">
      } @else {
        <i class="ri-image-line" [style.font-size.px]="fill() ? 32 : size() / 2.5"></i>
      }
    </span>
  `,
})
export class ProductThumbComponent {
  /** Ruta relativa que devuelve la API (`ImagePath`). */
  readonly path = input<string | null | undefined>(null);
  /** Lado en px; se ignora con `fill`. */
  readonly size = input(40);
  /** Ocupa todo el ancho disponible, en cuadrado. Para las tarjetas. */
  readonly fill = input(false);
  /** Imagen completa (800 px) en vez de la miniatura. */
  readonly full = input(false);
  readonly alt = input('');

  protected readonly src = computed(() => mediaUrl(this.path(), !this.full()));

  // Si el archivo no está (404) se muestra el marcador en vez del ícono roto del
  // navegador. Al cambiar la ruta se vuelve a intentar.
  protected readonly failed = signal(false);

  constructor() {
    effect(() => {
      this.path();
      this.failed.set(false);
    });
  }
}
