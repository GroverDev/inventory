import { Component, ElementRef, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Product } from '../products/product.model';
import { ProductService } from '../products/product.service';

const MOTIVOS_FRECUENTES = ['Más económico', 'Misma composición', 'Cuando no hay stock', 'El cliente lo prefiere'];

const formatCurrency = (val: number): string =>
  `Bs. ${(val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

/** Elegir un producto para sugerirlo como alternativa de otro, en dos pasos: producto y motivo. */
@Component({
  selector: 'app-alternative-picker-modal',
  imports: [FormsModule],
  templateUrl: './alternative-picker-modal.component.html',
})
export class AlternativePickerModalComponent {
  private readonly products = inject(ProductService);

  readonly visible = input.required<boolean>();
  /** Producto que se está editando: no puede sugerirse a sí mismo. */
  readonly productId = input.required<string>();
  readonly productName = input('');
  readonly productPrice = input(0);
  /** Ya sugeridos o deducidos por composición: se ocultan de los resultados. */
  readonly excludeIds = input<string[]>([]);
  /** Precarga del atajo desde los equivalentes automáticos. */
  readonly preselected = input<Product | null>(null);
  readonly preselectedReason = input('');

  readonly close = output<void>();
  readonly confirm = output<{ productId: string; reason: string }>();

  protected readonly motivosFrecuentes = MOTIVOS_FRECUENTES;
  protected readonly formatCurrency = formatCurrency;

  protected busqueda = '';
  protected motivo = '';
  protected readonly resultados = signal<Product[]>([]);
  protected readonly ocultos = signal(0);
  protected readonly buscando = signal(false);
  protected readonly buscado = signal(false);
  protected readonly elegido = signal<Product | null>(null);

  private readonly buscador = viewChild<ElementRef<HTMLInputElement>>('buscador');

  constructor() {
    effect(() => {
      if (!this.visible()) return;
      this.reiniciar();
      // Espera a que el modal se pinte para poder enfocar el buscador.
      setTimeout(() => this.buscador()?.nativeElement.focus());
    });
  }

  private reiniciar() {
    this.busqueda = '';
    this.resultados.set([]);
    this.ocultos.set(0);
    this.buscado.set(false);
    this.elegido.set(this.preselected() ?? null);
    this.motivo = this.preselectedReason();
  }

  /** Contra el producto que se está editando, que es la comparación que importa. */
  protected diferencia(precio: number): string {
    const d = +(precio - this.productPrice()).toFixed(2);
    if (d === 0) return 'igual precio';
    return `${d > 0 ? '+' : '−'} ${formatCurrency(Math.abs(d))}`;
  }

  protected claseDiferencia(precio: number): string {
    const d = +(precio - this.productPrice()).toFixed(2);
    if (d < 0) return 'text-success fw-semibold';
    if (d > 0) return 'text-danger';
    return 'text-muted';
  }

  protected async buscar() {
    const term = this.busqueda.trim();
    if (term.length < 3) return;

    this.buscando.set(true);
    try {
      const { ok, Data } = await this.products.getByName(term);
      const todos = ok ? Data : [];
      // Se excluye el producto mismo, lo ya sugerido y lo que el sistema deduce:
      // ofrecerlos sería invitar a duplicar algo que ya está resuelto.
      const visibles = todos.filter(p => p.Id !== this.productId() && !this.excludeIds().includes(p.Id));
      this.ocultos.set(todos.length - visibles.length);
      this.resultados.set(visibles);
      this.buscado.set(true);
    } finally {
      this.buscando.set(false);
    }
  }

  protected elegir(p: Product) {
    if (p.CurrentStock <= 0) return;
    this.elegido.set(p);
    if (!this.motivo) this.motivo = p.SalePrice < this.productPrice() ? 'Más económico' : '';
  }

  protected confirmar() {
    const elegido = this.elegido();
    if (!elegido) return;
    this.confirm.emit({ productId: elegido.Id, reason: this.motivo.trim() });
  }
}
