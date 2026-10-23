import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FormAction, FormActionsComponent } from '../../../shared/form-actions/form-actions.component';

import { MsgService } from '../../../core/services/msg.service';
import { todayIso } from '../../../core/utils/date-helper';
import { SharedModule } from '../../../shared/shared.module';
import { Provider, ProviderCrud } from '../../catalogs/provider.crud';
import { Product } from '../products/product.model';
import { ProductService } from '../products/product.service';
import { Purchase, PurchaseDetail, newPurchase, newPurchaseDetail } from './purchase.model';
import { PurchaseService } from './purchase.service';

/** Alta y edición de una orden de compra, con el detalle de productos pedidos. */
@Component({
  selector: 'app-purchase-edit',
  imports: [FormsModule, SharedModule, FormActionsComponent],
  templateUrl: './purchase-edit.component.html',
})
export class PurchaseEditComponent implements OnInit {
  protected readonly actions: FormAction[] = [
    { label: 'Grabar', icon: 'ri-save-line', btnClass: 'btn-primary', run: () => this.save(), disabled: () => this.isSaved() },
    { label: 'Cancelar', icon: 'ri-forbid-line', btnClass: 'btn-warning', run: () => this.back() },
  ];

  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly msg = inject(MsgService);
  private readonly purchaseService = inject(PurchaseService);
  private readonly providerCrud = inject(ProviderCrud);
  private readonly productService = inject(ProductService);

  protected purchase: Purchase = newPurchase();
  protected readonly isSaved = signal(false);

  // Búsqueda de proveedor
  protected providerSearch = '';
  protected providerResults: Provider[] = [];

  // Búsqueda de producto para la línea nueva
  protected productSearch = '';
  protected productResults: Product[] = [];
  protected newLine: PurchaseDetail = newPurchaseDetail();

  protected get lineSubtotal(): number {
    return +(this.newLine.OrderedQuantity * this.newLine.OrderUnitPrice).toFixed(2);
  }

  protected formatCurrency(val: number): string {
    return val?.toLocaleString('es-BO', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '0.00';
  }

  async ngOnInit() {
    // Local, no UTC: de noche una compra nueva quedaba fechada al día siguiente.
    const today = todayIso();
    this.purchase.PurchaseDate = today;
    this.purchase.EstimatedDeliveryDate = today;

    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== '0') await this.load(id);
  }

  private async load(id: string) {
    const { ok, Data } = await this.purchaseService.getById(id);
    if (!ok) return;
    this.purchase = {
      ...Data,
      // Los inputs de fecha esperan yyyy-MM-dd.
      PurchaseDate: Data.PurchaseDate ? Data.PurchaseDate.toString().substring(0, 10) : '',
      EstimatedDeliveryDate: Data.EstimatedDeliveryDate ? Data.EstimatedDeliveryDate.toString().substring(0, 10) : '',
    };
  }

  protected back() {
    this.router.navigate(['purchases-admin']);
  }

  // ── Proveedor ───────────────────────────────────────────
  protected async searchProviders() {
    if (!this.providerSearch.trim()) return;
    const { Data } = await this.providerCrud.list(this.providerSearch.trim());
    this.providerResults = Data ?? [];
  }

  protected selectProvider(p: Provider) {
    this.purchase.ProviderId = p.Id;
    this.purchase.ProviderName = p.ProviderName;
    this.providerResults = [];
    this.providerSearch = '';
  }

  protected clearProvider() {
    this.purchase.ProviderId = '';
    this.purchase.ProviderName = '';
  }

  // ── Detalle ─────────────────────────────────────────────
  protected async searchProducts() {
    if (!this.productSearch.trim()) return;
    const { Data } = await this.productService.getByName(this.productSearch.trim());
    this.productResults = Data ?? [];
  }

  protected selectProduct(prod: Product) {
    this.newLine.ProductId = prod.Id;
    this.newLine.ProductName = prod.ProductName;
    this.newLine.OrderUnitPrice = prod.SalePrice ?? 0;
    this.productResults = [];
    this.productSearch = '';
  }

  protected clearProduct() {
    this.newLine.ProductId = '';
    this.newLine.ProductName = '';
    this.newLine.OrderUnitPrice = 0;
  }

  protected addLine() {
    if (!this.newLine.ProductId) return;
    this.purchase.Detail.push({ ...this.newLine, OrderFinalPrice: this.lineSubtotal });
    this.recalcTotal();
    this.newLine = newPurchaseDetail();
  }

  protected removeLine(index: number) {
    this.purchase.Detail.splice(index, 1);
    this.recalcTotal();
  }

  private recalcTotal() {
    this.purchase.Total = +this.purchase.Detail.reduce((sum, d) => sum + d.OrderFinalPrice, 0).toFixed(2);
  }

  // ── Guardar ─────────────────────────────────────────────
  protected async save() {
    const warn = (Description: string) => this.msg.showMessageModal({ Description, MessageType: 'warning' });

    if (!this.purchase.ProviderId) return warn('Seleccione un proveedor.');
    if (!this.purchase.PurchaseDate) return warn('Ingrese la fecha de compra.');
    if (this.purchase.Detail.length === 0) return warn('Agregue al menos un producto al detalle.');

    if (!(await this.msg.showMessageQuestion('¿Desea guardar la compra?'))) return;

    const creating = !this.purchase.Id;
    const { ok } = creating
      ? await this.purchaseService.create(this.purchase)
      : await this.purchaseService.update(this.purchase);
    if (!ok) return;

    if (creating) this.isSaved.set(true);
    await this.msg.showMessageModal({
      Description: creating ? 'La compra se registró correctamente.' : 'La compra se actualizó correctamente.',
      MessageType: 'success',
    });
    this.back();
  }
}
