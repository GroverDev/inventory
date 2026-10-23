import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { PermissionsService } from '../../../core/services/permissions.service';
import { MsgService } from '../../../core/services/msg.service';
import { exportTemplateToExcel, exportToExcel, readExcel } from '../../../core/utils/excel-helper';
import { SharedModule } from '../../../shared/shared.module';
import { ProductThumbComponent } from '../components/product-thumb.component';
import { Product, ProductBulkUpdate } from './product.model';
import { ProductService } from './product.service';

const BULK_HEADERS = ['Id', 'ProductCode', 'ProductName', 'SalePrice', 'MinReorderQuantity', 'AvailableInPos', 'IsActive', 'BarCode', 'CurrentStock', 'LaboratoryName'];

@Component({
  selector: 'app-products-admin',
  imports: [FormsModule, SharedModule, ProductThumbComponent],
  templateUrl: './products-admin.component.html',
})
export class ProductsAdminComponent {
  private readonly router = inject(Router);
  private readonly msg = inject(MsgService);
  private readonly permissions = inject(PermissionsService);
  private readonly productService = inject(ProductService);

  protected readonly canCreate = computed(() => this.permissions.can('products-admin', 'create'));
  protected readonly canUpdate = computed(() => this.permissions.can('products-admin', 'update'));
  protected readonly canDelete = computed(() => this.permissions.can('products-admin', 'delete'));

  protected readonly products = signal<Product[]>([]);
  protected name = '';

  /**
   * Qué productos se manejan por lote o por serie: es justo el dato que decide
   * si una recepción va a pedir datos extra.
   */
  protected trackingLabel(product: Product): string {
    if (product.TrackingMode === 'lot') return 'Lote';
    if (product.TrackingMode === 'serial') return 'Serie';
    return '';
  }

  /** `subtle` + `emphasis`: son las variantes que el tema oscuro redefine. */
  protected trackingBadge(product: Product): string {
    const base = 'badge border ms-1 fw-normal';
    return product.TrackingMode === 'serial'
      ? `${base} bg-secondary-subtle text-secondary-emphasis border-secondary-subtle`
      : `${base} bg-info-subtle text-info-emphasis border-info-subtle`;
  }

  protected stockBadgeClass(product: Product): string {
    if (product.CurrentStock === 0) return 'bg-danger';
    if (product.CurrentStock <= product.MinReorderQuantity) return 'bg-danger';
    if (product.CurrentStock <= product.MinReorderQuantity * 1.5) return 'bg-warning text-dark';
    return 'bg-success';
  }

  async search() {
    const term = this.name.trim();
    if (term.length < 3) {
      this.msg.showMessageModal({ Description: 'Debe ingresar como mínimo tres caracteres para realizar la búsqueda.', MessageType: 'info' });
      return;
    }

    const { ok, Data } = await this.productService.getByName(term);
    this.products.set(Data ?? []);
    if (ok && this.products().length === 0) {
      this.msg.showMessageModal({ Description: 'No se encontraron productos con ese criterio de búsqueda.', MessageType: 'info' });
    }
  }

  async listAll() {
    this.name = '';
    const { ok, Data } = await this.productService.getAll();
    if (ok) this.products.set(Data);
  }

  create() {
    this.router.navigate(['product-edit', '0']);
  }

  edit(product: Product) {
    this.router.navigate(['product-edit', product.Id]);
  }

  async remove(product: Product) {
    // El Vue tampoco lo implementa todavía (TODO en el original): solo pregunta.
    await this.msg.showMessageQuestion(`¿Desea eliminar el producto "${product.ProductName}"?`);
  }

  exportProducts() {
    const products = this.products();
    if (!products.length) {
      exportTemplateToExcel(BULK_HEADERS, 'productos_template.xlsx');
      return;
    }

    exportToExcel(products.map(p => ({
      Id: p.Id,
      ProductCode: p.ProductCode,
      ProductName: p.ProductName,
      // Valores base: la planilla se reimporta como precio del catálogo, y la
      // excepción de la sucursal activa no debe terminar aplicada a todas.
      SalePrice: p.BaseSalePrice ?? p.SalePrice,
      MinReorderQuantity: p.BaseMinReorderQuantity ?? p.MinReorderQuantity,
      AvailableInPos: p.AvailableInPos,
      IsActive: p.IsActive,
      BarCode: p.BarCode,
      CurrentStock: p.CurrentStock,
      LaboratoryName: p.LaboratoryName,
    })), 'productos.xlsx');
  }

  async importProducts(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    let rows: ProductBulkUpdate[];
    try {
      rows = await readExcel<ProductBulkUpdate>(file);
    } catch {
      this.msg.showMessageModal({ Description: 'No se pudo leer el archivo Excel.', MessageType: 'error' });
      return;
    }

    if (!rows.length) {
      this.msg.showMessageModal({ Description: 'El archivo no contiene datos.', MessageType: 'info' });
      return;
    }

    const invalid = rows.filter(r => !r.Id || !/^[0-9a-fA-F-]{36}$/.test(String(r.Id)));
    if (invalid.length) {
      this.msg.showMessageModal({ Description: `${invalid.length} fila(s) no tienen un UUID válido en la columna Id.`, MessageType: 'error' });
      return;
    }

    const confirmed = await this.msg.showMessageQuestion(
      `¿Confirma actualizar ${rows.length} producto(s) en masa? Esta acción sobreescribirá: nombre, precio, cantidad mínima, disponibilidad en POS, estado y código de barras.`);
    if (!confirmed) return;

    const { ok, Data, Message } = await this.productService.bulkUpdate(rows);
    if (ok) {
      this.msg.showMessageModal({ Description: `Se actualizaron ${Data} producto(s) correctamente.`, MessageType: 'success' });
      if (this.name.trim().length >= 3) await this.search();
    } else {
      this.msg.showMessageModal(Message);
    }
  }
}
