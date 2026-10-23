import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject, ResponsePaged } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import { Product, ProductBulkUpdate } from './product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly api = inject(ApiService);

  create(product: Product) {
    return this.api.post<ResponseObject<string>>('Product', product);
  }

  update(product: Product) {
    return this.api.put<ResponseObject<boolean>>(`Product/${product.Id}`, product);
  }

  /** `branch`: de qué sucursales se suma el stock. '' = la activa, un id, o 'all'. */
  getByName(name: string, branch = '') {
    return this.api.get<ResponseArray<Product>>(
      `Product?productName=${encodeURIComponent(name)}` + (branch ? `&branch=${encodeURIComponent(branch)}` : ''),
    );
  }

  getStock(productName: string, page: number, pageSize: number) {
    const name = encodeURIComponent(productName);
    return this.api.get<ResponsePaged<Product>>(`Product/stock?productName=${name}&page=${page}&pageSize=${pageSize}`);
  }

  /** Productos activos para el punto de venta. */
  getForPos() {
    return this.api.get<ResponseArray<Product>>('Product');
  }

  validateSelection(productId: string) {
    return this.api.get<ResponseObject<{ SalePrice: number; CurrentStock: number }>>(`Product/${productId}/validate`);
  }

  getById(productId: string) {
    return this.api.get<ResponseObject<Product>>(`Product/${productId}`);
  }

  getAll() {
    return this.api.get<ResponseArray<Product>>('Product?productName=');
  }

  bulkUpdate(items: ProductBulkUpdate[]) {
    return this.api.put<ResponseObject<number>>('Product/bulk', items);
  }

  /**
   * Activa el seguimiento del producto, por lotes o por números de serie. Es
   * una acción aparte del guardado de la ficha porque no tiene vuelta atrás: el
   * stock actual queda como existencia sin identificar y desde entonces cada
   * recepción exige el código de lo que entra.
   */
  activateTracking(productId: string, modo: 'lot' | 'serial') {
    return this.api.post<ResponseObject<boolean>>(`Product/${productId}/tracking?modo=${modo}`, {});
  }

  /** Sube (o reemplaza) la imagen. Devuelve la ruta relativa que quedó guardada. */
  uploadImage(productId: string, file: Blob, fileName: string) {
    const form = new FormData();
    form.append('file', file, fileName);
    return this.api.postForm<ResponseObject<string>>(`Product/${productId}/image`, form);
  }

  deleteImage(productId: string) {
    return this.api.del<ResponseObject<boolean>>(`Product/${productId}/image`);
  }

  getBranchSettings(productId: string) {
    return this.api.get<ResponseArray<BranchSetting>>(`Product/${productId}/branch-settings`);
  }

  saveBranchSettings(productId: string, payload: BranchSettingPayload[]) {
    return this.api.put<ResponseObject<boolean>>(`Product/${productId}/branch-settings`, payload);
  }
}

export interface BranchSetting {
  BranchId: string;
  BranchName: string;
  SalePrice: number | null;
  MinReorderQuantity: number | null;
  BaseSalePrice: number;
  BaseMinReorderQuantity: number;
  Stock: number;
}

export interface BranchSettingPayload {
  branchId: string;
  salePrice: number | null;
  minReorderQuantity: number | null;
}
