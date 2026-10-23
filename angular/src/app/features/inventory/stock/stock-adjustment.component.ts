import { Location } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FormAction, FormActionsComponent } from '../../../shared/form-actions/form-actions.component';

import { MsgService } from '../../../core/services/msg.service';
import { SharedModule } from '../../../shared/shared.module';
import { StockMovementService } from './stock-movement.service';

/** Ajuste manual de stock de un producto (entrada o salida con motivo). */
@Component({
  selector: 'app-stock-adjustment',
  imports: [FormsModule, SharedModule, FormActionsComponent],
  templateUrl: './stock-adjustment.component.html',
})
export class StockAdjustmentComponent {
  protected readonly actions: FormAction[] = [
    { label: 'Aplicar Ajuste', icon: 'ri-save-line', btnClass: 'btn-primary', run: () => this.save(), disabled: () => this.saved() },
    { label: 'Cancelar', icon: 'ri-forbid-line', btnClass: 'btn-warning', run: () => this.location.back() },
  ];

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly location = inject(Location);
  private readonly msg = inject(MsgService);
  private readonly stockMovement = inject(StockMovementService);

  private readonly productId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly productName = this.route.snapshot.queryParamMap.get('name') ?? '';
  protected readonly productCode = this.route.snapshot.queryParamMap.get('code') ?? '';
  protected readonly currentStock = Number(this.route.snapshot.queryParamMap.get('stock') ?? 0);

  protected readonly motivos = [
    'Conteo físico', 'Ingreso manual', 'Merma/Pérdida', 'Devolución de cliente',
    'Devolución a proveedor', 'Corrección de error', 'Vencimiento de producto',
  ];

  protected readonly saved = signal(false);
  protected submitted = false;
  protected adjustType: 'increase' | 'decrease' = 'increase';
  protected quantity = 0;
  protected reason = '';
  protected observation = '';

  protected get stockAfterPreview(): number {
    const qty = this.quantity > 0 ? this.quantity : 0;
    return this.adjustType === 'increase' ? this.currentStock + qty : this.currentStock - qty;
  }

  protected async save() {
    this.submitted = true;
    if (!(this.quantity >= 1) || !this.reason) return;
    if (this.stockAfterPreview < 0) {
      this.msg.showMessageModal({ Description: 'El stock resultante no puede ser negativo.', MessageType: 'warning' });
      return;
    }
    if (!(await this.msg.showMessageQuestion('¿Desea aplicar el ajuste de stock?'))) return;

    const { ok } = await this.stockMovement.createAdjustment({
      ProductId: this.productId,
      Quantity: this.adjustType === 'increase' ? this.quantity : -this.quantity,
      Reason: this.reason,
      Observation: this.observation.trim(),
    });
    if (ok) {
      this.saved.set(true);
      await this.msg.showMessageModal({ Description: 'El ajuste se aplicó correctamente.', MessageType: 'success' });
      this.router.navigate(['inventory-stock']);
    }
  }
}
