import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { firstOfMonthIso, todayIso } from '../../core/utils/date-helper';
import { exportToExcel } from '../../core/utils/excel-helper';
import { SharedModule } from '../../shared/shared.module';
import { WriteOffReportResponse } from '../inventory/stock/stock-movement.model';
import { StockMovementService } from '../inventory/stock/stock-movement.service';
import { BranchFilterComponent } from './branch-filter.component';

const emptyReport = (): WriteOffReportResponse => ({
  TotalUnidades: 0, TotalValorPerdido: 0, TotalEventos: 0, PorProducto: [], Detalle: [],
});

@Component({
  selector: 'app-write-off-report',
  imports: [FormsModule, SharedModule, BranchFilterComponent],
  templateUrl: './write-off-report.component.html',
})
export class WriteOffReportComponent {
  private readonly stockMovement = inject(StockMovementService);

  protected readonly reporte = signal<WriteOffReportResponse>(emptyReport());
  protected readonly cargado = signal(false);
  protected filter = { desde: firstOfMonthIso(), hasta: todayIso() };

  // '' = sucursal activa, un id, o 'all' (consolidado de las sucursales del usuario).
  protected readonly branch = signal('');
  protected readonly consolidated = computed(() => this.branch() === 'all');

  protected fmt(v: number): string {
    return v.toLocaleString('es-BO', { style: 'currency', currency: 'BOB' });
  }

  protected fmtDate(v: string): string {
    return new Date(v).toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  protected async load() {
    const { ok, Data } = await this.stockMovement.getWriteOffs(this.filter.desde, this.filter.hasta, undefined, this.branch());
    if (ok) this.reporte.set(Data);
    this.cargado.set(true);
  }

  protected exportar() {
    const rows = this.reporte().Detalle.map(d => ({
      Fecha: this.fmtDate(d.Created),
      Sucursal: d.BranchName,
      Producto: d.ProductName,
      SKU: d.ProductCode,
      Lote: d.LotCode ?? '',
      Cantidad: d.Cantidad,
      Valor: d.ValorPerdido,
      Motivo: d.Reason ?? '',
    }));
    exportToExcel(rows, `reporte_mermas_${this.filter.desde}_${this.filter.hasta}.xlsx`);
  }
}
