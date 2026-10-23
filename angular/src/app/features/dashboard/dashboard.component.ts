import { Component, OnInit, inject, signal } from '@angular/core';

import { ResponseObject } from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { SharedModule } from '../../shared/shared.module';

export interface DashboardRecentSale {
  Id: string;
  CustomerName: string;
  SaleDate: string;
  Total: number;
}

export interface DashboardLowStockProduct {
  Id: string;
  ProductName: string;
  ProductCode: string;
  CurrentStock: number;
  MinReorderQuantity: number;
}

export interface DashboardKpi {
  TodaySalesTotal: number;
  TodaySalesCount: number;
  MonthSalesTotal: number;
  MonthSalesCount: number;
  PendingPurchasesCount: number;
  LowStockCount: number;
  RecentSales: DashboardRecentSale[];
  LowStockProducts: DashboardLowStockProduct[];
}

const emptyKpi = (): DashboardKpi => ({
  TodaySalesTotal: 0, TodaySalesCount: 0, MonthSalesTotal: 0, MonthSalesCount: 0,
  PendingPurchasesCount: 0, LowStockCount: 0, RecentSales: [], LowStockProducts: [],
});

/** Tablero de inicio: ventas del día y del mes, compras pendientes, stock crítico y últimas ventas. */
@Component({
  selector: 'app-dashboard',
  imports: [SharedModule],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(ApiService);

  protected readonly kpi = signal<DashboardKpi>(emptyKpi());

  async ngOnInit() {
    const { ok, Data } = await this.api.get<ResponseObject<DashboardKpi>>('Dashboard');
    if (ok) this.kpi.set(Data);
  }

  protected formatNum(value: number): string {
    return value.toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  protected formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('es-BO', { hour: '2-digit', minute: '2-digit', hour12: false });
  }
}
