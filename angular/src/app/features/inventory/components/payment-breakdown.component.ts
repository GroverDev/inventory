import { Component, input } from '@angular/core';

import { remixIcon } from '../../../core/utils/icon-map';
import { PaymentMethodTotal } from '../sales/sale.model';

/**
 * Neto por medio de pago, calculado en el servidor: cobrado (entregado menos el
 * vuelto) menos lo reintegrado por devoluciones con ese medio.
 */
@Component({
  selector: 'app-payment-breakdown',
  template: `
    @if (variant() === 'cards' && items().length) {
      <!-- Tarjetas: totales de un período o de un turno -->
      <div class="row g-2">
        @for (m of items(); track m.PaymentMethodId ?? 'sin-medio') {
          <div class="col-6 col-md-3">
            <div class="border rounded p-2 h-100">
              <div class="small text-muted text-truncate"><i [class]="icon(m.IconCss) + ' me-1'"></i>{{ m.Name }}</div>
              <div class="fw-bold fs-5">Bs. {{ fmt(m.Net) }}</div>
              @if (m.Refunded > 0) {
                <small class="text-warning-emphasis d-block">cobrado {{ fmt(m.Collected) }} · devuelto − {{ fmt(m.Refunded) }}</small>
              }
            </div>
          </div>
        }
      </div>
    } @else if (variant() === 'inline' && items().length) {
      <!-- En línea: dentro de una celda de tabla -->
      <div class="small text-muted">
        @for (m of items(); track m.PaymentMethodId ?? 'sin-medio'; let i = $index) {
          <span class="text-nowrap">
            @if (i > 0) {<span class="mx-1">·</span>}
            <i [class]="icon(m.IconCss) + ' me-1'"></i>{{ m.Name }} {{ fmt(m.Net) }}
          </span>
        }
      </div>
    }
  `,
})
export class PaymentBreakdownComponent {
  readonly items = input.required<PaymentMethodTotal[]>();
  readonly variant = input<'cards' | 'inline'>('cards');

  protected icon(faClass: string): string {
    return remixIcon(faClass);
  }

  protected fmt(v: number): string {
    return (v ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }
}
