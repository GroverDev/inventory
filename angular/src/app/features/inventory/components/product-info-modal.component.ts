import { Component, computed, effect, inject, input, output, signal, untracked } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

import { renderMarkdown } from '../../../core/utils/markdown';
import { mediaUrl } from '../../../core/utils/media-url';
import { ProductEquivalent, ProductPharma, newProductPharma } from '../products/pharma.model';
import { PharmaService } from '../products/pharma.service';
import { Product } from '../products/product.model';
import { ProductService } from '../products/product.service';
import { ImageLightboxComponent } from './image-lightbox.component';

/** Ficha del producto en el punto de venta: composición, posología, alternativas y prospecto. */
@Component({
  selector: 'app-product-info-modal',
  imports: [ImageLightboxComponent],
  templateUrl: './product-info-modal.component.html',
  styles: `
    .modal { background: rgba(0, 0, 0, 0.5); z-index: 1080; }
    /* contain: se ve la foto entera, sin recortar los bordes del empaque. */
    .ficha-imagen img { max-width: 100%; max-height: 240px; object-fit: contain; background: var(--bs-body-bg); }
    @media (min-width: 768px) { .ficha-imagen { width: 220px; } }
    :host ::ng-deep .prospecto :is(h1, h2, h3) { font-size: 0.95rem; font-weight: 600; margin-top: 0.6rem; }
    :host ::ng-deep .prospecto p { margin-bottom: 0.4rem; font-size: 0.875rem; }
    :host ::ng-deep .prospecto ul { margin-bottom: 0.4rem; padding-left: 1.2rem; font-size: 0.875rem; }
  `,
})
export class ProductInfoModalComponent {
  private readonly pharmaService = inject(PharmaService);
  private readonly productService = inject(ProductService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly visible = input.required<boolean>();
  readonly producto = input<Product | null>(null);

  readonly cerrar = output<void>();
  readonly agregar = output<string>();
  /** Llegaron el precio y el stock actuales: quien tiene la lista puede ponerse al día. */
  readonly actualizado = output<{ productId: string; datos: { SalePrice: number; CurrentStock: number } }>();

  protected readonly ficha = signal<ProductPharma>(newProductPharma());
  protected readonly equivalentes = signal<ProductEquivalent[]>([]);
  protected readonly prospecto = signal('');
  protected readonly cargando = signal(false);
  protected readonly actualizando = signal(false);
  protected readonly actual = signal<{ SalePrice: number; CurrentStock: number } | null>(null);

  // Si el archivo no está (404) se omite el bloque en vez de mostrar un ícono roto.
  protected readonly imagenFallo = signal(false);
  protected readonly ampliada = signal(false);

  protected readonly precio = computed(() => this.actual()?.SalePrice ?? this.producto()?.SalePrice ?? 0);
  protected readonly stock = computed(() => this.actual()?.CurrentStock ?? this.producto()?.CurrentStock ?? 0);
  protected readonly stockClase = computed(() =>
    this.stock() > 5 ? 'bg-success-subtle text-success-emphasis'
      : this.stock() > 0 ? 'bg-warning-subtle text-warning-emphasis'
        : 'bg-danger-subtle text-danger-emphasis');

  protected readonly imagenUrl = computed(() => mediaUrl(this.producto()?.ImagePath));
  protected readonly imagenMiniatura = computed(() => mediaUrl(this.producto()?.ImagePath, true));

  protected readonly automaticas = computed(() => this.equivalentes().filter(e => !e.IsManual));
  protected readonly manuales = computed(() => this.equivalentes().filter(e => e.IsManual));
  protected readonly principiosActivos = computed(() => this.ficha().Components.filter(c => c.IsActiveIngredient));
  protected readonly excipientes = computed(() => this.ficha().Components.filter(c => !c.IsActiveIngredient));

  protected readonly excipienteNombres = computed(() => this.excipientes().map(e => e.SubstanceName).join(', '));

  protected readonly hayDatos = computed(() =>
    this.ficha().Components.length > 0 || this.equivalentes().length > 0 ||
    !!this.prospecto() || !!this.ficha().FormName || !!this.ficha().DosageReference);

  protected readonly prospectoHtml = computed<SafeHtml>(() =>
    this.sanitizer.bypassSecurityTrustHtml(renderMarkdown(this.prospecto())));

  constructor() {
    effect(() => {
      this.producto()?.ImagePath;
      this.imagenFallo.set(false);
    });
    effect(() => {
      if (!this.visible()) this.ampliada.set(false);
    });

    /**
     * Todo se pide al abrir y no con la lista de productos: son tres consultas por
     * producto, y la grilla del punto de venta muestra cientos.
     */
    effect(() => {
      const visible = this.visible();
      const producto = this.producto();
      if (!visible || !producto) return;
      untracked(() => this.cargar(producto));
    });
  }

  private async cargar(producto: Product) {
    this.cargando.set(true);
    this.actual.set(null);
    this.ficha.set(newProductPharma());
    this.equivalentes.set([]);
    this.prospecto.set('');

    try {
      const id = producto.Id;
      this.actualizando.set(true);
      // Aparte de las otras: es lo que se ve primero y no debe esperar a la ficha.
      this.productService.validateSelection(id)
        .then(r => {
          if (r.ok && r.Data && this.producto()?.Id === id) {
            this.actual.set(r.Data);
            this.actualizado.emit({ productId: id, datos: { SalePrice: r.Data.SalePrice, CurrentStock: r.Data.CurrentStock } });
          }
        })
        .finally(() => this.actualizando.set(false));

      const [f, e, p] = await Promise.all([
        this.pharmaService.getByProduct(id),
        this.pharmaService.getEquivalents(id),
        this.pharmaService.getLeaflet(id),
      ]);
      if (f.ok && f.Data) this.ficha.set({ ...newProductPharma(), ...f.Data });
      if (e.ok) this.equivalentes.set(e.Data);
      if (p.ok) this.prospecto.set(p.Data ?? '');
    } finally {
      this.cargando.set(false);
    }
  }

  protected formatNum(v: number): string {
    return (v ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  protected etiquetaTipo(t: string): string {
    return t === 'generico' ? 'Genérico' : t === 'marca' ? 'Marca' : t === 'similar' ? 'Similar' : t;
  }

  /** Negativo = la alternativa es más barata. */
  protected diferencia(e: ProductEquivalent): number {
    return e.SalePrice - this.precio();
  }

  protected abs(v: number): number {
    return Math.abs(v);
  }
}
