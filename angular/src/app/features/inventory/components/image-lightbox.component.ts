import { Component, effect, input, output, signal } from '@angular/core';

/** Visor de imagen ampliada a pantalla completa; se cierra con clic fuera, con la X o con Esc. */
@Component({
  selector: 'app-image-lightbox',
  template: `
    @if (open()) {
      <div class="lightbox" role="dialog" aria-modal="true" [attr.aria-label]="caption() || 'Imagen ampliada'" (click)="onBackdrop($event)">
        <button type="button" class="lightbox-close" aria-label="Cerrar" (click)="close.emit()">
          <i class="ri-close-line"></i>
        </button>

        <figure class="lightbox-figure">
          <!-- Mientras llega la grande se muestra la miniatura, que ya está en caché. -->
          <img [src]="loaded() || !placeholder() ? src() : placeholder()" [alt]="caption()" (error)="failed.set(true)">
          @if (caption()) {<figcaption>{{ caption() }}</figcaption>}
          @if (failed()) {<p class="text-white-50 small mb-0">No se pudo cargar la imagen.</p>}
        </figure>
      </div>
    }
  `,
  styles: `
    /* Por encima de los modales de Bootstrap (1055) y de su fondo (1050). */
    .lightbox { position: fixed; inset: 0; z-index: 2000; display: flex; align-items: center; justify-content: center; padding: 1rem; background: rgba(0, 0, 0, 0.85); }
    .lightbox-figure { margin: 0; text-align: center; }
    .lightbox-figure img { display: block; margin: 0 auto; max-width: min(92vw, 800px); max-height: 80vh; object-fit: contain; border-radius: 0.5rem; background: #fff; }
    .lightbox-figure figcaption { margin-top: 0.75rem; color: #fff; font-size: 0.95rem; }
    .lightbox-close { position: absolute; top: 0.75rem; right: 1rem; width: 2.5rem; height: 2.5rem; border: 0; border-radius: 50%; background: rgba(255, 255, 255, 0.15); color: #fff; font-size: 1.4rem; line-height: 1; }
    .lightbox-close:hover { background: rgba(255, 255, 255, 0.3); }
  `,
})
export class ImageLightboxComponent {
  readonly open = input.required<boolean>();
  /** Imagen completa. */
  readonly src = input.required<string>();
  /** Se ve mientras carga la completa (la miniatura). Opcional. */
  readonly placeholder = input<string | undefined>(undefined);
  readonly caption = input('');
  readonly close = output<void>();

  protected readonly loaded = signal(false);
  protected readonly failed = signal(false);

  // En fase de captura, para cerrar solo el visor: sin esto el Esc también
  // llegaría al modal de abajo.
  private readonly onKey = (e: KeyboardEvent) => {
    if (e.key !== 'Escape') return;
    e.stopPropagation();
    this.close.emit();
  };

  constructor() {
    effect(onCleanup => {
      if (!this.open()) return;

      this.loaded.set(false);
      this.failed.set(false);
      window.addEventListener('keydown', this.onKey, true);
      onCleanup(() => window.removeEventListener('keydown', this.onKey, true));

      if (this.placeholder()) {
        const pre = new Image();
        pre.onload = () => this.loaded.set(true);
        pre.src = this.src();
      }
    });
  }

  protected onBackdrop(event: MouseEvent) {
    if (event.target === event.currentTarget) this.close.emit();
  }
}
