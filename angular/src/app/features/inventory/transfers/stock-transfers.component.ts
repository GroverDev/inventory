import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { firstOfMonthIso, todayIso } from '../../../core/utils/date-helper';
import { SharedModule } from '../../../shared/shared.module';
import { StockTransfer, TransferStatus, transferStatusBadge, transferStatusLabel } from './stock-transfer.model';
import { StockTransferService } from './stock-transfer.service';

/** Traspasos de la sucursal activa: los que envía y los que le llegan. */
@Component({
  selector: 'app-stock-transfers',
  imports: [FormsModule, SharedModule],
  templateUrl: './stock-transfers.component.html',
})
export class StockTransfersComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly permissions = inject(PermissionsService);
  private readonly transferService = inject(StockTransferService);
  protected readonly auth = inject(AuthService);

  protected readonly statusLabel = transferStatusLabel;
  protected readonly statusBadge = transferStatusBadge;
  protected readonly statuses = Object.keys(transferStatusLabel) as TransferStatus[];

  protected readonly canCreate = computed(() => this.permissions.can('stock-transfers', 'create'));
  protected readonly transfers = signal<StockTransfer[]>([]);
  protected readonly pendingToReceive = computed(() =>
    this.transfers().filter(t => t.Direction === 'entrada' && t.Status === 'enviado').length);

  protected dateFrom = firstOfMonthIso();
  protected dateTo = todayIso();
  protected status = '';

  ngOnInit() {
    return this.load();
  }

  protected async load() {
    const { ok, Data } = await this.transferService.getAll(this.dateFrom, this.dateTo, this.status);
    if (ok) this.transfers.set(Data);
  }

  /** Lo que se puede hacer con el traspaso desde esta sucursal, para el botón de la fila. */
  protected actionFor(t: StockTransfer) {
    if (t.Status === 'enviado' && t.Direction === 'entrada') return { label: 'Recibir', icon: 'ri-inbox-unarchive-line', cls: 'btn-warning' };
    if (t.Status === 'borrador' && t.Direction === 'salida') return { label: 'Continuar', icon: 'ri-edit-2-line', cls: 'btn-outline-primary' };
    return { label: 'Ver', icon: 'ri-eye-line', cls: 'btn-outline-secondary' };
  }

  protected open(id: string) {
    this.router.navigate(['stock-transfer', id]);
  }

  protected formatDateTime(value: string): string {
    return value
      ? new Date(value).toLocaleString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false })
      : '—';
  }
}
