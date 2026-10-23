import { Location } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, ElementRef, HostListener, OnDestroy, OnInit, ViewEncapsulation, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { MsgService } from '../../../core/services/msg.service';
import { apiUrl } from '../../../core/services/api-url';
import { remixIcon } from '../../../core/utils/icon-map';
import { Customer, CustomerCrud } from '../../catalogs/customer.crud';
import { CashCloseSettings, CashCount, CashMovementRequest, CashSession, BOB_DENOMINATIONS } from '../cash/cash.model';
import { CashService } from '../cash/cash.service';
import { ProductInfoModalComponent } from '../components/product-info-modal.component';
import { ProductThumbComponent } from '../components/product-thumb.component';
import { Discount, DiscountService } from '../discounts/discount.service';
import { Product } from '../products/product.model';
import { ProductService } from '../products/product.service';
import { PaymentMethod, SaleDetail, SalePayment, newSale, newSaleDetail, newSalePayment } from '../sales/sale.model';
import { REQUIRES_SUPERVISOR_STOCK, SaleService } from '../sales/sale.service';
import { StockSerialResponse } from '../stock/stock-movement.model';
import { StockMovementService } from '../stock/stock-movement.service';
import { HeldSale, HeldSaleService, PosSettingsService } from './pos.services';

type PendingDiscount = {
  targetIndex: number;
  discountId: string; discountLabel: string; discountType: string;
  discountValue: number; discountAmount: number;
};

/** Lo que se guarda de la venta en curso. `v` permite cambiar el formato sin romper las guardadas. */
interface HeldPayload {
  v: 1;
  customer: Customer | null;
  lines: SaleDetail[];
  header: { id: string; label: string; type: string; value: number };
}

const UNDO_MS = 6000;

const newCustomerModel = (): Customer => ({ Id: '', FullName: '', DocumentNumber: '', Email: '', Cellphone: '', IsActive: true });

