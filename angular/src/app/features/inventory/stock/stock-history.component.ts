import { Location } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { SharedModule } from '../../../shared/shared.module';
import { StockMovementResponse } from './stock-movement.model';
import { StockMovementService } from './stock-movement.service';

/** Historial (kardex) de movimientos de un producto, o de un lote puntual si llega `stockItemId`. */
@Component({
  selector: 'app-stock-history',
  imports: [SharedModule],
  templateUrl: './stock-history.component.html',
})
export class StockHistoryComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  protected readonly location = inject(Location);
  private readonly stockMovement = inject(StockMovementService);

  private readonly productId = this.route.snapshot.paramMap.get('id') ?? '';
  protected readonly productName = this.route.snapshot.queryParamMap.get('name') ?? '';
  protected readonly productCode = this.route.snapshot.queryParamMap.get('code') ?? '';
  protected readonly lot = this.route.snapshot.queryParamMap.get('lot') ?? '';
  protected readonly stockItemId = signal(this.route.snapshot.queryParamMap.get('stockItemId') ?? '');

  protected readonly movements = signal<StockMovementResponse[]>([]);
  protected readonly loading = signal(false);

  ngOnInit() {
    return this.load();
  }

  protected showAll() {
    this.stockItemId.set('');
    return this.load();
  }

  private async load() {
    this.loading.set(true);
    const { Data } = await this.stockMovement.getByProduct(this.productId, this.stockItemId() || undefined);
    this.movements.set(Data ?? []);
    this.loading.set(false);
  }

  protected formatDate(dateStr: string): string {
    const d = new Date(dateStr);
    return d.toLocaleDateString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric' })
      + ' ' + d.toLocaleTimeString('es-BO', { hour: '2-digit', minute: '2-digit', hour12: false });
  }

  protected typeLabel(type: string): string {
    switch (type) {
      case 'VENTA': return 'Venta';
      case 'COMPRA': return 'Compra';
      case 'AJUSTE': return 'Ajuste';
      case 'TRASPASO_SALIDA': return 'Traspaso enviado';
      case 'TRASPASO_ENTRADA': return 'Traspaso recibido';
      default: return type;
    }
  }

  protected movCardBorder(type: string): string {
    switch (type) {
      case 'VENTA': return 'border-danger';
      case 'COMPRA': return 'border-success';
      case 'AJUSTE': return 'border-warning';
      case 'TRASPASO_SALIDA': return 'border-primary';
      case 'TRASPASO_ENTRADA': return 'border-info';
      default: return 'border-secondary';
    }
  }

  protected typeBadge(type: string): string {
    switch (type) {
      case 'VENTA': return 'bg-danger';
      case 'COMPRA': return 'bg-success';
      case 'AJUSTE': return 'bg-warning text-dark';
      case 'TRASPASO_SALIDA': return 'bg-primary';
      case 'TRASPASO_ENTRADA': return 'bg-info text-dark';
      default: return 'bg-secondary';
    }
  }
}
