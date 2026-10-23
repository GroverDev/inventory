import { Location } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FormAction, FormActionsComponent } from '../../../shared/form-actions/form-actions.component';

import { MsgService } from '../../../core/services/msg.service';
import { formatDateOnly } from '../../../core/utils/date-helper';
import { SharedModule } from '../../../shared/shared.module';
import { StockMovementService } from './stock-movement.service';

/** Baja de una existencia puntual (lote vencido, dañado o retirado). */
@Component({
  selector: 'app-stock-write-off',
  imports: [FormsModule, SharedModule, FormActionsComponent],
  templateUrl: './stock-write-off.component.html',
})
export class StockWriteOffComponent {
  protected readonly actions: FormAction[] = [
    { label: 'Dar de Baja', icon: 'ri-delete-bin-line', btnClass: 'btn-danger', run: () => this.save(), disabled: () => this.saved() },
    { label: 'Cancelar', icon: 'ri-forbid-line', btnClass: 'btn-warning', run: () => this.location.back() },
  ];

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly location = inject(Location);
  private readonly msg = inject(MsgService);
  private readonly stockMovement = inject(StockMovementService);

  // :id es el StockItemId (la existencia/lote puntual), no el ProductId.
  private readonly stockItemId = this.route.snapshot.paramMap.get('id') ?? '';
  private readonly query = this.route.snapshot.queryParamMap;
  private readonly productId = this.query.get('productId') ?? '';
  protected readonly productName = this.query.get('name') ?? '';
  protected readonly productCode = this.query.get('code') ?? '';
  protected readonly lot = this.query.get('lot') ?? '';
  protected readonly expiry = formatDateOnly(this.query.get('expiry'));
  protected readonly availableQty = Number(this.query.get('quantity') ?? 0);

  protected readonly saved = signal(false);
  protected submitted = false;
  protected quantity = this.availableQty;
  protected reason = '';
  protected observation = '';

  protected get quantityInvalid(): boolean {
    return !(this.quantity >= 1 && this.quantity <= this.availableQty);
  }

  protected async save() {
    this.submitted = true;
    if (this.quantityInvalid || !this.reason) return;

    const confirmed = await this.msg.showMessageQuestion(
      `¿Dar de baja ${this.quantity} unidad(es) del lote "${this.lot || 'sin lote'}"? Esta acción no se puede deshacer.`);
    if (!confirmed) return;

    const { ok } = await this.stockMovement.createWriteOff({
      ProductId: this.productId,
      StockItemId: this.stockItemId,
      Quantity: this.quantity,
      Reason: this.reason,
      Observation: this.observation.trim(),
    });
    if (ok) {
      this.saved.set(true);
      await this.msg.showMessageModal({ Description: 'La baja se registró correctamente.', MessageType: 'success' });
      this.router.navigate(['stock-expiry']);
    }
  }
}
