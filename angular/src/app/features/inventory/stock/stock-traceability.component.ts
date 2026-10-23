import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { exportToExcel } from '../../../core/utils/excel-helper';
import { formatDateOnly, todayIso } from '../../../core/utils/date-helper';
import { SharedModule } from '../../../shared/shared.module';
import { LotTraceabilityResponse } from './stock-movement.model';
import { StockMovementService } from './stock-movement.service';

/** Trazabilidad de un lote o número de serie: a qué clientes se vendió, con sus datos de contacto. */
@Component({
  selector: 'app-stock-traceability',
  imports: [FormsModule, SharedModule],
  templateUrl: './stock-traceability.component.html',
})
export class StockTraceabilityComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly stockMovement = inject(StockMovementService);

  protected readonly formatDate = formatDateOnly;
  protected lote = '';
  protected readonly loteBuscado = signal('');
  protected readonly ventas = signal<LotTraceabilityResponse[]>([]);
  protected readonly loading = signal(false);
  /** Distingue "todavía no buscaste" de "buscaste y no hay nada". */
  protected readonly buscado = signal(false);

  protected readonly unidades = computed(() => this.ventas().reduce((acc, v) => acc + v.Quantity, 0));

  // Se puede llegar con el lote ya elegido desde la pantalla de vencimientos.
  ngOnInit() {
    const inicial = this.route.snapshot.queryParamMap.get('lote');
    if (inicial) {
      this.lote = inicial;
      return this.buscar();
    }
    return undefined;
  }

  protected async buscar() {
    const lote = this.lote.trim();
    if (!lote) return;

    this.loading.set(true);
    try {
      const { ok, Data } = await this.stockMovement.getTraceability(lote);
      this.ventas.set(ok ? Data : []);
      this.loteBuscado.set(lote);
      this.buscado.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  protected verVenta(v: LotTraceabilityResponse) {
    this.router.navigate(['sale-detail', v.SaleId]);
  }

  /** El listado se exporta para llamar a los clientes uno por uno. */
  protected exportar() {
    const filas = this.ventas().map(v => ({
      Fecha: formatDateOnly(v.SaleDate),
      Cliente: v.Cliente,
      Documento: v.DocumentNumber ?? '',
      Teléfono: v.Cellphone ?? '',
      Sucursal: v.BranchName,
      Cantidad: v.Quantity,
      Producto: v.ProductName,
      Lote: v.LotCode,
      Serie: v.SerialNumber,
    }));
    exportToExcel(filas, `trazabilidad_${this.loteBuscado()}_${todayIso()}.xlsx`);
  }
}
