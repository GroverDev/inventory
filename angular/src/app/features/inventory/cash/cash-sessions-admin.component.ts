import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { remixIcon } from '../../../core/utils/icon-map';
import { toIsoDate, todayIso } from '../../../core/utils/date-helper';
import { exportToExcel } from '../../../core/utils/excel-helper';
import { SharedModule } from '../../../shared/shared.module';
import { PaymentBreakdownComponent } from '../components/payment-breakdown.component';
import { CashSession, MovementTypeLabels, SessionSale } from './cash.model';
import { CashService } from './cash.service';

const QUICK_FILTERS = [
  { key: 'today', label: 'Hoy', icon: 'ri-calendar-event-line' },
  { key: 'week', label: 'Esta semana', icon: 'ri-calendar-2-line' },
  { key: 'month', label: 'Este mes', icon: 'ri-calendar-line' },
];

// Turnos pensados para durar un día; uno abierto por más tiempo suele ser un
// olvido (nadie lo cerró) más que un caso de uso real: se avisa en vez de
// cerrarlo solo, porque un cajero puede legítimamente cruzar medianoche.
const STALE_OPEN_DAYS = 1;

/** Turnos de caja: lista por período y detalle de cada turno (arqueo, ventas y movimientos). */
@Component({
  selector: 'app-cash-sessions-admin',
  imports: [FormsModule, SharedModule, PaymentBreakdownComponent],
  templateUrl: './cash-sessions-admin.component.html',
})
export class CashSessionsAdminComponent implements OnInit {
  private readonly cashService = inject(CashService);

  protected readonly quickFilters = QUICK_FILTERS;
  protected readonly icon = (fa: string) => remixIcon(fa);

  protected readonly sessions = signal<CashSession[]>([]);
  protected readonly selectedSession = signal<CashSession | null>(null);
  protected readonly activeQuick = signal<string | null>('today');
  protected readonly activeTab = signal<'sales' | 'movements'>('sales');
  protected readonly sessionSales = signal<SessionSale[]>([]);
  protected readonly loadingSales = signal(false);
  protected readonly expandedSales = signal<Set<string>>(new Set());
  protected filter = { dateFrom: todayIso(), dateTo: todayIso() };

  /**
   * La API vieja no netea las ventas del turno: TotalReturned y NetTotal llegan en 0 y
   * SaleStatus vacío. Con SaleStatus vacío se toma el facturado como neto, en vez de
   * mostrar Bs. 0,00 en toda la tabla.
   */
  protected readonly salesTotals = computed(() =>
    this.sessionSales().reduce(
      (acc, s) => ({
        subtotal: acc.subtotal + s.Subtotal,
        discounts: acc.discounts + s.TotalDiscounts,
        billed: acc.billed + s.Total,
        returned: acc.returned + this.returnedOf(s),
        net: acc.net + this.netOf(s),
      }),
      { subtotal: 0, discounts: 0, billed: 0, returned: 0, net: 0 },
    ));

  ngOnInit() {
    return this.load();
  }

  private daysAgo(n: number): string {
    const d = new Date();
    d.setDate(d.getDate() - n);
    return toIsoDate(d);
  }

  protected applyQuick(key: string) {
    this.activeQuick.set(key);
    if (key === 'today') this.filter = { dateFrom: todayIso(), dateTo: todayIso() };
    else if (key === 'week') this.filter = { dateFrom: this.daysAgo(6), dateTo: todayIso() };
    else if (key === 'month') this.filter = { dateFrom: this.daysAgo(29), dateTo: todayIso() };
    return this.load();
  }

  protected onDateChange() {
    this.activeQuick.set(null);
  }

  protected async load() {
    const resp = await this.cashService.getAll(this.filter.dateFrom, this.filter.dateTo);
    this.sessions.set(resp.ok ? (resp.Data ?? []) : []);
  }

  protected async selectSession(s: CashSession) {
    this.selectedSession.set(s);
    this.activeTab.set('sales');
    this.expandedSales.set(new Set());
    this.sessionSales.set([]);
    this.loadingSales.set(true);

    const [salesResp, sessionResp] = await Promise.all([this.cashService.getSales(s.Id), this.cashService.getById(s.Id)]);
    this.sessionSales.set(salesResp.ok ? (salesResp.Data ?? []) : []);
    if (sessionResp.ok && sessionResp.Data) this.selectedSession.set(sessionResp.Data);
    this.loadingSales.set(false);
  }

  protected closeDetail() {
    this.selectedSession.set(null);
  }

  protected toggleSale(id: string) {
    const next = new Set(this.expandedSales());
    if (!next.delete(id)) next.add(id);
    this.expandedSales.set(next);
  }

  protected returnedOf(s: SessionSale): number {
    return s.SaleStatus ? s.TotalReturned : 0;
  }

  protected netOf(s: SessionSale): number {
    return s.SaleStatus ? s.NetTotal : s.Total;
  }

  protected formatNum(val: number): string {
    return (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  protected formatDate(val: string): string {
    return new Date(val).toLocaleString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false });
  }

  protected formatDenomination(value: number): string {
    return value >= 1 ? String(value) : value.toFixed(2);
  }

  protected daysOpen(s: CashSession): number {
    return Math.floor((Date.now() - new Date(s.OpenedAt).getTime()) / 86_400_000);
  }

  protected isStaleOpen(s: CashSession): boolean {
    return s.IsOpen && this.daysOpen(s) >= STALE_OPEN_DAYS;
  }

  /**
   * Lo que debería haber en la caja. Es la misma fórmula que aplica el backend al
   * cerrar. En una sesión ya cerrada se muestra el valor guardado, no uno recalculado:
   * el arqueo es un hecho histórico.
   */
  protected expectedAmount(s: CashSession): number {
    return s.ExpectedAmount !== null
      ? s.ExpectedAmount
      : s.OpeningAmount + s.TotalCashSales + s.TotalIncome - s.TotalExpenses - s.TotalWithdrawals - s.TotalReturns;
  }

  protected movementLabel(type: string): string {
    return MovementTypeLabels[type] ?? type;
  }

  protected movementBadge(type: string): string {
    if (type === 'income') return 'bg-success-subtle text-success';
    if (type === 'withdrawal') return 'bg-warning-subtle text-warning';
    return 'bg-danger-subtle text-danger';
  }

  protected exportSessionSales() {
    const session = this.selectedSession();
    if (!session) return;
    const rows: object[] = [];
    for (const sale of this.sessionSales()) {
      for (const d of sale.Detail) {
        rows.push({
          Fecha: this.formatDate(sale.SaleDate),
          Cliente: sale.CustomerName,
          Cajero: sale.SellerName,
          Producto: d.ProductName,
          Cantidad: d.Quantity,
          PrecioUnitario: d.UnitPrice,
          Descuento: d.LineTotalDiscounts,
          Subtotal: d.LineTotal,
          Facturado: sale.Total,
          Devuelto: this.returnedOf(sale),
          TotalVenta: this.netOf(sale),
          MetodoPago: sale.Payments.map(p => p.PaymentMethodName).join(' / '),
        });
      }
    }
    const cajero = session.UserFullName.replace(/\s+/g, '_');
    exportToExcel(rows, `turno_${cajero}_${session.OpenedAt.slice(0, 10)}.xlsx`);
  }
}
