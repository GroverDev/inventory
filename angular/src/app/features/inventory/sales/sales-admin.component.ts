import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { MsgService } from '../../../core/services/msg.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { toIsoDate } from '../../../core/utils/date-helper';
import { exportToExcel } from '../../../core/utils/excel-helper';
import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { SharedModule } from '../../../shared/shared.module';
import { PaymentBreakdownComponent } from '../components/payment-breakdown.component';
import { PaymentMethodTotal, Sale } from './sale.model';
import { SaleService } from './sale.service';

const PAGE_SIZE = 50;

const getWeekStart = (): string => {
  const d = new Date();
  const day = d.getDay();
  d.setDate(d.getDate() + (day === 0 ? -6 : 1 - day));
  return toIsoDate(d);
};

const getMonthStart = (): string => toIsoDate(new Date(new Date().getFullYear(), new Date().getMonth(), 1));

/** Accesos rápidos de período. */
const QUICK_FILTERS = [
  { key: 'today', label: 'Hoy', icon: 'ri-sun-line' },
  { key: 'week', label: 'Esta semana', icon: 'ri-calendar-2-line' },
  { key: 'month', label: 'Este mes', icon: 'ri-calendar-line' },
] as const;

type QuickKey = (typeof QUICK_FILTERS)[number]['key'];

/** Registro de ventas: filtros, totales del período calculados en el servidor y listado paginado. */
@Component({
  selector: 'app-sales-admin',
  imports: [FormsModule, SharedModule, PaginationComponent, PaymentBreakdownComponent],
  templateUrl: './sales-admin.component.html',
  styles: `
    .kpi-card { background: var(--vz-tertiary-bg, #f8f9fa); border: 1px solid var(--vz-border-color); border-radius: 0.5rem; padding: 0.6rem 0.75rem; text-align: center; }
    .kpi-card--discount { border-color: rgba(var(--vz-danger-rgb), 0.3); background: rgba(var(--vz-danger-rgb), 0.04); }
    .kpi-card--total { border-color: rgba(var(--vz-primary-rgb), 0.35); background: rgba(var(--vz-primary-rgb), 0.06); }
    .kpi-label { font-size: 0.68rem; text-transform: uppercase; letter-spacing: 0.04em; color: var(--vz-secondary-color, #6c757d); margin-bottom: 0.15rem; }
    .kpi-value { font-weight: 700; font-size: 0.9rem; }
    .kpi-sub { font-size: 0.65rem; color: var(--vz-secondary-color, #6c757d); margin-top: 0.1rem; }
  `,
})
export class SalesAdminComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly permissions = inject(PermissionsService);
  private readonly saleService = inject(SaleService);

  protected readonly quickFilters = QUICK_FILTERS;
  private readonly today = toIsoDate(new Date());

  protected readonly sales = signal<Sale[]>([]);
  protected readonly currentPage = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly periodTotals = signal({ Subtotal: 0, Discounts: 0, Total: 0, Returned: 0, Net: 0 });
  protected readonly periodByMethod = signal<PaymentMethodTotal[]>([]);
  protected readonly sellerOptions = signal<string[]>([]);
  protected readonly activeQuick = signal<QuickKey | null>('today');
  protected filter = { dateInitial: this.today, dateEnd: this.today, seller: '' };

  // Eliminar no repone stock ni mueve caja: se reserva para quien tenga el
  // permiso de borrado del formulario, no para todo el que ve las ventas.
  protected readonly canDelete = computed(() => this.permissions.can('sales-admin', 'delete'));

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)));
  protected readonly discountRatePct = computed(() => {
    const t = this.periodTotals();
    return t.Subtotal > 0 ? ((t.Discounts / t.Subtotal) * 100).toFixed(1) : '0.0';
  });

  ngOnInit() {
    return this.applyQuick('today');
  }

  protected applyQuick(key: QuickKey) {
    this.activeQuick.set(key);
    this.filter.seller = '';
    if (key === 'today') this.filter = { ...this.filter, dateInitial: this.today, dateEnd: this.today };
    if (key === 'week') this.filter = { ...this.filter, dateInitial: getWeekStart(), dateEnd: this.today };
    if (key === 'month') this.filter = { ...this.filter, dateInitial: getMonthStart(), dateEnd: this.today };
    return this.load(1);
  }

  // La API manda sale_date en UTC; new Date() lo pasa a la hora local del
  // navegador, que es la del mostrador. Se muestra la hora porque en un día de
  // mucho movimiento la fecha sola no alcanza para ubicar una venta.
  protected formatDate(val: string | Date): string {
    if (!val) return '—';
    return new Date(val).toLocaleString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false });
  }

  protected formatCurrency(val: number): string {
    return (val ?? 0).toLocaleString('es-BO', { style: 'currency', currency: 'BOB' });
  }

  protected async load(page: number) {
    this.currentPage.set(page);
    const { Data } = await this.saleService.getAll(this.filter.dateInitial, this.filter.dateEnd, page, PAGE_SIZE, this.filter.seller || undefined);
    this.sales.set(Data?.Items ?? []);
    this.totalCount.set(Data?.TotalCount ?? 0);
    this.periodTotals.set({
      Subtotal: Data?.PeriodSubtotal ?? 0,
      Discounts: Data?.PeriodDiscounts ?? 0,
      Total: Data?.PeriodTotal ?? 0,
      Returned: Data?.PeriodReturned ?? 0,
      Net: Data?.PeriodNet ?? 0,
    });
    this.periodByMethod.set(Data?.PeriodByPaymentMethod ?? []);
    // Actualiza vendedores disponibles solo cuando no hay filtro activo
    if (!this.filter.seller) {
      this.sellerOptions.set([...new Set(this.sales().map(s => s.SellerName).filter(Boolean))].sort());
    }
  }

  protected onDateChange() {
    this.activeQuick.set(null);
  }

  protected viewDetail(id: string) {
    this.router.navigate(['sale-detail', id]);
  }

  protected async remove(id: string) {
    // El backend además lo valida: eliminar solo se admite sobre una venta ya
    // devuelta por completo, porque no repone stock ni mueve caja.
    const ok = await this.msg.showMessageQuestion(
      'Eliminar saca la venta de los totales, pero NO repone stock ni devuelve el dinero. ' +
      'Para deshacer una venta use Registrar Devolución. ¿Eliminar de todos modos?');
    if (!ok) return;

    const { ok: deleted } = await this.saleService.remove(id);
    if (deleted) {
      await this.msg.showMessageModal({ Description: 'La venta se eliminó correctamente.', MessageType: 'success' });
      await this.load(this.currentPage());
    }
  }

  /** Exporta la página actual y los totales del período. */
  protected exportExcel() {
    const t = this.periodTotals();
    const rows = this.sales().map(s => ({
      Fecha: this.formatDate(s.SaleDate),
      Cliente: s.CustomerName,
      Vendedor: s.SellerName || '',
      Subtotal: s.Subtotal,
      Descuentos: s.TotalDiscounts,
      Facturado: s.Total,
      Devuelto: s.TotalReturned,
      Total: s.NetTotal,
    }));
    rows.push({
      Fecha: 'TOTALES DEL PERÍODO',
      Cliente: '',
      Vendedor: '',
      Subtotal: t.Subtotal,
      Descuentos: t.Discounts,
      Facturado: t.Total,
      Devuelto: t.Returned,
      Total: t.Net,
    });
    exportToExcel(rows, `ventas_${this.filter.dateInitial}_${this.filter.dateEnd}_p${this.currentPage()}.xlsx`);
  }
}
