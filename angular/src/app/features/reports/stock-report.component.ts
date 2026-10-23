import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { todayIso } from '../../core/utils/date-helper';
import { exportToExcel } from '../../core/utils/excel-helper';
import { SharedModule } from '../../shared/shared.module';
import { Product } from '../inventory/products/product.model';
import { ProductService } from '../inventory/products/product.service';
import { BranchFilterComponent } from './branch-filter.component';

@Component({
  selector: 'app-stock-report',
  imports: [FormsModule, SharedModule, BranchFilterComponent],
  templateUrl: './stock-report.component.html',
})
export class StockReportComponent {
  private readonly productService = inject(ProductService);

  protected readonly products = signal<Product[]>([]);
  protected readonly loaded = signal(false);
  protected readonly name = signal('');
  protected readonly lowOnly = signal(false);
  // De qué sucursales se suma el stock: '' = la activa, un id, o 'all'.
  protected readonly branch = signal('');

  protected readonly displayRows = computed(() =>
    this.lowOnly() ? this.products().filter(p => p.CurrentStock < p.MinReorderQuantity) : this.products());

  protected readonly lowStockCount = computed(() => this.products().filter(p => p.CurrentStock < p.MinReorderQuantity).length);
  protected readonly totalUnits = computed(() => this.displayRows().reduce((s, p) => s + p.CurrentStock, 0));
  protected readonly totalValue = computed(() => this.displayRows().reduce((s, p) => s + p.CurrentStock * p.SalePrice, 0));

  protected fmt(v: number): string {
    return v.toLocaleString('es-BO', { style: 'currency', currency: 'BOB' });
  }

  protected formatUnits(v: number): string {
    return v.toLocaleString('es-BO');
  }

  protected async load() {
    const { ok, Data } = await this.productService.getByName(this.name(), this.branch());
    if (ok) {
      this.products.set(Data ?? []);
      this.loaded.set(true);
    }
  }

  protected exportar() {
    const rows = this.displayRows().map(p => ({
      Codigo: p.ProductCode,
      Producto: p.ProductName,
      Laboratorio: p.LaboratoryName,
      Stock_Actual: p.CurrentStock,
      Stock_Minimo: p.MinReorderQuantity,
      Precio_Venta: p.SalePrice,
      Valor_Inventario: +(p.CurrentStock * p.SalePrice).toFixed(2),
      Alerta: p.CurrentStock < p.MinReorderQuantity ? 'BAJO MÍNIMO' : '',
    }));
    exportToExcel(rows, `reporte_stock_${todayIso()}.xlsx`);
  }
}
