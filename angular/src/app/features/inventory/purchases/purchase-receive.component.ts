import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FormAction, FormActionsComponent } from '../../../shared/form-actions/form-actions.component';

import { MsgService } from '../../../core/services/msg.service';
import { formatDateOnly, todayIso } from '../../../core/utils/date-helper';
import { SharedModule } from '../../../shared/shared.module';
import {
  PURCHASE_STATUS, Purchase, PurchaseDelivery, PurchaseDeliveryDetail,
  newPurchase, newPurchaseDelivery, purchaseStatusBadge, purchaseStatusLabel,
} from './purchase.model';
import { PurchaseService } from './purchase.service';

/** Uid de operación: evita que un doble envío duplique el ingreso de stock. */
const newOperationUid = (): string =>
  typeof crypto !== 'undefined' && 'randomUUID' in crypto
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(16).slice(2)}`.padEnd(36, '0');

/** Recepción (total o parcial) de una orden de compra, con lote o series cuando el producto los usa. */
@Component({
  selector: 'app-purchase-receive',
  imports: [FormsModule, SharedModule, FormActionsComponent],
  templateUrl: './purchase-receive.component.html',
  // Barra fija del móvil: el total y "Confirmar" no obligan a recorrer la lista.
  styles: `
    .receive-bar {
      position: sticky;
      bottom: 0;
      z-index: 5;
      margin: 0.5rem -1rem 0;
      padding: 0.75rem 1rem;
      background: var(--bs-body-bg);
      border-top: 1px solid var(--bs-border-color);
      box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.08);
    }
  `,
})
export class PurchaseReceiveComponent implements OnInit {
  protected readonly actions: FormAction[] = [
    { label: 'Confirmar Recepción', icon: 'ri-inbox-archive-line', btnClass: 'btn-success', run: () => this.save(), disabled: () => this.isSaved() || !this.canSubmit },
    { label: 'Cerrar con Faltante', icon: 'ri-lock-line', btnClass: 'btn-outline-secondary', run: () => this.closeWithShortage(), disabled: () => this.isSaved(), visible: () => this.canClose },
    { label: 'Cancelar', icon: 'ri-forbid-line', btnClass: 'btn-warning', run: () => this.back() },
  ];

  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly msg = inject(MsgService);
  private readonly purchaseService = inject(PurchaseService);

  protected readonly formatDateOnly = formatDateOnly;
  protected readonly statusBadge = purchaseStatusBadge;
  protected readonly statusLabel = purchaseStatusLabel;

  protected purchase: Purchase = newPurchase();
  protected delivery: PurchaseDelivery = newPurchaseDelivery();
  protected readonly isSaved = signal(false);
  protected readonly loaded = signal(false);

  protected lineErrors: boolean[] = [];
  protected lotErrors: boolean[] = [];
  protected serialErrors: boolean[] = [];

  /**
   * En horario LOCAL: con UTC, a partir de las 20:00 el servidor rechazaba la
   * recepción por futura y no se podía recepcionar nada hasta la medianoche.
   */
  protected readonly today = todayIso();

  protected formatCurrency(val: number): string {
    return (val ?? 0).toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  /** El servidor rechaza la recepción sin lote cuando el producto lo lleva. */
  protected usesLot(line: PurchaseDeliveryDetail): boolean {
    return line.TrackingMode === 'lot';
  }

  /** Una unidad, un número de serie: la lista tiene que igualar a la cantidad. */
  protected usesSerial(line: PurchaseDeliveryDetail): boolean {
    return line.TrackingMode === 'serial';
  }

  protected serialCountClass(line: PurchaseDeliveryDetail): string {
    return line.SerialNumbers.length === line.DeliveryQuantity ? 'badge bg-success ms-1' : 'badge bg-secondary ms-1';
  }

  /** Una serie por línea; los vacíos se descartan al escribir. */
  protected onSerialsInput(line: PurchaseDeliveryDetail, index: number, text: string) {
    line.SerialNumbers = text.split('\n').map(x => x.trim()).filter(x => x.length > 0);
    this.serialErrors[index] = false;
  }

  /** Un vencimiento ya cumplido no bloquea, pero sí se advierte antes de confirmar. */
  protected isExpired(line: PurchaseDeliveryDetail): boolean {
    return !!line.ExpiryDate && line.ExpiryDate < this.today;
  }

  protected get hasPending(): boolean {
    return this.delivery.Detail.some(d => d.PendingQuantity > 0);
  }

  protected get deliveryTotal(): number {
    return this.delivery.Detail.reduce((acc, d) => acc + d.DeliveryQuantity * d.UnitPrice, 0);
  }

  /** Habilita el guardado solo si hay al menos una unidad cargada. */
  protected get canSubmit(): boolean {
    return this.hasPending && this.delivery.Detail.some(d => d.DeliveryQuantity > 0);
  }

  /** El cierre con faltante solo aplica sobre una orden ya parcialmente recibida. */
  protected get canClose(): boolean {
    return this.purchase.PurchaseStatusId === PURCHASE_STATUS.PARTIALLY_RECEIVED;
  }

  async ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id || id === '0') return this.back();
    await this.load(id);
  }

  private async load(id: string) {
    const { ok, Data } = await this.purchaseService.getById(id);
    if (!ok) return this.back();

    this.purchase = { ...Data, PurchaseDate: Data.PurchaseDate ? Data.PurchaseDate.toString().substring(0, 10) : '' };

    this.delivery = {
      ...newPurchaseDelivery(),
      PurchaseId: Data.Id,
      DeliveryDate: this.today,
      OperationUid: newOperationUid(),
      Detail: (Data.Detail ?? []).map(d => {
        const ordered = Number(d.OrderedQuantity ?? 0);
        const received = Number(d.ReceivedQuantity ?? 0);
        // Si el backend no envía el pendiente, se deriva en lugar de quedar en
        // undefined: así la fila se comporta de forma coherente.
        const pending = Number(d.PendingQuantity ?? Math.max(0, ordered - received));
        return {
          Id: '',
          PurchaseDeliveryId: '',
          ProductId: d.ProductId,
          ProductName: d.ProductName,
          OrderedQuantity: ordered,
          ReceivedQuantity: received,
          PendingQuantity: pending,
          // Se propone recibir el saldo pendiente, no el total ordenado.
          DeliveryQuantity: pending,
          // El precio pactado es el punto de partida; se corrige si el proveedor facturó otro.
          UnitPrice: d.OrderUnitPrice ?? 0,
          LotCode: '',
          ExpiryDate: '',
          // Una orden vieja puede venir sin el campo; sin seguimiento es el caso simple.
          TrackingMode: d.TrackingMode ?? 'none',
          SerialNumbers: [],
        };
      }),
    };

    this.lineErrors = this.delivery.Detail.map(() => false);
    this.lotErrors = this.delivery.Detail.map(() => false);
    this.serialErrors = this.delivery.Detail.map(() => false);
    this.loaded.set(true);
  }

  /** Recorta la cantidad al pendiente disponible mientras el usuario tipea. */
  protected clampLine(index: number) {
    const line = this.delivery.Detail[index];
    const qty = Number(line.DeliveryQuantity);

    if (Number.isNaN(qty) || qty < 0) line.DeliveryQuantity = 0;
    else if (qty > line.PendingQuantity) line.DeliveryQuantity = line.PendingQuantity;
    this.lineErrors[index] = false;
  }

  protected back() {
    this.router.navigate(['purchases-admin']);
  }

  protected async save() {
    const warn = (Description: string) => this.msg.showMessageModal({ Description, MessageType: 'warning' });
    const detail = this.delivery.Detail;

    if (!this.delivery.DeliveryDate) return warn('Ingrese la fecha de recepción.');
    if (this.delivery.DeliveryDate > this.today) return warn('La fecha de recepción no puede ser futura.');

    // El servidor revalida contra el pendiente real; esto solo evita el viaje.
    const invalid = detail.findIndex(d => d.DeliveryQuantity > d.PendingQuantity);
    if (invalid >= 0) {
      this.lineErrors[invalid] = true;
      return warn(`No puede recibir más de ${detail[invalid].PendingQuantity} de "${detail[invalid].ProductName}".`);
    }

    if (!detail.some(d => d.DeliveryQuantity > 0)) {
      return warn('Debe indicar al menos un producto con cantidad recibida.');
    }

    // Sin lote el servidor rechaza la línea y se pierde toda la recepción, no solo
    // esa fila. Se corta acá para que el usuario no reescriba lo demás.
    const sinLote = detail.findIndex(d => d.DeliveryQuantity > 0 && this.usesLot(d) && !d.LotCode);
    if (sinLote >= 0) {
      this.lotErrors[sinLote] = true;
      return warn(`"${detail[sinLote].ProductName}" se maneja por lotes: indique el lote recibido.`);
    }

    // La cantidad de series tiene que coincidir con las unidades.
    const seriesMal = detail.findIndex(d => d.DeliveryQuantity > 0 && this.usesSerial(d) && d.SerialNumbers.length !== d.DeliveryQuantity);
    if (seriesMal >= 0) {
      const linea = detail[seriesMal];
      this.serialErrors[seriesMal] = true;
      return warn(`"${linea.ProductName}" se identifica por número de serie: indique ${linea.DeliveryQuantity} número(s), hay ${linea.SerialNumbers.length}.`);
    }

    // Un mismo número dos veces sería la misma unidad física repetida.
    const seriesRepetidas = detail.findIndex(d => {
      if (!this.usesSerial(d) || d.DeliveryQuantity === 0) return false;
      const normalizadas = d.SerialNumbers.map(x => x.toUpperCase());
      return new Set(normalizadas).size !== normalizadas.length;
    });
    if (seriesRepetidas >= 0) {
      this.serialErrors[seriesRepetidas] = true;
      return warn(`Hay números de serie repetidos en "${detail[seriesRepetidas].ProductName}".`);
    }

    // Recibir mercadería vencida es casi siempre un error de tipeo, pero a veces es
    // real (una devolución al proveedor pendiente). Se advierte, no se prohíbe.
    const vencidos = detail.filter(d => d.DeliveryQuantity > 0 && this.isExpired(d));
    if (vencidos.length > 0) {
      const nombres = vencidos.map(d => d.ProductName).join(', ');
      if (!(await this.msg.showMessageQuestion(`El vencimiento indicado ya pasó en: ${nombres}. ¿Registrar la recepción de todos modos?`))) return;
    }

    const partial = detail.some(d => d.DeliveryQuantity < d.PendingQuantity);
    const question = partial
      ? '¿Confirma esta recepción parcial? La orden quedará con saldo pendiente.'
      : '¿Confirma la recepción de esta orden?';
    if (!(await this.msg.showMessageQuestion(question))) return;

    const { ok } = await this.purchaseService.receive(this.purchase.Id, this.delivery);
    if (ok) {
      this.isSaved.set(true);
      await this.msg.showMessageModal({ Description: 'La recepción se registró correctamente.', MessageType: 'success' });
      this.back();
    }
  }

  protected async closeWithShortage() {
    if (!(await this.msg.showMessageQuestion('¿Cerrar la orden con faltante? El saldo pendiente ya no se podrá recibir.'))) return;

    const { ok } = await this.purchaseService.close(this.purchase.Id);
    if (ok) {
      this.isSaved.set(true);
      await this.msg.showMessageModal({ Description: 'La orden se cerró con faltante.', MessageType: 'success' });
      this.back();
    }
  }
}
