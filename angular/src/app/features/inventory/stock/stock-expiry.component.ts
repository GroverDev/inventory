import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { exportToExcel } from '../../../core/utils/excel-helper';
import { formatDateOnly, todayIso } from '../../../core/utils/date-helper';
import { SharedModule } from '../../../shared/shared.module';
import { ExpiryStatus, StockExpiryResponse } from './stock-movement.model';
import { StockMovementService } from './stock-movement.service';

const ESTADOS: { estado: ExpiryStatus; label: string; badge: string; border: string }[] = [
  { estado: 'VENCIDO', label: 'Vencido', badge: 'bg-danger', border: 'border-danger-subtle' },
  { estado: 'CRITICO', label: 'Crítico (30 d)', badge: 'bg-warning text-dark', border: 'border-warning-subtle' },
  { estado: 'PROXIMO', label: 'Próximo (90 d)', badge: 'bg-info', border: 'border-info-subtle' },
  { estado: 'VIGENTE', label: 'Vigente', badge: 'bg-success', border: 'border-success-subtle' },
];

/** Existencias con vencimiento: resumen por urgencia, lista filtrable y accesos a historial, trazabilidad y baja. */
@Component({
  selector: 'app-stock-expiry',
  imports: [FormsModule, SharedModule],
  templateUrl: './stock-expiry.component.html',
})
export class StockExpiryComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly stockMovement = inject(StockMovementService);

  protected readonly formatDate = formatDateOnly;
  protected readonly items = signal<StockExpiryResponse[]>([]);
  protected readonly loading = signal(false);
  protected readonly search = signal('');
  protected readonly estado = signal<'' | ExpiryStatus>('');
  /** Noventa días es el horizonte con que trabaja la vista del servidor por defecto. */
  protected dias = 90;

  protected readonly filtered = computed(() => {
    const texto = this.search().trim().toLowerCase();
    const estado = this.estado();
    return this.items().filter(item => {
      if (estado && item.Estado !== estado) return false;
      if (!texto) return true;
      return item.ProductName.toLowerCase().includes(texto)
        || item.ProductCode.toLowerCase().includes(texto)
        || (item.LotCode ?? '').toLowerCase().includes(texto);
    });
  });

  protected readonly totalFiltrado = computed(() => this.filtered().reduce((acc, item) => acc + item.ValorEnRiesgo, 0));

  /**
   * El resumen se calcula sobre todo lo traído, no sobre lo filtrado: es el marco
   * de referencia, y cambiaría bajo los pies del usuario al escribir en el buscador.
   */
  protected readonly summary = computed(() => ESTADOS.map(e => {
    const grupo = this.items().filter(i => i.Estado === e.estado);
    return {
      ...e,
      count: grupo.length,
      valor: grupo.reduce((acc, i) => acc + i.ValorEnRiesgo, 0),
      unidades: grupo.reduce((acc, i) => acc + i.Quantity, 0),
    };
  }));

  ngOnInit() {
    return this.load();
  }

  protected async load() {
    this.loading.set(true);
    try {
      const { ok, Data } = await this.stockMovement.getExpiring(Number(this.dias));
      this.items.set(ok ? Data : []);
    } finally {
      this.loading.set(false);
    }
  }

  protected formatCurrency(val: number): string {
    return (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  protected estadoLabel(value: ExpiryStatus): string {
    return ESTADOS.find(e => e.estado === value)?.label.replace(/ \(.*\)$/, '') ?? 'Vigente';
  }

  /** Variantes `subtle` + `emphasis`: son las que el tema oscuro redefine. */
  protected estadoBadge(value: ExpiryStatus): string {
    const base = 'badge border';
    if (value === 'VENCIDO') return `${base} bg-danger-subtle text-danger-emphasis border-danger-subtle`;
    if (value === 'CRITICO') return `${base} bg-warning-subtle text-warning-emphasis border-warning-subtle`;
    if (value === 'PROXIMO') return `${base} bg-info-subtle text-info-emphasis border-info-subtle`;
    return `${base} bg-success-subtle text-success-emphasis border-success-subtle`;
  }

  /** «Vencido hace 5 días» dice más que un −5 suelto en una columna. */
  protected diasTexto(item: StockExpiryResponse): string {
    if (item.DiasRestantes < 0) return `hace ${Math.abs(item.DiasRestantes)} d`;
    if (item.DiasRestantes === 0) return 'hoy';
    return `en ${item.DiasRestantes} d`;
  }

  protected goHistory(item: StockExpiryResponse) {
    this.router.navigate(['stock-history', item.ProductId], {
      queryParams: { stockItemId: item.StockItemId, lot: item.LotCode, name: item.ProductName, code: item.ProductCode },
    });
  }

  /** Desde acá se llega con el lote ya cargado; la pantalla también acepta buscarlo. */
  protected goTraceability(item: StockExpiryResponse) {
    this.router.navigate(['stock-traceability'], { queryParams: { lote: item.LotCode } });
  }

  protected goWriteOff(item: StockExpiryResponse) {
    this.router.navigate(['stock-write-off', item.StockItemId], {
      queryParams: {
        productId: item.ProductId, name: item.ProductName, code: item.ProductCode,
        lot: item.LotCode, expiry: item.ExpiryDate, quantity: item.Quantity,
      },
    });
  }

  /** Se exporta lo que el usuario está viendo, con los encabezados de la pantalla. */
  protected exportList() {
    const filas = this.filtered().map(item => ({
      SKU: item.ProductCode,
      Producto: item.ProductName,
      Lote: item.LotCode,
      Vence: formatDateOnly(item.ExpiryDate),
      'Días restantes': item.DiasRestantes,
      Cantidad: item.Quantity,
      'Valor en riesgo': item.ValorEnRiesgo,
      Estado: this.estadoLabel(item.Estado),
    }));
    // Local: con UTC el archivo sale fechado mañana si se exporta de noche.
    exportToExcel(filas, `vencimientos_${todayIso()}.xlsx`);
  }
}
