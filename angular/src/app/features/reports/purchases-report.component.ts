import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { firstOfMonthIso, formatDateOnly, todayIso } from '../../core/utils/date-helper';
import { exportToExcel } from '../../core/utils/excel-helper';
import { SharedModule } from '../../shared/shared.module';
import { Purchase } from '../inventory/purchases/purchase.model';
import { PurchaseService } from '../inventory/purchases/purchase.service';
import { BranchFilterComponent } from './branch-filter.component';

@Component({
  selector: 'app-purchases-report',
  imports: [FormsModule, SharedModule, BranchFilterComponent],
  templateUrl: './purchases-report.component.html',
})
export class PurchasesReportComponent {
  private readonly purchaseService = inject(PurchaseService);

  protected readonly formatDateOnly = formatDateOnly;
  protected readonly purchases = signal<Purchase[]>([]);
  protected filter = { dateInitial: firstOfMonthIso(), dateEnd: todayIso(), statusId: 1 };

  // '' = sucursal activa, un id, o 'all' (consolidado de las sucursales del usuario).
  protected readonly branch = signal('');
  protected readonly consolidated = computed(() => this.branch() === 'all');

  protected readonly totalAmount = computed(() => this.purchases().reduce((s, p) => s + p.Total, 0));
  protected readonly uniqueProviders = computed(() => new Set(this.purchases().map(p => p.ProviderId)).size);

  protected fmt(v: number): string {
    return v.toLocaleString('es-BO', { style: 'currency', currency: 'BOB' });
  }

  protected statusBadge(id: number): string {
    return id === 3 ? 'badge bg-success' : id === 2 ? 'badge bg-warning text-dark' : 'badge bg-info text-dark';
  }

  protected statusLabel(id: number): string {
    return id === 3 ? 'Tot. Recibido' : id === 2 ? 'Parc. Recibido' : 'Solicitado';
  }

  protected async load() {
    const { ok, Data } = await this.purchaseService.getAll(this.filter.dateInitial, this.filter.dateEnd, Number(this.filter.statusId), this.branch());
    if (ok) this.purchases.set(Data ?? []);
  }

  protected exportar() {
    const rows = this.purchases().map(p => ({
      Fecha: formatDateOnly(p.PurchaseDate),
      Sucursal: p.BranchName ?? '',
      Proveedor: p.ProviderName,
      Estado: this.statusLabel(p.PurchaseStatusId),
      Entrega_Estimada: p.EstimatedDeliveryDate ? formatDateOnly(p.EstimatedDeliveryDate) : '',
      Total: p.Total,
    }));
    rows.push({ Fecha: 'TOTAL', Sucursal: '', Proveedor: '', Estado: '', Entrega_Estimada: '', Total: this.totalAmount() });
    exportToExcel(rows, `reporte_compras_${this.filter.dateInitial}_${this.filter.dateEnd}.xlsx`);
  }
}