/** Punto de venta: catálogo, carrito, descuentos, cobro, caja y ventas en espera. */
@Component({
  selector: 'app-pos',
  imports: [FormsModule, ProductThumbComponent, ProductInfoModalComponent],
  templateUrl: './pos.component.html',
  styleUrl: './pos.component.scss',
  encapsulation: ViewEncapsulation.None,
})
export class PosComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly http = inject(HttpClient);
  private readonly msg = inject(MsgService);
  private readonly auth = inject(AuthService);
  private readonly saleService = inject(SaleService);
  private readonly productService = inject(ProductService);
  private readonly cashService = inject(CashService);
  private readonly customerCrud = inject(CustomerCrud);
  private readonly discountService = inject(DiscountService);
  private readonly posSettingsService = inject(PosSettingsService);
  private readonly heldSaleService = inject(HeldSaleService);
  private readonly stockMovement = inject(StockMovementService);

  protected readonly BOB_DENOMINATIONS = BOB_DENOMINATIONS;
  protected readonly icon = (fa: string) => remixIcon(fa, 'ri-money-dollar-circle-line');

  private readonly productInput = viewChild<ElementRef<HTMLInputElement>>('productInput');
  private readonly amountInput = viewChild<ElementRef<HTMLInputElement>>('amountInput');

  // Topes del descuento manual de la sucursal activa (GET /Settings/pos). El
  // backend los vuelve a aplicar al grabar: acá solo se avisa antes.
  protected maxCashierDiscountPct = 15;
  protected maxCashierDiscountAmount = 50;
  protected maxDiscountPct: number | null = null;
  protected maxDiscountAmount: number | null = null;

  // ── Caja ──
  protected cashSession: CashSession | null = null;
  // null = "sin caja" solo vale una vez terminada la consulta: mientras tanto la
  // pantalla muestra "Verificando caja..." en vez de "Abrir caja".
  protected cashChecking = true;
  protected cashError = false;
  protected showOpenCashModal = false;
  protected showCloseCashModal = false;
  protected showMovementModal = false;
  protected openingAmount = 0;
  // Lo declarado por medio en el cierre, por id de medio. '' / null = sin completar.
  protected declaredCounts: Record<string, number | '' | null> = {};
  /** Resultado del arqueo, para mostrarlo después de cerrar. */
  protected closeResult: CashCount[] = [];
  protected showCloseResult = false;
  protected closeNotes = '';
  protected savingCash = false;

  // Configuración del cierre (se lee al abrir el modal: puede haber cambiado).
  protected closeSettings: CashCloseSettings = { NoteThreshold: 10, SupervisorThreshold: null, MaxAttempts: null, RequireDenominations: false };
  // Conteo del efectivo por billete y moneda, por valor.
  protected showDenominations = false;
  protected denominationQty: Record<number, number | null> = {};

  protected movementType: 'expense' | 'withdrawal' | 'income' = 'expense';
  protected movementAmount = 0;
  protected movementDescription = '';
  protected savingMovement = false;
  protected readonly movementTypes = [
    { value: 'expense' as const, label: 'Gasto', icon: 'ri-receipt-line', activeClass: 'btn-danger' },
    { value: 'withdrawal' as const, label: 'Retiro', icon: 'ri-arrow-up-circle-line', activeClass: 'btn-warning' },
    { value: 'income' as const, label: 'Ingreso', icon: 'ri-arrow-down-circle-line', activeClass: 'btn-success' },
  ];

  // ── Estado general ──
  protected cart: SaleDetail[] = [];
  protected readonly allProducts = signal<Product[]>([]);
  protected readonly loadingProducts = signal(false);
  protected selectedCustomer: Customer | null = null;
  protected customerSearch = '';
  protected customerResults: Customer[] = [];
  protected showNewCustomerModal = false;
  protected newCustomer: Customer = newCustomerModel();
  protected newCustomerError = '';
  protected savingNewCustomer = false;
  protected readonly productSearch = signal('');
  protected readonly selectedCategory = signal('');
  protected activeTab: 'products' | 'cart' = 'products';

  // ── Cobro ──
  protected paymentMethods: PaymentMethod[] = [];
  protected showPaymentModal = false;

  // ── Descuentos ──
  protected discountCatalog: Discount[] = [];
  protected showDiscountModal = false;
  protected discountTargetIndex = -1; // -1 = cabecera, >= 0 = índice de línea en cart
  protected discountMode: 'catalog' | 'manual' = 'catalog';
  protected manualDiscountType: 'Percentage' | 'FixedAmount' = 'Percentage';
  protected manualDiscountValue = 0;
  protected selectedDiscountId = '';
  protected headerDiscountId = '';
  protected headerDiscountLabel = '';
  protected headerDiscountAmount = 0;
  protected headerDiscountType = '';
  protected headerDiscountValue = 0;

  // ── Autorización de supervisor ──
  protected showSupervisorModal = false;
  protected supervisorEmail = '';
  protected supervisorPassword = '';
  protected supervisorError = '';
  protected supervisorLoading = false;
  private pendingDiscount: PendingDiscount | null = null;
  private supervisorAuthToken = '';
  // El mismo modal autoriza un descuento, un cierre de caja o una venta sin stock.
  // La autorización del cierre vale para ese intento: no se reutiliza.
  protected supervisorPurpose: 'discount' | 'close' | 'stock' = 'discount';
  protected supervisorReason = '';
  private closeSupervisorToken = '';

  // ── Venta completada ──
  protected showCompletedModal = false;
  protected completedSaleId = '';
  protected completedCustomer = '';
  protected completedTotal = 0;
  protected completedChange = 0;
  protected completedPayments: SalePayment[] = [];
  protected completedDetail: SaleDetail[] = [];
  protected completedDate = '';
  protected completedTotalLineDiscounts = 0;
  protected completedHeaderDiscountAmount = 0;
  protected savingPayment = false;
  protected selectedMethodId = '';
  protected selectedMethod: PaymentMethod | null = null;
  // null / '' = vacío: el efectivo arranca así, para escribir lo que el cliente entregó.
  protected currentAmount: number | '' | null = '';
  protected paymentLines: SalePayment[] = [];

  // ── Selector de números de serie ──
  // Un producto serializado no se agrega tocándolo: hay que decir QUÉ unidad sale,
  // porque la garantía queda atada a ese número. El +/- del carrito no alcanza.
  protected showSerialModal = false;
  protected serialProduct: Product | null = null;
  protected serialAvailable: StockSerialResponse[] = [];
  protected serialSelected: string[] = [];
  protected serialLoading = false;

  // ── Ficha del producto ──
  protected readonly fichaVisible = signal(false);
  protected readonly productoFicha = signal<Product | null>(null);

  // ── Ventas en espera ──
  protected heldSales: HeldSale[] = [];
  protected showHoldModal = false;
  protected showHeldList = false;
  protected holdLabel = '';
  protected holding = false;

  // ── Deshacer el último producto quitado ──
  protected lastRemoved: { line: SaleDetail; index: number } | null = null;
  private undoTimer: ReturnType<typeof setTimeout> | undefined;

  // ── Computados ──
  protected get isCashier(): boolean {
    return this.auth.user()?.RolName === 'Cajero';
  }

  protected readonly categoryFilters = computed(() => {
    const cats = this.allProducts().map(p => p.CategoryName).filter(c => c && c.trim() !== '');
    return [...new Set(cats)].sort();
  });

  protected readonly filteredProducts = computed(() => {
    let list = this.allProducts().filter(p => p.IsActive);
    const search = this.productSearch().trim();
    if (search) {
      const q = search.toLowerCase();
      list = list.filter(p => p.ProductName.toLowerCase().includes(q));
    }
    const category = this.selectedCategory();
    if (category) list = list.filter(p => p.CategoryName === category);
    return list;
  });

  protected get totalItems(): number {
    return this.cart.reduce((s, l) => s + l.Quantity, 0);
  }

  protected get subtotal(): number {
    return +this.cart.reduce((s, l) => s + l.LineSubtotal, 0).toFixed(2);
  }

  protected get totalLineDiscounts(): number {
    return +this.cart.reduce((s, l) => s + l.LineTotalDiscounts, 0).toFixed(2);
  }

  protected get totalDiscounts(): number {
    return +(this.totalLineDiscounts + this.headerDiscountAmount).toFixed(2);
  }

  protected get total(): number {
    return +(this.subtotal - this.totalLineDiscounts - this.headerDiscountAmount).toFixed(2);
  }

  protected get totalPaid(): number {
    return +this.paymentLines.reduce((s, l) => s + l.AmountGiven, 0).toFixed(2);
  }

  // El vuelto sale solo de los pagos que lo admiten (efectivo): cobrar de más con
  // tarjeta no muestra un vuelto que nadie va a entregar. El servidor lo recalcula igual.
  protected get totalChange(): number {
    return +this.paymentLines.reduce((s, l) => s + (l.AmountReturned ?? 0), 0).toFixed(2);
  }

  protected get discountBaseAmount(): number {
    return this.discountTargetIndex >= 0
      ? (this.cart[this.discountTargetIndex]?.LineSubtotal ?? 0)
      : +(this.subtotal - this.totalLineDiscounts).toFixed(2);
  }

  protected get discountPreview(): number {
    const base = this.discountBaseAmount;
    if (this.discountMode === 'catalog') {
      const d = this.discountCatalog.find(x => x.Id === this.selectedDiscountId);
      if (!d) return 0;
      return d.Type === 'Percentage' ? +Math.min(base * d.Value / 100, base).toFixed(2) : +Math.min(d.Value, base).toFixed(2);
    }
    if (this.manualDiscountValue <= 0) return 0;
    return this.manualDiscountType === 'Percentage'
      ? +Math.min(base * this.manualDiscountValue / 100, base).toFixed(2)
      : +Math.min(this.manualDiscountValue, base).toFixed(2);
  }

  protected get canApplyDiscount(): boolean {
    return this.discountMode === 'catalog' ? !!this.selectedDiscountId : this.manualDiscountValue > 0;
  }

  // ── Cierre de caja ──
  protected get denominationTotal(): number {
    return Math.round(BOB_DENOMINATIONS.reduce((t, v) => t + v * (Number(this.denominationQty[v]) || 0), 0) * 100) / 100;
  }

  /** Mientras se cuenta por billetes, el efectivo declarado es su suma. */
  protected get countingByDenomination(): boolean {
    return this.closeSettings.RequireDenominations || (this.showDenominations && this.denominationTotal > 0);
  }

  protected get cashMethodId(): string {
    return this.paymentMethods.find(m => m.AffectsCash)?.Id ?? '';
  }

  /** Medios que se arquean al cierre: el efectivo siempre, los demás si se configuró. */
  protected get countedMethods(): PaymentMethod[] {
    return this.paymentMethods.filter(m => m.AffectsCash || m.RequiresCount);
  }

  // ── Modal de cobro (computados) ──
  protected get pendingAmount(): number {
    return +Math.max(0, this.total - this.totalPaid).toFixed(2);
  }

  protected get typedAmount(): number {
    return Number(this.currentAmount) || 0;
  }

  protected get methodGivesChange(): boolean {
    return !!this.selectedMethod?.RequiresChanges;
  }

  /** Vuelto de lo que se está escribiendo, antes de agregarlo. */
  protected get liveChange(): number {
    return this.methodGivesChange ? +Math.max(0, this.typedAmount - this.pendingAmount).toFixed(2) : 0;
  }

  protected get liveShortfall(): number {
    return +Math.max(0, this.pendingAmount - this.typedAmount).toFixed(2);
  }

  /** Tarjeta o QR no dan vuelto: no se puede cobrar de más con ellos. */
  protected get exceedsNoChange(): boolean {
    return !!this.selectedMethod && !this.methodGivesChange && this.typedAmount > this.pendingAmount + 0.004;
  }

  protected get canAddPayment(): boolean {
    return !!this.selectedMethod && this.typedAmount > 0 && !this.exceedsNoChange;
  }

  /** Billetes con los que suele pagarse este saldo: el siguiente múltiplo de 10, 50, 100 y 200. */
  protected get quickAmounts(): number[] {
    const p = this.pendingAmount;
    if (p <= 0) return [];
    const candidatos = [10, 50, 100, 200].map(b => Math.ceil(p / b) * b).filter(v => v > p);
    return [...new Set(candidatos)].sort((a, b) => a - b).slice(0, 4);
  }

  /**
   * Qué muestra el panel de cobro. Siempre hay una cifra a la vista: lo que falta
   * cobrar mientras no se cubre el total, y el vuelto cuando se cubre.
   */
  protected get payPanel(): { label: string; amount: number | null; note: string; cls: string } {
    const neutro = 'bg-body-secondary';
    const falta = 'bg-warning-subtle text-warning-emphasis';
    const ok = 'bg-success-subtle text-success-emphasis';

    if (this.exceedsNoChange) {
      return { label: `${this.selectedMethod?.Name} no da vuelto`, amount: null, note: `No puede superar Bs. ${this.formatNum(this.pendingAmount)}`, cls: 'bg-danger-subtle text-danger-emphasis' };
    }
    // Los pagos ya agregados cubren el total.
    if (this.pendingAmount <= 0) {
      return this.totalChange > 0
        ? { label: 'Vuelto a entregar', amount: this.totalChange, note: '', cls: ok }
        : { label: 'Cobro completo', amount: null, note: 'Sin vuelto', cls: ok };
    }
    if (this.typedAmount <= 0) return { label: 'Por cobrar', amount: this.pendingAmount, note: '', cls: neutro };
    if (this.liveShortfall > 0) return { label: 'Faltan', amount: this.liveShortfall, note: '', cls: falta };
    return this.liveChange > 0
      ? { label: 'Vuelto', amount: this.liveChange, note: '', cls: ok }
      : { label: 'Monto exacto', amount: null, note: 'Sin vuelto', cls: ok };
  }

  // ── Ciclo de vida ──
  async ngOnInit() {
    this.loadingProducts.set(true);
    // La caja va aparte y sin esperar al catálogo: es rápida y decide qué se
    // muestra, mientras que los productos pueden tardar.
    this.checkCash();
    const [products, methods, discounts, settings] = await Promise.all([
      this.productService.getByName(''),
      this.saleService.getPaymentMethods(),
      this.discountService.getAll(),
      this.posSettingsService.get(),
    ]);
    this.allProducts.set((products.Data ?? []).filter(p => p.IsActive && p.CurrentStock >= 0));
    this.paymentMethods = methods.Data ?? [];
    this.discountCatalog = (discounts.Data ?? []).filter(d => d.IsActive);
    if (settings.Data) {
      this.maxCashierDiscountPct = settings.Data.MaxCashierDiscountPct;
      this.maxCashierDiscountAmount = settings.Data.MaxCashierDiscountAmount;
      this.maxDiscountPct = settings.Data.MaxDiscountPct ?? null;
      this.maxDiscountAmount = settings.Data.MaxDiscountAmount ?? null;
    }
    this.loadingProducts.set(false);
    this.loadHeldSales();
    this.focusProductSearch();
    this.loadDefaultCustomer();
  }

  ngOnDestroy() {
    this.clearUndo();
  }

  // Cerrar la pestaña, recargar o cambiar de sucursal (recarga la página): el
  // navegador muestra su propio aviso; no permite un mensaje personalizado.
  @HostListener('window:beforeunload', ['$event'])
  protected warnBeforeUnload(e: BeforeUnloadEvent) {
    if (this.cart.length === 0) return;
    e.preventDefault();
    e.returnValue = '';
  }

  private focusProductSearch() {
    setTimeout(() => this.productInput()?.nativeElement.focus());
  }

  protected formatNum(val: number): string {
    return (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  protected formatHeldTime(value: string): string {
    return new Date(value).toLocaleTimeString('es-BO', { hour: '2-digit', minute: '2-digit', hour12: false });
  }

  protected cartQty(productId: string): number {
    return this.cart.find(l => l.ProductId === productId)?.Quantity ?? 0;
  }

  private recalcLine(i: number) {
    const l = this.cart[i];
    l.LineSubtotal = +(l.Quantity * l.UnitPrice).toFixed(2);
    if (l.DiscountType === 'Percentage' && l.DiscountValue > 0) {
      l.LineTotalDiscounts = +Math.min(l.LineSubtotal * l.DiscountValue / 100, l.LineSubtotal).toFixed(2);
    } else if (l.DiscountType === 'FixedAmount' && l.DiscountValue > 0) {
      l.LineTotalDiscounts = +Math.min(l.DiscountValue, l.LineSubtotal).toFixed(2);
    } else {
      l.LineTotalDiscounts = 0;
    }
    l.LineTotal = +(l.LineSubtotal - l.LineTotalDiscounts).toFixed(2);
  }

  // ── Caja ──
  // Consulta la caja abierta. Si la consulta falla NO se da por cerrada: se marca
  // el error y se ofrece reintentar, para que un fallo de red no lleve a abrir una
  // segunda caja. Solo se ofrece abrir caja cuando el servidor confirma que no hay ninguna.
  protected async checkCash() {
    this.cashChecking = true;
    this.cashError = false;
    const resp = await this.cashService.getActive();
    if (resp.ok) {
      this.cashSession = resp.Data ?? null;
    } else {
      this.cashSession = null;
      this.cashError = true;
    }
    this.cashChecking = false;
    if (!this.cashSession && !this.cashError) this.showOpenCashModal = true;
  }

  protected async doOpenCash() {
    this.savingCash = true;
    try {
      const resp = await this.cashService.open({ OpeningAmount: this.openingAmount });
      if (resp.ok) {
        const sessionResp = await this.cashService.getActive();
        this.cashSession = sessionResp.Data ?? null;
        this.showOpenCashModal = false;
        this.openingAmount = 0;
      } else {
        this.msg.showMessageModal({ Description: resp.Message?.Description || 'No se pudo abrir la caja.', MessageType: 'warning' });
      }
    } finally {
      this.savingCash = false;
    }
  }

  protected async openCloseCash() {
    this.showCloseCashModal = true;
    const { ok, Data } = await this.cashService.getCloseSettings();
    if (ok && Data) this.closeSettings = Data;
  }

  /** De dónde sale lo que se declara en cada medio. */
  protected countHint(m: PaymentMethod): string {
    return m.AffectsCash ? 'lo contado en el cajón, con el fondo inicial'
      : /tarjeta|pos|dat[aá]fono/i.test(m.Name) ? 'total del cierre de lote del datáfono'
        : 'lo recibido en la cuenta';
  }

  protected syncCashFromDenominations() {
    if (this.cashMethodId) this.declaredCounts[this.cashMethodId] = this.denominationTotal;
  }

  protected formatDenomination(v: number): string {
    return v >= 1 ? String(v) : v.toFixed(2);
  }

  protected openHeldFromClose() {
    this.showCloseCashModal = false;
    this.openHeldList();
  }

  protected async doCloseCash() {
    if (!this.cashSession) return;
    const faltan = this.countedMethods.filter(m => {
      const v = this.declaredCounts[m.Id];
      return v === '' || v === undefined || v === null;
    });
    if (faltan.length) {
      this.msg.showMessageModal({ Description: `Declare ${faltan.map(m => m.Name).join(', ')} (0 si no hubo).`, MessageType: 'warning' });
      return;
    }
    this.savingCash = true;
    try {
      const usaBilletes = this.countingByDenomination;
      const resp = await this.cashService.close(this.cashSession.Id, {
        DeclaredAmount: 0,
        Notes: this.closeNotes,
        Counts: this.countedMethods.map(m => ({ PaymentMethodId: m.Id, Declared: Number(this.declaredCounts[m.Id]) })),
        Denominations: usaBilletes
          ? BOB_DENOMINATIONS.map(v => ({ Value: v, Quantity: Number(this.denominationQty[v]) || 0 })).filter(d => d.Quantity > 0)
          : [],
        SupervisorAuthToken: this.closeSupervisorToken || undefined,
      });
      // La autorización valió para este intento y nada más.
      this.closeSupervisorToken = '';
      if (resp.ok) {
        this.cashSession = null;
        this.showCloseCashModal = false;
        this.declaredCounts = {};
        this.denominationQty = {};
        this.showDenominations = false;
        this.closeNotes = '';
        // Recién ahora se ve el resultado: esperado, declarado y diferencia.
        this.closeResult = resp.Data?.Counts ?? [];
        this.showCloseResult = true;
      } else if (resp.Data?.CloseRequires === 'supervisor' && this.closeNotes.trim()) {
        // La observación ya está: falta la firma. ApiService ya mostró el motivo.
        this.openSupervisorModal('close', resp.Message?.Description ?? '');
      }
      // Cualquier otro rechazo (falta la observación, billetes que no suman) ya lo
      // mostró ApiService: el cajero corrige en el mismo modal y vuelve a intentar.
    } finally {
      this.savingCash = false;
    }
  }

  protected async doAddMovement() {
    if (!this.cashSession) return;
    if (!this.movementDescription.trim()) {
      this.msg.showMessageModal({ Description: 'La descripción es obligatoria.', MessageType: 'warning' });
      return;
    }
    if (this.movementAmount <= 0) {
      this.msg.showMessageModal({ Description: 'El monto debe ser mayor a cero.', MessageType: 'warning' });
      return;
    }
    this.savingMovement = true;
    try {
      const request: CashMovementRequest = {
        CashSessionId: this.cashSession.Id,
        MovementType: this.movementType,
        Amount: this.movementAmount,
        Description: this.movementDescription.trim(),
      };
      const resp = await this.cashService.addMovement(this.cashSession.Id, request);
      if (resp.ok) {
        // Recargar sesión para actualizar totales
        const sessionResp = await this.cashService.getActive();
        this.cashSession = sessionResp.Data ?? null;
        this.showMovementModal = false;
        this.movementAmount = 0;
        this.movementDescription = '';
        this.movementType = 'expense';
      } else {
        this.msg.showMessageModal({ Description: resp.Message?.Description || 'No se pudo registrar el movimiento.', MessageType: 'warning' });
      }
    } finally {
      this.savingMovement = false;
    }
  }

  // ── Clientes ──
  protected async searchCustomers() {
    const term = this.customerSearch.trim();
    if (!term) return;
    const { Data } = await this.customerCrud.list(term);
    this.customerResults = Data ?? [];
  }

  protected selectCustomer(c: Customer) {
    this.selectedCustomer = c;
    this.customerResults = [];
    this.customerSearch = '';
  }

  protected clearCustomer() {
    this.selectedCustomer = null;
    this.customerSearch = '';
    this.customerResults = [];
  }

  // Precarga el cliente genérico del tenant para que cobrar nunca quede bloqueado
  // por falta de cliente. Si falla (sin red, por ejemplo), no rompe nada.
  private async loadDefaultCustomer() {
    const { ok, Data } = await this.customerCrud.getDefault();
    if (ok && Data) this.selectedCustomer = Data;
  }

  protected openNewCustomerModal() {
    this.newCustomer = { ...newCustomerModel(), FullName: this.customerSearch.trim() };
    this.newCustomerError = '';
    this.showNewCustomerModal = true;
  }

  protected async saveNewCustomer() {
    this.newCustomerError = '';
    this.savingNewCustomer = true;
    try {
      const { ok, Data: id, Message } = await this.customerCrud.create(this.newCustomer);
      if (ok && id) {
        this.selectCustomer({ ...this.newCustomer, Id: id });
        this.showNewCustomerModal = false;
      } else {
        this.newCustomerError = Message?.Description || 'No se pudo crear el cliente.';
      }
    } finally {
      this.savingNewCustomer = false;
    }
  }

  // ── Ficha del producto ──
  /**
   * La ficha trae el precio y el stock de este momento. Si la tarjeta quedó con
   * otros (cambió el precio, vendió otra caja), se corrige acá para que grilla y
   * ficha no se contradigan. El carrito no se toca: al cobrar, el servidor
   * revalida el precio vigente de cada línea.
   */
  protected actualizarProducto(event: { productId: string; datos: { SalePrice: number; CurrentStock: number } }) {
    this.allProducts.update(list => list.map(p =>
      p.Id === event.productId ? { ...p, SalePrice: event.datos.SalePrice, CurrentStock: event.datos.CurrentStock } : p));
  }

  protected abrirFicha(prod: Product) {
    this.productoFicha.set(prod);
    this.fichaVisible.set(true);
  }

  /**
   * Agregar desde la ficha, que puede ser el producto mismo o una alternativa. Se
   * cierra el modal: quien eligió la alternativa ya decidió.
   */
  protected agregarDesdeFicha(productId: string) {
    const prod = this.allProducts().find(p => p.Id === productId);
    if (prod) this.addToCart(prod);
    this.fichaVisible.set(false);
  }

  // ── Carrito ──
  protected usesSerial(prod: Product): boolean {
    return prod.TrackingMode === 'serial';
  }

  protected addToCart(prod: Product) {
    if (this.usesSerial(prod)) { this.openSerialPicker(prod); return; }

    const idx = this.cart.findIndex(l => l.ProductId === prod.Id);
    if (idx >= 0) {
      this.cart[idx].Quantity++;
      this.recalcLine(idx);
    } else {
      const line = newSaleDetail();
      line.ProductId = prod.Id;
      line.ProductName = prod.ProductName;
      line.UnitPrice = prod.SalePrice;
      line.Quantity = 1;
      line.LineTotalDiscounts = 0;
      line.LineSubtotal = +prod.SalePrice.toFixed(2);
      line.LineTotal = line.LineSubtotal;
      this.cart.push(line);
    }
  }

  /** Una línea serializada no se ajusta con +/-: hay que decir qué unidad entra o sale. */
  private esLineaSerializada(l: SaleDetail): boolean {
    return (l.SerialNumbers?.length ?? 0) > 0;
  }

  private reabrirSelector(l: SaleDetail) {
    const prod = this.allProducts().find(p => p.Id === l.ProductId);
    if (prod) this.openSerialPicker(prod);
  }

  protected increaseQty(i: number) {
    if (this.esLineaSerializada(this.cart[i])) { this.reabrirSelector(this.cart[i]); return; }
    this.cart[i].Quantity++;
    this.recalcLine(i);
  }

  protected decreaseQty(i: number) {
    if (this.esLineaSerializada(this.cart[i])) { this.reabrirSelector(this.cart[i]); return; }
    if (this.cart[i].Quantity <= 1) {
      this.removeWithUndo(i);
    } else {
      this.cart[i].Quantity--;
      this.recalcLine(i);
    }
  }

  protected decreaseQtyByProduct(productId: string) {
    const idx = this.cart.findIndex(l => l.ProductId === productId);
    if (idx >= 0) this.decreaseQty(idx);
  }

  protected removeFromCart(i: number) {
    this.removeWithUndo(i);
  }

  // En una tablet es fácil tocar la X en vez del %: quitar no pide confirmación
  // (frenaría cada venta), pero deja unos segundos para deshacerlo.
  private clearUndo() {
    clearTimeout(this.undoTimer);
    this.lastRemoved = null;
  }

  private removeWithUndo(i: number) {
    const [line] = this.cart.splice(i, 1);
    if (!line) return;
    clearTimeout(this.undoTimer);
    this.lastRemoved = { line, index: i };
    this.undoTimer = setTimeout(() => this.clearUndo(), UNDO_MS);
  }

  protected undoRemove() {
    const removed = this.lastRemoved;
    this.clearUndo();
    if (!removed) return;
    // Si mientras tanto volvió a agregar el mismo producto, restaurar duplicaría la línea.
    if (this.cart.some(l => l.ProductId === removed.line.ProductId)) return;
    this.cart.splice(Math.min(removed.index, this.cart.length), 0, removed.line);
  }

  // ── Selector de series ──
  protected async openSerialPicker(prod: Product) {
    this.serialProduct = prod;
    this.serialSelected = [...(this.cart.find(l => l.ProductId === prod.Id)?.SerialNumbers ?? [])];
    this.serialAvailable = [];
    this.showSerialModal = true;
    this.serialLoading = true;
    try {
      const { ok, Data } = await this.stockMovement.getAvailableSerials(prod.Id);
      this.serialAvailable = ok ? Data : [];
    } finally {
      this.serialLoading = false;
    }
  }

  protected toggleSerial(serie: string) {
    const i = this.serialSelected.indexOf(serie);
    if (i >= 0) this.serialSelected.splice(i, 1);
    else this.serialSelected.push(serie);
  }

  /** La cantidad la determina cuántas unidades se eligieron: no hay forma de vender 3 aparatos indicando 2 números. */
  protected confirmSerials() {
    const prod = this.serialProduct;
    if (!prod) return;

    const idx = this.cart.findIndex(l => l.ProductId === prod.Id);
    if (this.serialSelected.length === 0) {
      if (idx >= 0) this.cart.splice(idx, 1);
      this.showSerialModal = false;
      return;
    }

    if (idx >= 0) {
      this.cart[idx].SerialNumbers = [...this.serialSelected];
      this.cart[idx].Quantity = this.serialSelected.length;
      this.recalcLine(idx);
    } else {
      const line = newSaleDetail();
      line.ProductId = prod.Id;
      line.ProductName = prod.ProductName;
      line.UnitPrice = prod.SalePrice;
      line.Quantity = this.serialSelected.length;
      line.SerialNumbers = [...this.serialSelected];
      line.LineTotalDiscounts = 0;
      line.LineSubtotal = +(prod.SalePrice * line.Quantity).toFixed(2);
      line.LineTotal = line.LineSubtotal;
      this.cart.push(line);
    }
    this.showSerialModal = false;
  }

  protected formatSerialExpiry(date: string): string {
    return new Date(date).toLocaleDateString('es-BO');
  }

  // ── Cobro ──
  private focusAmount() {
    setTimeout(() => this.amountInput()?.nativeElement.focus());
  }

  /** El efectivo es el cobro más común: se deja elegido para escribir directo. */
  private selectCashIfPending() {
    const efectivo = this.paymentMethods.find(m => m.RequiresChanges);
    if (efectivo && this.pendingAmount > 0) this.selectMethod(efectivo);
  }

  protected selectMethod(m: PaymentMethod) {
    this.selectedMethodId = m.Id;
    this.selectedMethod = m;
    // Efectivo: vacío, para escribir lo recibido y ver el vuelto. Tarjeta o QR: el saldo, porque se cobra exacto.
    this.currentAmount = m.RequiresChanges ? '' : this.pendingAmount;
    this.focusAmount();
  }

  protected setAmount(v: number) {
    this.currentAmount = v;
    this.focusAmount();
  }

  protected addPaymentLine() {
    if (!this.canAddPayment || !this.selectedMethod) return;
    const m = this.selectedMethod;
    const line = newSalePayment();
    line.PaymentMethodId = m.Id;
    line.PaymentMethodName = m.Name;
    line.IconCss = m.IconCss;
    line.AmountGiven = +this.typedAmount.toFixed(2);
    line.AmountReturned = this.liveChange;
    this.paymentLines.push(line);
    this.selectedMethodId = '';
    this.selectedMethod = null;
    this.currentAmount = '';
    // Pago mixto: si queda saldo (pagó una parte con tarjeta), el resto normalmente va en efectivo.
    this.selectCashIfPending();
  }

  /** Enter: agrega el pago y, si con eso se cubre el total, confirma la venta. */
  protected onAmountEnter() {
    if (!this.canAddPayment) return;
    this.addPaymentLine();
    if (this.totalPaid >= this.total && !this.savingPayment) this.finalizeSale();
  }

  protected removePaymentLine(i: number) {
    this.paymentLines.splice(i, 1);
  }

  private openPaymentModal() {
    this.paymentLines = [];
    this.selectedMethodId = '';
    this.selectedMethod = null;
    this.currentAmount = '';
    this.showPaymentModal = true;
    this.selectCashIfPending();
  }

  protected closePaymentModal() {
    this.showPaymentModal = false;
  }

  // ── Descuentos ──
  protected openDiscountModal(targetIndex: number) {
    this.discountTargetIndex = targetIndex;
    if (targetIndex >= 0) {
      const line = this.cart[targetIndex];
      if (line.DiscountId) {
        this.discountMode = 'catalog';
        this.selectedDiscountId = line.DiscountId;
      } else if (line.DiscountType) {
        this.discountMode = 'manual';
        this.manualDiscountType = line.DiscountType as 'Percentage' | 'FixedAmount';
        this.manualDiscountValue = line.DiscountValue;
        this.selectedDiscountId = '';
      } else {
        this.discountMode = 'catalog';
        this.selectedDiscountId = '';
        this.manualDiscountValue = 0;
      }
    } else {
      this.discountMode = 'catalog';
      this.selectedDiscountId = this.headerDiscountId;
      this.manualDiscountValue = 0;
    }
    this.showDiscountModal = true;
  }

  private buildDiscountPayload(): PendingDiscount | null {
    const base = this.discountBaseAmount;
    let discountId = '', discountLabel = '', discountType = '', discountValue = 0, discountAmount = 0;

    if (this.discountMode === 'catalog') {
      const d = this.discountCatalog.find(x => x.Id === this.selectedDiscountId);
      if (!d) return null;
      discountId = d.Id;
      discountType = d.Type;
      discountValue = d.Value;
      discountAmount = d.Type === 'Percentage' ? +Math.min(base * d.Value / 100, base).toFixed(2) : +Math.min(d.Value, base).toFixed(2);
      discountLabel = `${d.Name} (${d.Type === 'Percentage' ? d.Value + '%' : 'Bs. ' + this.formatNum(d.Value)})`;
    } else {
      discountType = this.manualDiscountType;
      discountValue = this.manualDiscountValue;
      discountAmount = this.manualDiscountType === 'Percentage'
        ? +Math.min(base * discountValue / 100, base).toFixed(2)
        : +Math.min(discountValue, base).toFixed(2);
      discountLabel = this.manualDiscountType === 'Percentage' ? `Manual ${discountValue}%` : `Manual Bs. ${this.formatNum(discountValue)}`;
    }
    return { targetIndex: this.discountTargetIndex, discountId, discountLabel, discountType, discountValue, discountAmount };
  }

  private commitDiscount(payload: PendingDiscount) {
    if (payload.targetIndex >= 0) {
      const line = this.cart[payload.targetIndex];
      line.DiscountId = payload.discountId;
      line.DiscountLabel = payload.discountLabel;
      line.DiscountType = payload.discountType;
      line.DiscountValue = payload.discountValue;
      line.LineTotalDiscounts = payload.discountAmount;
      this.recalcLine(payload.targetIndex);
    } else {
      this.headerDiscountId = payload.discountId;
      this.headerDiscountLabel = payload.discountLabel;
      this.headerDiscountAmount = payload.discountAmount;
      this.headerDiscountType = payload.discountType;
      this.headerDiscountValue = payload.discountValue;
    }
  }

  private requiresSupervisorAuth(payload: PendingDiscount): boolean {
    if (!this.isCashier) return false;
    if (this.discountMode === 'catalog') return false;
    if (payload.discountType === 'Percentage') return payload.discountValue > this.maxCashierDiscountPct;
    if (payload.discountType === 'FixedAmount') return payload.discountValue > this.maxCashierDiscountAmount;
    return false;
  }

  /** Mensaje si el descuento manual supera el tope máximo, que nadie puede autorizar. */
  private exceedsMaxDiscount(payload: PendingDiscount): string {
    if (this.discountMode === 'catalog') return '';
    if (payload.discountType === 'Percentage' && this.maxDiscountPct !== null && payload.discountValue > this.maxDiscountPct) {
      return `El descuento supera el tope máximo del ${this.maxDiscountPct}% de esta sucursal.`;
    }
    if (payload.discountType === 'FixedAmount' && this.maxDiscountAmount !== null && payload.discountValue > this.maxDiscountAmount) {
      return `El descuento supera el tope máximo de Bs. ${this.maxDiscountAmount} de esta sucursal.`;
    }
    return '';
  }

  protected applyDiscount() {
    const payload = this.buildDiscountPayload();
    if (!payload) return;

    const tope = this.exceedsMaxDiscount(payload);
    if (tope) {
      this.msg.showMessageModal({ Description: tope, MessageType: 'warning' });
      return;
    }

    if (this.requiresSupervisorAuth(payload)) {
      this.pendingDiscount = payload;
      this.showDiscountModal = false;
      this.openSupervisorModal('discount', '');
      return;
    }

    this.commitDiscount(payload);
    this.showDiscountModal = false;
  }

  protected removeLineDiscount(i: number) {
    const line = this.cart[i];
    line.DiscountId = '';
    line.DiscountLabel = '';
    line.DiscountType = '';
    line.DiscountValue = 0;
    line.LineTotalDiscounts = 0;
    this.recalcLine(i);
  }

  protected removeHeaderDiscount() {
    this.headerDiscountId = '';
    this.headerDiscountLabel = '';
    this.headerDiscountAmount = 0;
    this.headerDiscountType = '';
    this.headerDiscountValue = 0;
  }

  // ── Supervisor ──
  private openSupervisorModal(purpose: 'discount' | 'close' | 'stock', reason: string) {
    this.supervisorPurpose = purpose;
    this.supervisorReason = reason;
    this.supervisorEmail = '';
    this.supervisorPassword = '';
    this.supervisorError = '';
    this.showSupervisorModal = true;
  }

  protected async verifySupervisor() {
    if (!this.supervisorEmail || !this.supervisorPassword) {
      this.supervisorError = 'Ingresa email y contraseña del supervisor.';
      return;
    }
    this.supervisorLoading = true;
    this.supervisorError = '';
    try {
      // Igual que en el Vue: va directo al API, sin pantalla de carga ni modal de error propios.
      const data = await firstValueFrom(this.http.post<{ ok: boolean; Data?: { Token?: string; RolName?: string } }>(apiUrl('Login'), {
        UserName: '',
        Email: this.supervisorEmail,
        Password: this.supervisorPassword,
        Device: '',
        WithEmail: true,
        LoginFrom: 5,
        LoginWith: 1,
      }));
      if (!data?.ok || !data?.Data?.Token) {
        this.supervisorError = 'Credenciales incorrectas.';
        return;
      }
      if (data.Data.RolName === 'Cajero') {
        this.supervisorError = 'El usuario ingresado no tiene permisos de supervisor.';
        return;
      }
      if (this.supervisorPurpose === 'close') {
        // Se reintenta el cierre con la firma; si el servidor la rechaza, lo dice.
        this.closeSupervisorToken = data.Data.Token;
        this.showSupervisorModal = false;
        await this.doCloseCash();
        return;
      }
      if (this.supervisorPurpose === 'stock') {
        // Con la firma se reintenta el cobro; el servidor la valida por su cuenta.
        this.supervisorAuthToken = data.Data.Token;
        this.showSupervisorModal = false;
        await this.finalizeSale();
        return;
      }
      // Guardar token del supervisor para enviarlo al backend al grabar la venta
      this.supervisorAuthToken = data.Data.Token;
      // Autorizado: aplicar descuento pendiente
      if (this.pendingDiscount) this.commitDiscount(this.pendingDiscount);
      this.pendingDiscount = null;
      this.showSupervisorModal = false;
    } catch {
      this.supervisorError = 'Error al verificar las credenciales. Intenta nuevamente.';
    } finally {
      this.supervisorLoading = false;
    }
  }

  // ── Cobrar ──
  protected confirmSale() {
    if (!this.cashSession) {
      this.msg.showMessageModal({ Description: 'Debes abrir la caja antes de realizar ventas.', MessageType: 'warning' });
      this.showOpenCashModal = true;
      return;
    }
    if (!this.selectedCustomer) {
      this.msg.showMessageModal({ Description: 'Selecciona un cliente antes de cobrar.', MessageType: 'warning' });
      return;
    }
    if (this.cart.length === 0) {
      this.msg.showMessageModal({ Description: 'El carrito está vacío.', MessageType: 'warning' });
      return;
    }
    this.openPaymentModal();
  }

  protected async finalizeSale() {
    this.savingPayment = true;
    try {
      const sale = newSale();
      sale.CustomerId = this.selectedCustomer!.Id;
      sale.SaleDate = new Date().toISOString();
      sale.IsActive = true;
      sale.CashSessionId = this.cashSession?.Id ?? '';
      sale.Subtotal = this.subtotal;
      sale.TotalDiscounts = this.totalDiscounts;
      sale.Total = this.total;
      sale.HeaderDiscountId = this.headerDiscountId;
      sale.HeaderDiscountAmount = this.headerDiscountAmount;
      sale.HeaderDiscountType = this.headerDiscountType;
      sale.HeaderDiscountValue = this.headerDiscountValue;
      sale.SupervisorAuthToken = this.supervisorAuthToken;
      // Este detalle se arma campo por campo: lo que no se nombre acá no viaja,
      // por más que el modelo y el carrito lo tengan.
      sale.Detail = this.cart.map(l => {
        const d = newSaleDetail();
        d.ProductId = l.ProductId;
        d.Quantity = l.Quantity;
        d.UnitPrice = l.UnitPrice;
        d.LineSubtotal = l.LineSubtotal;
        d.LineTotalDiscounts = l.LineTotalDiscounts;
        d.LineTotal = l.LineTotal;
        d.DiscountId = l.DiscountId;
        d.DiscountType = l.DiscountType;
        d.DiscountValue = l.DiscountValue;
        d.SerialNumbers = l.SerialNumbers ?? [];
        return d;
      });
      sale.Payments = this.paymentLines.map(l => {
        const p = newSalePayment();
        p.PaymentMethodId = l.PaymentMethodId;
        p.PaymentMethodName = l.PaymentMethodName;
        p.AmountGiven = l.AmountGiven;
        p.AmountReturned = l.AmountReturned;
        return p;
      });

      const { ok: saved, Message, Data: newSaleId } = await this.saleService.save(sale);
      if (saved) {
        // Recargar sesión para que TotalSales refleje la venta recién registrada
        const sessionResp = await this.cashService.getActive();
        this.cashSession = sessionResp.Data ?? null;
        // Capturar datos antes de resetear
        this.completedSaleId = newSaleId ?? '';
        this.completedCustomer = this.selectedCustomer?.FullName ?? '';
        this.completedTotal = this.total;
        this.completedChange = this.totalChange;
        this.completedPayments = [...this.paymentLines];
        this.completedDetail = [...this.cart];
        this.completedTotalLineDiscounts = this.totalLineDiscounts;
        this.completedHeaderDiscountAmount = this.headerDiscountAmount;
        this.completedDate = new Date().toLocaleString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false });
        this.closePaymentModal();
        this.resetAll();
        this.showCompletedModal = true;
      } else if (Message?.Id === REQUIRES_SUPERVISOR_STOCK) {
        // Vender de más no es un error: falta la firma de un supervisor. La venta no
        // se grabó, así que la pantalla queda como está y se reintenta con la firma.
        this.openSupervisorModal('stock', Message.Description);
      } else {
        this.msg.showMessageModal({ Description: Message?.Description || 'No se pudo registrar la venta.', MessageType: 'error' });
      }
    } finally {
      this.savingPayment = false;
    }
  }

  // ── Ventas en espera ──
  // Se guardan en el servidor, por sucursal: cualquier caja las retoma. No reservan
  // stock ni precios: al retomar se actualizan precios y descuentos con lo vigente.
  private async loadHeldSales() {
    const { ok, Data } = await this.heldSaleService.getAll();
    if (ok) this.heldSales = Data;
  }

  protected openHoldModal() {
    if (this.cart.length === 0) return;
    this.holdLabel = '';
    this.showHoldModal = true;
  }

  protected async holdCurrentSale() {
    const payload: HeldPayload = {
      v: 1,
      customer: this.selectedCustomer,
      lines: this.cart,
      header: { id: this.headerDiscountId, label: this.headerDiscountLabel, type: this.headerDiscountType, value: this.headerDiscountValue },
    };
    this.holding = true;
    const { ok } = await this.heldSaleService.hold(this.holdLabel.trim(), this.selectedCustomer?.Id ?? null, this.totalItems, this.total, payload);
    this.holding = false;
    if (!ok) return;

    this.showHoldModal = false;
    this.resetAll();
    await this.loadHeldSales();
  }

  protected async openHeldList() {
    await this.loadHeldSales();
    this.showHeldList = true;
  }

  /** Importe de un descuento sobre una base, igual que al aplicarlo. */
  private discountOn(type: string, value: number, base: number): number {
    return type === 'Percentage' ? +Math.min(base * value / 100, base).toFixed(2)
      : type === 'FixedAmount' ? +Math.min(value, base).toFixed(2) : 0;
  }

  /**
   * Motivo por el que un descuento guardado ya no se puede aplicar tal cual, o ''
   * si sigue valiendo: un descuento del catálogo dado de baja, o uno manual que
   * pide un supervisor o que supera el tope máximo de la sucursal.
   */
  private invalidDiscount(id: string, type: string, value: number): string {
    if (id) return this.discountCatalog.some(d => d.Id === id) ? '' : 'ya no está en el catálogo';
    if (!type || value <= 0) return '';
    const pct = type === 'Percentage';
    const max = pct ? this.maxDiscountPct : this.maxDiscountAmount;
    if (max !== null && value > max) return 'supera el tope máximo';
    const cajero = pct ? this.maxCashierDiscountPct : this.maxCashierDiscountAmount;
    if (this.isCashier && value > cajero) return 'requiere autorizar de nuevo';
    return '';
  }

  protected async resumeHeldSale(h: HeldSale) {
    if (this.cart.length > 0) {
      await this.msg.showMessageModal({ Description: 'Termine o ponga en espera la venta actual antes de retomar otra.', MessageType: 'warning' });
      return;
    }

    const { ok, Data } = await this.heldSaleService.take(h.Id);
    await this.loadHeldSales();
    if (!ok || !Data.Payload) return;

    const saved = JSON.parse(Data.Payload) as HeldPayload;
    const avisos: string[] = [];

    // Precios del momento: si cambiaron mientras esperaba, se cobra el vigente.
    const lines: SaleDetail[] = [];
    for (const l of saved.lines ?? []) {
      const prod = this.allProducts().find(p => p.Id === l.ProductId);
      if (!prod) { avisos.push(`«${l.ProductName}» ya no está disponible y se quitó.`); continue; }
      if (prod.SalePrice !== l.UnitPrice) avisos.push(`«${l.ProductName}»: el precio cambió a Bs. ${this.formatNum(prod.SalePrice)}.`);
      const motivo = this.invalidDiscount(l.DiscountId, l.DiscountType, l.DiscountValue);
      if (motivo) {
        avisos.push(`Se quitó el descuento de «${l.ProductName}»: ${motivo}.`);
        Object.assign(l, { DiscountId: '', DiscountLabel: '', DiscountType: '', DiscountValue: 0 });
      }
      lines.push({ ...l, UnitPrice: prod.SalePrice });
    }
    this.cart = lines;
    this.cart.forEach((_, i) => this.recalcLine(i));

    this.selectedCustomer = saved.customer ?? null;
    if (!this.selectedCustomer) this.loadDefaultCustomer();

    this.removeHeaderDiscount();
    const hd = saved.header;
    if (hd?.type && hd.value > 0) {
      const motivo = this.invalidDiscount(hd.id, hd.type, hd.value);
      if (motivo) {
        avisos.push(`Se quitó el descuento global: ${motivo}.`);
      } else {
        const base = this.cart.reduce((s, l) => s + l.LineTotal, 0);
        this.headerDiscountId = hd.id;
        this.headerDiscountLabel = hd.label;
        this.headerDiscountType = hd.type;
        this.headerDiscountValue = hd.value;
        this.headerDiscountAmount = this.discountOn(hd.type, hd.value, base);
      }
    }

    this.showHeldList = false;
    if (avisos.length) await this.msg.showMessageModal({ Description: avisos.join(' '), MessageType: 'info' });
  }

  protected async discardHeldSale(h: HeldSale) {
    const confirmed = await this.msg.showMessageQuestion(
      `¿Descartar la venta en espera${h.Label ? ` «${h.Label}»` : ''} por Bs. ${this.formatNum(h.Total)}? No se puede recuperar.`);
    if (!confirmed) return;
    await this.heldSaleService.discard(h.Id);
    await this.loadHeldSales();
  }

  // ── Reinicios y acciones post-venta ──
  private resetCart() {
    this.clearUndo();
    this.cart = [];
    this.activeTab = 'products';
    this.focusProductSearch();
  }

  protected resetAll() {
    this.resetCart();
    this.clearCustomer();
    this.loadDefaultCustomer();
    this.productSearch.set('');
    this.selectedCategory.set('');
    this.removeHeaderDiscount();
    this.supervisorAuthToken = '';
  }

  protected newOrder() {
    this.showCompletedModal = false;
    this.productService.getByName('').then(({ Data }) => {
      this.allProducts.set((Data ?? []).filter(p => p.IsActive && p.CurrentStock >= 0));
    });
    this.focusProductSearch();
  }

  protected printReceipt() {
    window.print();
  }

  // ── Salir del punto de venta ──
  // "Volver" y "Ver venta / Devolver" preguntan siempre. Cualquier otra salida
  // (menú, atrás del navegador) pregunta solo si hay una venta armada, que es lo
  // único que se pierde. `leaveConfirmed` evita preguntar dos veces.
  private leaveConfirmed = false;

  private cartLossWarning(): string {
    return `Hay una venta en curso con ${this.totalItems} producto(s) que se perderá. ` +
      'Si la va a retomar, póngala en espera antes de salir. ¿Salir del punto de venta?';
  }

  protected async leavePos() {
    const mensaje = this.cart.length > 0 ? this.cartLossWarning() : '¿Salir del punto de venta?';
    if (!(await this.msg.showMessageQuestion(mensaje))) return;
    this.leaveConfirmed = true;
    this.location.back();
  }

  /** Lo consulta el guard de salida de la ruta. */
  async canLeave(nextUrl: string): Promise<boolean> {
    if (this.leaveConfirmed || this.cart.length === 0) return true;
    // Sesión vencida o cierre de sesión: el carrito ya no se puede cobrar, y frenar
    // la salida dejaría al cajero en una pantalla sin sesión.
    if (nextUrl.startsWith('/auth/login')) return true;
    return this.msg.showMessageQuestion(this.cartLossWarning());
  }

  protected async goToReturn() {
    const confirmed = await this.msg.showMessageQuestion(
      '¿Registrar una devolución de esta venta? Se abrirá la ficha de la venta y saldrá del punto de venta.');
    if (!confirmed) return;
    this.leaveConfirmed = true;
    this.showCompletedModal = false;
    this.router.navigate(['sale-detail', this.completedSaleId]);
  }
}
