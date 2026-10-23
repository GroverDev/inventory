import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { PaginationComponent } from '../../../shared/pagination/pagination.component';
import { SharedModule } from '../../../shared/shared.module';
import { Product } from '../products/product.model';
import { ProductService } from '../products/product.service';

/** Control de stock: lista paginada de productos con su existencia y accesos a historial y ajuste. */
@Component({
  selector: 'app-inventory-stock',
  imports: [FormsModule, SharedModule, PaginationComponent],
  templateUrl: './inventory-stock.component.html',
})
export class InventoryStockComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly productService = inject(ProductService);

  protected readonly products = signal<Product[]>([]);
  protected readonly loading = signal(false);
  protected readonly currentPage = signal(1);
  protected readonly pageSize = signal(15);
  protected readonly totalCount = signal(0);

  protected search = '';
  protected showOnlyLow = false;

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));
  protected readonly rangeStart = computed(() => (this.totalCount() === 0 ? 0 : (this.currentPage() - 1) * this.pageSize() + 1));
  protected readonly rangeEnd = computed(() => Math.min(this.currentPage() * this.pageSize(), this.totalCount()));

  ngOnInit() {
    return this.loadPage();
  }

  protected stockBadgeClass(product: Product): string {
    if (product.CurrentStock === 0) return 'bg-danger';
    if (product.CurrentStock <= product.MinReorderQuantity) return 'bg-danger';
    if (product.CurrentStock <= product.MinReorderQuantity * 1.5) return 'bg-warning text-dark';
    return 'bg-success';
  }

  private async loadPage() {
    this.loading.set(true);
    const resp = await this.productService.getStock(this.search.trim(), this.currentPage(), this.pageSize());
    if (resp.ok) {
      this.products.set(resp.Data ?? []);
      this.totalCount.set(resp.TotalCount);
    }
    this.loading.set(false);
  }

  protected onSearch() {
    this.currentPage.set(1);
    return this.loadPage();
  }

  protected onPageSizeChange(size: number) {
    this.pageSize.set(Number(size));
    return this.onSearch();
  }

  protected goToPage(page: number) {
    this.currentPage.set(page);
    return this.loadPage();
  }

  protected goHistory(product: Product) {
    this.router.navigate(['stock-history', product.Id], { queryParams: { name: product.ProductName, code: product.ProductCode } });
  }

  protected goAdjust(product: Product) {
    this.router.navigate(['stock-adjustment', product.Id], {
      queryParams: { name: product.ProductName, code: product.ProductCode, stock: product.CurrentStock },
    });
  }
}
