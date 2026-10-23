import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MsgService } from '../../../core/services/msg.service';
import { firstOfMonthIso, formatDateOnly, todayIso } from '../../../core/utils/date-helper';
import { SharedModule } from '../../../shared/shared.module';
import { PURCHASE_STATUS, Purchase, purchaseStatusBadge, purchaseStatusLabel } from './purchase.model';
import { PurchaseService } from './purchase.service';

@Component({
  selector: 'app-purchases-admin',
  imports: [FormsModule, SharedModule],
  templateUrl: './purchases-admin.component.html',
})
export class PurchasesAdminComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly purchaseService = inject(PurchaseService);

  protected readonly formatDateOnly = formatDateOnly;
  protected readonly purchases = signal<Purchase[]>([]);
  protected filter = { dateInitial: firstOfMonthIso(), dateEnd: todayIso(), statusId: 1 };

  ngOnInit() {
    return this.search();
  }

  protected formatCurrency(val: number): string {
    return val?.toLocaleString('es-BO', { style: 'currency', currency: 'BOB' }) ?? 'Bs. 0.00';
  }

  protected readonly statusBadge = purchaseStatusBadge;
  protected readonly statusLabel = purchaseStatusLabel;

  // Las acciones disponibles se derivan del estado, igual que en el backend.
  // Ocultarlas es solo comodidad: el servidor vuelve a validar cada una.
  protected canReceive(statusId: number): boolean {
    return statusId === PURCHASE_STATUS.REQUESTED || statusId === PURCHASE_STATUS.PARTIALLY_RECEIVED;
  }

  protected canClose(statusId: number): boolean {
    return statusId === PURCHASE_STATUS.PARTIALLY_RECEIVED;
  }

  /** Editar, anular o eliminar solo mientras la orden no haya recibido nada. */
  protected canModify(statusId: number): boolean {
    return statusId === PURCHASE_STATUS.REQUESTED;
  }

  protected isFinal(statusId: number): boolean {
    return statusId === PURCHASE_STATUS.TOTALLY_RECEIVED
      || statusId === PURCHASE_STATUS.CANCELLED
      || statusId === PURCHASE_STATUS.CLOSED;
  }

  async search() {
    const { Data } = await this.purchaseService.getAll(this.filter.dateInitial, this.filter.dateEnd, Number(this.filter.statusId));
    this.purchases.set(Data ?? []);
  }

  protected create() {
    this.router.navigate(['purchase-edit', '0']);
  }

  protected edit(id: string) {
    this.router.navigate(['purchase-edit', id]);
  }

  protected receive(id: string) {
    this.router.navigate(['purchase-receive', id]);
  }

  protected async remove(id: string) {
    if (!(await this.msg.showMessageQuestion('¿Desea eliminar la compra?'))) return;

    const { ok } = await this.purchaseService.remove(id);
    if (ok) {
      await this.msg.showMessageModal({ Description: 'La compra se eliminó correctamente.', MessageType: 'success' });
      await this.search();
    }
  }

  protected async closeWithShortage(id: string) {
    if (!(await this.msg.showMessageQuestion('¿Cerrar la orden con faltante? El saldo pendiente ya no se podrá recibir.'))) return;

    const { ok } = await this.purchaseService.close(id);
    if (ok) {
      await this.msg.showMessageModal({ Description: 'La orden se cerró con faltante.', MessageType: 'success' });
      await this.search();
    }
  }

  protected async cancelOrder(id: string) {
    if (!(await this.msg.showMessageQuestion('¿Desea anular esta orden de compra?'))) return;

    const { ok } = await this.purchaseService.cancel(id);
    if (ok) {
      await this.msg.showMessageModal({ Description: 'La orden se anuló correctamente.', MessageType: 'success' });
      await this.search();
    }
  }
}
