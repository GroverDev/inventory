import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import { PaymentMethod, Sale, SaleReturnRequest, SalesPagedResult } from './sale.model';

/** Id con el que el servidor pide la firma de un supervisor por falta de stock. */
export const REQUIRES_SUPERVISOR_STOCK = 'requires-supervisor-stock';

@Injectable({ providedIn: 'root' })
export class SaleService {
  private readonly api = inject(ApiService);

  /** Ese aviso lo maneja el punto de venta pidiendo el supervisor, no un modal de error. */
  save(sale: Sale) {
    return this.api.post<ResponseObject<string>>('Sales', sale, { silentMessageIds: [REQUIRES_SUPERVISOR_STOCK] });
  }

  /** `branch`: '' = sucursal activa, un id, o 'all' para el consolidado. */
  getAll(dateInitial: string, dateEnd: string, page = 1, pageSize = 50, sellerName?: string, branch = '') {
    const params = new URLSearchParams({
      saleDateInitial: dateInitial,
      saleDateEnd: dateEnd,
      page: String(page),
      pageSize: String(pageSize),
    });
    if (sellerName) params.append('sellerName', sellerName);
    if (branch) params.append('branch', branch);
    return this.api.get<ResponseObject<SalesPagedResult>>(`Sales?${params}`);
  }

  getById(id: string) {
    return this.api.get<ResponseObject<Sale>>(`Sales/${id}`);
  }

  remove(id: string) {
    return this.api.del<ResponseObject<boolean>>(`Sales/${id}`);
  }

  createReturn(request: SaleReturnRequest) {
    return this.api.post<ResponseObject<string>>('SaleReturn', request);
  }

  getPaymentMethods() {
    return this.api.get<ResponseArray<PaymentMethod>>('PaymentMethod');
  }
}
