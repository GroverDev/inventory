import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { firstOfMonthIso, todayIso } from '../../core/utils/date-helper';
import { exportToExcel } from '../../core/utils/excel-helper';
import { SharedModule } from '../../shared/shared.module';
import { PaymentBreakdownComponent } from '../inventory/components/payment-breakdown.component';
import { PaymentMethodTotal, Sale } from '../inventory/sales/sale.model';
import { SaleService } from '../inventory/sales/sale.service';
import { BranchFilterComponent } from './branch-filter.component';

@Component({
  selector: 'app-sales-report',
  imports: [FormsModule, SharedModule, BranchFilterComponent, PaymentBreakdownComponent],
  templateUrl: './sales-report.component.html',
})
export class SalesReportComponent {
  private readonly saleService = inject(SaleService);

  protected readonly sales = signal<Sale[]>([]);
  protected readonly byMethod = signal<PaymentMethodTotal[]>([]);
  protected filter = { dateInitial: firstOfMonthIso(), dateEnd: todayIso() };

  // '' = sucursal activa, un id, o 'all' (consolidado de las sucursales del usuario).
  protected readonly branch = signal('');
  protected readonly consolidated = computed(() => this.branch() === 'all');

  protected readonly totalSubtotal = computed(() => this.sales().reduce((s, v) => s + v.Subtotal, 0));
  protected readonly totalDiscounts = computed(() => this.sales().reduce((s, v) => s + v.TotalDiscounts, 0));
  // Neto de verdad: el total facturado menos lo devuelto.
  protected readonly totalReturned = computed(() => this.sales().reduce((s, v) => s + v.TotalReturned, 0));
  protected readonly totalNet = computed(() => this.sales().reduce((s, v) => s + v.NetTotal, 0));

  protected fmt(v: number): string {
    return v.toLocaleString('es-BO', { style: 'currency', currency: 'BOB' });
  }

  protected fmtDate(v: string | Date): string {
    return new Date(v).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  protected async load() {
    const { ok, Data } = await this.saleService.getAll(this.filter.dateInitial, this.filter.dateEnd, 1, 10000, undefined, this.branch());
    if (ok) {
      this.sales.set(Data?.Items ?? []);
      this.byMethod.set(Data?.PeriodByPaymentMethod ?? []);
    }
  }

  protected exportar() {
    const rows = this.sales().map(s => ({
      Fecha: this.fmtDate(s.SaleDate),
      Sucursal: s.BranchName ?? '',
      Cliente: s.CustomerName,
      MedioPago: s.PaymentMethodsLabel ?? '',
      Subtotal: s.Subtotal,
      Descuentos: s.TotalDiscounts,
      Facturado: s.Total,
      Devuelto: s.TotalReturned,
      Total: s.NetTotal,
    }));
    rows.push({
      Fecha: 'TOTALES', Sucursal: '', Cliente: '', MedioPago: '',
      Subtotal: this.totalSubtotal(), Descuentos: this.totalDiscounts(),
      Facturado: this.totalNet() + this.totalReturned(), Devuelto: this.totalReturned(), Total: this.totalNet(),
    });
    // Al pie, el neto por medio de pago del período: Total por medio, y en
    // Devuelto lo reintegrado con ese medio.
    for (const m of this.byMethod()) {
      rows.push({
        Fecha: '', Sucursal: '', Cliente: '', MedioPago: m.Name, Subtotal: 0, Descuentos: 0,
        Facturado: m.Collected, Devuelto: m.Refunded, Total: m.Net,
      });
    }
    exportToExcel(rows, `reporte_ventas_${this.filter.dateInitial}_${this.filter.dateEnd}.xlsx`);
  }
}
