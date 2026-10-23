import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { MsgService } from '../../../core/services/msg.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { formatDateOnly } from '../../../core/utils/date-helper';
import { SharedModule } from '../../../shared/shared.module';
import { Branch } from '../../account/branches/branch.model';
import { BranchService } from '../../account/branches/branch.service';
import { Product } from '../products/product.model';
import { ProductService } from '../products/product.service';
import {
  StockTransfer, StockTransferLot, TransferLine, transferStatusBadge, transferStatusLabel,
} from './stock-transfer.model';
import { StockTransferService } from './stock-transfer.service';

const FORM = 'stock-transfers';

/** Un traspaso: armar el borrador, enviarlo, o recibirlo en la sucursal de destino. */
@Component({
  selector: 'app-stock-transfer',
  imports: [FormsModule, SharedModule],
  templateUrl: './stock-transfer.component.html',
  // Barra fija del móvil: la acción principal queda a la vista sin recorrer la lista.
  styles: `
    .action-bar {
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
export class StockTransferComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly permissions = inject(PermissionsService);
  private readonly transferService = inject(StockTransferService);
  private readonly productService = inject(ProductService);
  private readonly branchService = inject(BranchService);
  protected readonly auth = inject(AuthService);

  protected readonly statusLabel = transferStatusLabel;
  protected readonly statusBadge = transferStatusBadge;
  protected readonly formatDateOnly = formatDateOnly;

  protected readonly transfer = signal<StockTransfer | null>(null);
  protected readonly busy = signal(false);

  protected destBranchId = '';
  protected notes = '';
  protected lines: TransferLine[] = [];
  protected destinations: Branch[] = [];
  protected productSearch = '';
  protected productResults: Product[] = [];
  /** Cantidad recibida por id de lote. */
  protected received: Record<string, number> = {};

  protected get canDelete(): boolean {
    return this.permissions.can(FORM, 'delete');
  }

  /** Nuevo, o borrador propio de la sucursal de origen. */
  protected get isEditable(): boolean {
    const t = this.transfer();
    if (!t) return this.permissions.can(FORM, 'create');
    return t.Status === 'borrador' && t.Direction === 'salida' && this.permissions.can(FORM, 'update');
  }

  protected get canReceive(): boolean {
    const t = this.transfer();
    return t?.Status === 'enviado' && t.Direction === 'entrada' && this.permissions.can(FORM, 'update');
  }

  protected readonly title = computed(() => {
    const t = this.transfer();
    return t ? `Traspaso ${t.Number}` : 'Nuevo Traspaso';
  });

  /** Qué falta y dónde, cuando desde esta sucursal no hay nada que hacer. */
  protected get waitingElsewhere(): string {
    const t = this.transfer();
    if (!t) return '';
    if (t.Status === 'enviado' && t.Direction === 'salida') return `En tránsito: lo confirma ${t.DestBranchName} al recibirlo.`;
    if (t.Status === 'borrador' && t.Direction === 'entrada') return `Todavía es un borrador de ${t.OriginBranchName}: aparecerá para recibir cuando lo envíen.`;
    return '';
  }

  async ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    const { ok, Data } = await this.branchService.getAll();
    if (ok) this.destinations = Data.filter(b => b.Id !== this.auth.user()?.BranchId);

    if (id && id !== '0') await this.load(id);
  }

  private async load(id: string) {
    const { ok, Data } = await this.transferService.getById(id);
    if (!ok) return;

    this.transfer.set(Data);
    this.destBranchId = Data.DestBranchId;
    this.notes = Data.Notes;
    this.lines = Data.Detail.map(d => ({
      ProductId: d.ProductId,
      ProductCode: d.ProductCode,
      ProductName: d.ProductName,
      Available: d.AvailableAtOrigin,
      Quantity: d.Quantity,
    }));
    this.received = Object.fromEntries(Data.Detail.flatMap(d => d.Lots).map(l => [l.Id, l.QuantityReceived ?? l.QuantitySent]));
  }

  protected async searchProducts() {
    const term = this.productSearch.trim();
    if (!term) return;
    const { ok, Data } = await this.productService.getByName(term);
    if (ok) this.productResults = Data.filter(p => p.IsActive);
  }

  protected addProduct(p: Product) {
    this.productResults = [];
    this.productSearch = '';
    if (this.lines.some(l => l.ProductId === p.Id)) {
      this.msg.showMessageModal({ Description: 'El producto ya está en la lista: corrija su cantidad.', MessageType: 'info' });
      return;
    }
    this.lines.push({ ProductId: p.Id, ProductCode: p.ProductCode, ProductName: p.ProductName, Available: p.CurrentStock, Quantity: 1 });
  }

  protected removeLine(index: number) {
    this.lines.splice(index, 1);
  }

  protected lineInvalid(l: TransferLine): boolean {
    return l.Quantity <= 0 || l.Quantity > l.Available;
  }

  private async validDraft(): Promise<boolean> {
    let problema = '';
    if (!this.destBranchId) problema = 'Elija la sucursal de destino.';
    else if (this.lines.length === 0) problema = 'Agregue al menos un producto.';
    else if (this.lines.some(l => !(l.Quantity > 0))) problema = 'Las cantidades deben ser mayores a cero.';
    if (problema) await this.msg.showMessageModal({ Description: problema, MessageType: 'warning' });
    return !problema;
  }

  /** Guarda el borrador y devuelve su id, o '' si no se pudo. */
  private async persistDraft(): Promise<string> {
    const t = this.transfer();
    if (t) {
      const { ok } = await this.transferService.updateDraft(t.Id, this.destBranchId, this.notes, this.lines);
      return ok ? t.Id : '';
    }
    const { ok, Data } = await this.transferService.createDraft(this.destBranchId, this.notes, this.lines);
    return ok ? Data : '';
  }

  protected async saveDraft() {
    if (!(await this.validDraft())) return;
    this.busy.set(true);
    const id = await this.persistDraft();
    this.busy.set(false);
    if (!id) return;

    await this.msg.showMessageModal({ Description: 'Borrador guardado. Envíelo cuando la mercadería salga.', MessageType: 'success' });
    if (!this.transfer()) await this.router.navigate(['stock-transfer', id], { replaceUrl: true });
    await this.load(id);
  }

  protected async send() {
    if (!(await this.validDraft())) return;
    const destino = this.destinations.find(b => b.Id === this.destBranchId)?.Name ?? 'el destino';
    const confirmed = await this.msg.showMessageQuestion(
      `¿Enviar a ${destino}? El stock sale ahora de esta sucursal y queda en tránsito hasta que lo reciban.`);
    if (!confirmed) return;

    this.busy.set(true);
    const id = await this.persistDraft();
    const ok = id ? (await this.transferService.send(id)).ok : false;
    this.busy.set(false);

    if (id && !this.transfer()) await this.router.navigate(['stock-transfer', id], { replaceUrl: true });
    if (id) await this.load(id);
    if (ok) await this.msg.showMessageModal({ Description: 'Traspaso enviado.', MessageType: 'success' });
  }

  protected validReceived(l: StockTransferLot): boolean {
    const q = this.received[l.Id];
    if (typeof q !== 'number' || q < 0 || q > l.QuantitySent) return false;
    return !l.SerialNumber || q === 0 || q === 1;
  }

  protected async receive() {
    const t = this.transfer()!;
    const lots = t.Detail.flatMap(d => d.Lots);
    if (lots.some(l => !this.validReceived(l))) {
      await this.msg.showMessageModal({ Description: 'Revise las cantidades recibidas: van de 0 a lo enviado.', MessageType: 'warning' });
      return;
    }

    const faltante = lots.reduce((s, l) => s + (l.QuantitySent - (this.received[l.Id] ?? 0)), 0);
    const confirmed = await this.msg.showMessageQuestion(faltante > 0
      ? `Faltan ${faltante} unidad(es) respecto de lo enviado: quedarán como merma de ${t.OriginBranchName}. ¿Confirmar la recepción?`
      : '¿Confirmar que llegó todo lo enviado? El stock entra a esta sucursal.');
    if (!confirmed) return;

    this.busy.set(true);
    const { ok } = await this.transferService.receive(t.Id, this.received);
    this.busy.set(false);
    if (!ok) return;

    await this.msg.showMessageModal({ Description: 'Recepción registrada.', MessageType: 'success' });
    await this.load(t.Id);
  }

  protected async cancel() {
    if (!(await this.msg.showMessageQuestion('¿Anular este borrador? No se movió stock todavía.'))) return;

    const { ok } = await this.transferService.cancel(this.transfer()!.Id);
    if (ok) this.back();
  }

  protected back() {
    this.router.navigate(['stock-transfers']);
  }

  protected formatDateTime(value: string | null): string {
    return value
      ? new Date(value).toLocaleString('es-BO', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false })
      : '—';
  }
}
