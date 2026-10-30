import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { MsgService } from '../../../core/services/msg.service';
import { remixIcon } from '../../../core/utils/icon-map';
import { SharedModule } from '../../../shared/shared.module';
import { PaymentMethod, Sale, SaleDetail } from './sale.model';
import { SaleService } from './sale.service';

interface ReturnLine {
  SaleDetailId: string;
  ProductId: string;
  ProductName: string;
  Quantity: number;
  AlreadyReturned: number;
  Available: number;
  /** Precio efectivamente cobrado por unidad (ya con descuentos): es lo que se reembolsa. */
  UnitPrice: number;
  QuantityReturned: number;
}

type EnrichedLine = SaleDetail & { Returned: number; Net: number; LineReturned: number; LineNet: number };

/** Detalle de una venta: importes, cobro, productos con sus devoluciones y registro de devoluciones. */
@Component({
  selector: 'app-sale-detail',
  imports: [FormsModule, SharedModule],
  templateUrl: './sale-detail.component.html',
  styles: `
    /* Producto clicable en el modal de devolución: lo lleva a su máximo. */
    .cursor-pointer { cursor: pointer; }
    .cursor-pointer:hover { text-decoration: underline; }

    .info-label { font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.04em; color: var(--vz-secondary-color, #6c757d); margin-bottom: 0.1rem; }
    .section-title { font-size: 0.72rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.06em; color: var(--vz-secondary-color, #6c757d); border-bottom: 1px solid var(--vz-border-color); padding-bottom: 0.4rem; margin-bottom: 0.75rem; }

    .summary-card { background: var(--vz-tertiary-bg, #f8f9fa); border-radius: 0.5rem; padding: 0.5rem 0.75rem; text-align: center; border: 1px solid var(--vz-border-color); }
    .summary-label { font-size: 0.7rem; text-transform: uppercase; letter-spacing: 0.03em; color: var(--vz-secondary-color, #6c757d); }
    .summary-value { font-weight: 600; font-size: 0.95rem; margin-top: 0.1rem; }
    .summary-card--primary { background: var(--vz-primary); border-color: var(--vz-primary); color: #fff; }
    .summary-card--primary .summary-label { color: rgba(255, 255, 255, 0.8); }
    .summary-card--warning { background: rgba(var(--vz-warning-rgb), 0.12); border-color: rgba(var(--vz-warning-rgb), 0.35); }
    .summary-card--warning .summary-value { color: #856404; }
    .summary-card--success { background: rgba(var(--vz-success-rgb), 0.1); border-color: rgba(var(--vz-success-rgb), 0.35); }
    .summary-card--success .summary-value { color: var(--vz-success); }

    .payment-chip { display: inline-flex; align-items: center; background: var(--vz-tertiary-bg, #f8f9fa); border: 1px solid var(--vz-border-color); border-radius: 2rem; padding: 0.25rem 0.85rem; font-size: 0.85rem; }

    .history-toggle { cursor: pointer; user-select: none; padding: 0.25rem 0; border-radius: 0.25rem; }
    .history-toggle:hover { opacity: 0.8; }
    .return-history-item { border-left: 3px solid var(--vz-warning); padding: 0.4rem 0.75rem; border-radius: 0 0.375rem 0.375rem 0; background: rgba(var(--vz-warning-rgb), 0.06); }
    .return-chip { display: inline-block; font-size: 0.72rem; background: var(--vz-tertiary-bg, #f8f9fa); border: 1px solid var(--vz-border-color); border-radius: 0.25rem; padding: 0.1rem 0.45rem; color: var(--vz-secondary-color, #6c757d); }
  `,
})
export class SaleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly saleService = inject(SaleService);

  protected readonly sale = signal<Sale | null>(null);
  protected readonly showReturnHistory = signal(false);
  protected readonly paymentMethods = signal<PaymentMethod[]>([]);

  // ── Modal de devolución ──
  protected readonly showReturnModal = signal(false);
  protected readonly savingReturn = signal(false);
  protected returnReason = '';
  protected returnPaymentMethodId = '';
  protected returnLines: ReturnLine[] = [];

  protected readonly icon = (fa: string) => remixIcon(fa, 'ri-money-dollar-circle-line');

  // Igual que en el listado: la API manda UTC y se muestra en hora local, con hora y minutos.
  protected formatDate(val: string | Date | null): string {
    if (!val) return '—';
    return new Date(val).toLocaleString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false });
  }

  protected formatCurrency(val: number): string {
    return (val ?? 0).toLocaleString('es-BO', { style: 'currency', currency: 'BOB' });
  }

  protected readonly hasReturns = computed(() => (this.sale()?.Returns?.length ?? 0) > 0);
  protected readonly hasLineDiscounts = computed(() => this.sale()?.Detail.some(d => d.LineTotalDiscounts > 0) ?? false);
  protected readonly totalLineDiscounts = computed(() => this.sale()?.Detail.reduce((s, d) => s + d.LineTotalDiscounts, 0) ?? 0);
  // Colspan del label en tfoot = todas las columnas menos la última (valor)
  protected readonly tableColspan = computed(() => 3 + (this.hasReturns() ? 2 : 0) + (this.hasLineDiscounts() ? 2 : 0));
  protected readonly totalReturned = computed(() => this.sale()?.Returns?.reduce((s, r) => s + r.TotalReturned, 0) ?? 0);
  protected readonly netTotal = computed(() => (this.sale()?.Total ?? 0) - this.totalReturned());

  protected readonly statusLabel = computed(() => {
    if (!this.sale()?.IsActive) return 'Devuelta (total)';
    return this.hasReturns() ? 'Con devolución parcial' : 'Activa';
  });

  protected readonly statusBadgeClass = computed(() => {
    if (!this.sale()?.IsActive) return 'badge bg-danger';
    return this.hasReturns() ? 'badge bg-warning text-dark' : 'badge bg-success';
  });

  protected readonly enrichedLines = computed<EnrichedLine[]>(() => {
    const sale = this.sale();
    if (!sale) return [];
    return sale.Detail.map(d => {
      const returned = (sale.Returns ?? [])
        .flatMap(r => r.Detail)
        .filter(rd => rd.SaleDetailId === d.Id)
        .reduce((s, rd) => s + rd.QuantityReturned, 0);
      const net = d.Quantity - returned;
      return { ...d, Returned: returned, Net: net, LineReturned: returned * d.UnitPrice, LineNet: net * d.UnitPrice };
    });
  });

  protected lineRowClass(line: { Returned: number; Net: number }): string {
    if (line.Net === 0) return 'table-secondary';
    if (line.Returned > 0) return 'table-warning';
    return '';
  }

  // Solo los medios que entran al cajón mueven la caja; el aviso y la validación
  // del servidor (caja abierta) dependen de esto.
  protected get returnAffectsCash(): boolean {
    return this.paymentMethods().find(pm => pm.Id === this.returnPaymentMethodId)?.AffectsCash === true;
  }

  protected get returnTotal(): number {
    return this.returnLines.reduce((s, l) => s + l.QuantityReturned * l.UnitPrice, 0);
  }

  // Todas las líneas ya en su máximo: el botón pasa a limpiar, así el mismo
  // control sirve para deshacer y no queda sin salida.
  protected get allReturnLinesFull(): boolean {
    return this.returnLines.some(l => l.Available > 0) && this.returnLines.every(l => l.QuantityReturned >= l.Available);
  }

  protected get nothingToReturn(): boolean {
    return this.returnLines.every(l => l.Available === 0);
  }

  async ngOnInit() {
    await this.load();
    const { ok, Data } = await this.saleService.getPaymentMethods();
    if (ok) this.paymentMethods.set(Data ?? []);
  }

  private async load() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    const { ok, Data } = await this.saleService.getById(id);
    if (ok && Data) this.sale.set(Data);
  }

  protected back() {
    this.router.navigate(['sales-admin']);
  }

  protected toggleReturnAll() {
    const vaciar = this.allReturnLinesFull;
    this.returnLines.forEach(l => { l.QuantityReturned = vaciar ? 0 : l.Available; });
  }

  protected openReturnModal() {
    const sale = this.sale()!;
    const methods = this.paymentMethods();
    this.returnReason = '';

    // Se precarga el medio con el que se cobró la venta (el de mayor importe si
    // hubo varios): es lo que va a pasar casi siempre, y el cajero solo lo cambia
    // si al cliente se le devuelve por otra vía.
    const cobro = [...(sale.Payments ?? [])].sort((a, b) => b.AmountGiven - a.AmountGiven)[0];
    this.returnPaymentMethodId =
      methods.find(pm => pm.Id === cobro?.PaymentMethodId)?.Id
      ?? methods.find(pm => pm.AffectsCash)?.Id
      ?? methods[0]?.Id
      ?? '';

    this.returnLines = sale.Detail.map(d => {
      const alreadyReturned = (sale.Returns ?? [])
        .flatMap(r => r.Detail)
        .filter(rd => rd.SaleDetailId === d.Id)
        .reduce((s, rd) => s + rd.QuantityReturned, 0);
      return {
        SaleDetailId: d.Id,
        ProductId: d.ProductId,
        ProductName: d.ProductName,
        Quantity: d.Quantity,
        AlreadyReturned: alreadyReturned,
        Available: d.Quantity - alreadyReturned,
        UnitPrice: d.EffectiveUnitPrice,
        QuantityReturned: 0,
      };
    });
    this.showReturnModal.set(true);
  }

  protected closeReturnModal() {
    this.showReturnModal.set(false);
  }

  protected async confirmReturn() {
    const linesToReturn = this.returnLines.filter(l => l.QuantityReturned > 0);
    if (linesToReturn.length === 0) return;

    if (!(await this.msg.showMessageQuestion(`¿Confirmar devolución de ${this.formatCurrency(this.returnTotal)}?`))) return;

    this.savingReturn.set(true);
    try {
      const { ok, Message } = await this.saleService.createReturn({
        SaleId: this.sale()!.Id,
        Reason: this.returnReason || null,
        PaymentMethodId: this.returnPaymentMethodId,
        Detail: linesToReturn.map(l => ({
          SaleDetailId: l.SaleDetailId,
          ProductId: l.ProductId,
          QuantityReturned: l.QuantityReturned,
          UnitPrice: l.UnitPrice,
        })),
      });
      if (ok) {
        this.closeReturnModal();
        await this.msg.showMessageModal({ Description: 'Devolución registrada correctamente.', MessageType: 'success' });
        await this.load();
      } else {
        this.msg.showMessageModal({ Description: Message?.Description || 'No se pudo registrar la devolución.', MessageType: 'error' });
      }
    } finally {
      this.savingReturn.set(false);
    }
  }
}
