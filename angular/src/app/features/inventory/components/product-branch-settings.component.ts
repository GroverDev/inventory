import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AuthService } from '../../../core/services/auth.service';
import { MsgService } from '../../../core/services/msg.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { BranchSetting, ProductService } from '../products/product.service';

/** Fila editable: los inputs trabajan con texto para distinguir "vacío" de 0. */
interface Row extends BranchSetting {
  SalePriceText: string | number;
  MinText: string | number;
}

/** '' o null = sin excepción; cualquier otro valor, número. */
const toNumber = (v: string | number | null): number | null =>
  v === '' || v === null || v === undefined ? null : Number(v);

/** Precio y mínimo de reposición por sucursal; guarda por su cuenta, aparte de la ficha. */
@Component({
  selector: 'app-product-branch-settings',
  imports: [FormsModule],
  template: `
    <h6 class="text-muted border-bottom pb-2 mb-3 mt-2">
      <i class="ri-store-2-line align-middle me-1"></i> Precio y mínimo por sucursal
      <small class="fw-normal ms-1">(opcional)</small>
    </h6>
    <p class="small text-muted mb-2">
      Deje en blanco para usar los valores base de arriba. Complete solo las sucursales que venden a otro
      precio o necesitan otro mínimo de reposición.
    </p>

    @if (rows().length === 0) {
      <div class="text-center py-2"><small class="text-muted">Cargando sucursales...</small></div>
    } @else {
      <div class="table-responsive">
        <table class="table table-sm align-middle mb-2">
          <thead>
            <tr>
              <th>Sucursal</th>
              <th class="text-center">Stock</th>
              <th style="width: 11rem">Precio (Bs.)</th>
              <th style="width: 9rem">Mínimo</th>
            </tr>
          </thead>
          <tbody>
            @for (r of rows(); track r.BranchId) {
            <tr>
              <td>
                {{ r.BranchName }}
                @if (r.BranchId === auth.user()?.BranchId) {<span class="badge bg-info ms-1">Actual</span>}
              </td>
              <td class="text-center">{{ r.Stock }}</td>
              <td>
                <input type="number" min="0" step="0.01" class="form-control form-control-sm text-end"
                  [placeholder]="'base ' + r.BaseSalePrice.toFixed(2)" [disabled]="!canUpdate()" [(ngModel)]="r.SalePriceText">
              </td>
              <td>
                <input type="number" min="0" step="1" class="form-control form-control-sm text-end"
                  [placeholder]="'base ' + r.BaseMinReorderQuantity" [disabled]="!canUpdate()" [(ngModel)]="r.MinText">
              </td>
            </tr>
            }
          </tbody>
        </table>
      </div>
    }

    @if (canUpdate() && rows().length > 0) {
      <button type="button" class="btn btn-sm btn-outline-primary mb-3" [disabled]="saving()" (click)="save()">
        <i class="ri-save-line align-middle me-1"></i>Guardar precios por sucursal
      </button>
    }
  `,
})
export class ProductBranchSettingsComponent implements OnInit {
  protected readonly auth = inject(AuthService);
  private readonly products = inject(ProductService);
  private readonly msg = inject(MsgService);
  private readonly permissions = inject(PermissionsService);

  readonly productId = input.required<string>();

  protected readonly canUpdate = computed(() => this.permissions.can('products-admin', 'update'));
  protected readonly rows = signal<Row[]>([]);
  protected readonly saving = signal(false);

  ngOnInit() {
    return this.load();
  }

  private async load() {
    const { ok, Data } = await this.products.getBranchSettings(this.productId());
    if (!ok) return;
    this.rows.set(Data.map(s => ({ ...s, SalePriceText: s.SalePrice ?? '', MinText: s.MinReorderQuantity ?? '' })));
  }

  protected async save() {
    const payload = this.rows().map(r => ({
      branchId: r.BranchId,
      salePrice: toNumber(r.SalePriceText),
      minReorderQuantity: toNumber(r.MinText),
    }));

    const invalid = payload.some(p =>
      (p.salePrice !== null && (isNaN(p.salePrice) || p.salePrice < 0)) ||
      (p.minReorderQuantity !== null && (!Number.isInteger(p.minReorderQuantity) || p.minReorderQuantity < 0)));
    if (invalid) {
      await this.msg.showMessageModal({ Description: 'Los precios no pueden ser negativos y el mínimo debe ser un número entero.', MessageType: 'warning' });
      return;
    }

    this.saving.set(true);
    const { ok } = await this.products.saveBranchSettings(this.productId(), payload);
    this.saving.set(false);
    if (!ok) return;

    await this.msg.showMessageModal({ Description: 'Precios por sucursal guardados.', MessageType: 'success' });
    await this.load();
  }
}
