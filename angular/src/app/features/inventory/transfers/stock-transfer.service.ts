import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import { StockTransfer, TransferLine } from './stock-transfer.model';

const toRequest = (destBranchId: string, notes: string, lines: TransferLine[]) => ({
  destBranchId,
  notes,
  detail: lines.map(l => ({ productId: l.ProductId, quantity: l.Quantity })),
});

@Injectable({ providedIn: 'root' })
export class StockTransferService {
  private readonly api = inject(ApiService);

  /** Fechas como días del calendario (yyyy-mm-dd). Los pendientes vienen siempre. */
  getAll(dateFrom: string, dateTo: string, status = '') {
    return this.api.get<ResponseArray<StockTransfer>>(`StockTransfer?dateFrom=${dateFrom}&dateTo=${dateTo}&status=${status}`);
  }

  getById(id: string) {
    return this.api.get<ResponseObject<StockTransfer>>(`StockTransfer/${id}`);
  }

  createDraft(destBranchId: string, notes: string, lines: TransferLine[]) {
    return this.api.post<ResponseObject<string>>('StockTransfer', toRequest(destBranchId, notes, lines));
  }

  updateDraft(id: string, destBranchId: string, notes: string, lines: TransferLine[]) {
    return this.api.put<ResponseObject<boolean>>(`StockTransfer/${id}`, toRequest(destBranchId, notes, lines));
  }

  send(id: string) {
    return this.api.post<ResponseObject<boolean>>(`StockTransfer/${id}/send`, {});
  }

  /** `received`: cantidad recibida por id de lote. Los que no figuran se reciben completos. */
  receive(id: string, received: Record<string, number>) {
    return this.api.post<ResponseObject<boolean>>(`StockTransfer/${id}/receive`, {
      lots: Object.entries(received).map(([lotId, quantityReceived]) => ({ lotId, quantityReceived })),
    });
  }

  cancel(id: string) {
    return this.api.del<ResponseObject<boolean>>(`StockTransfer/${id}`);
  }
}
