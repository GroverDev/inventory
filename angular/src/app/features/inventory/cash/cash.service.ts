import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import { PaymentMethod } from '../sales/sale.model';
import {
  CashCloseSettings, CashMovementRequest, CashSession, CloseCashSessionRequest, OpenCashSessionRequest, SessionSale,
} from './cash.model';

@Injectable({ providedIn: 'root' })
export class CashService {
  private readonly api = inject(ApiService);

  getActive() {
    return this.api.get<ResponseObject<CashSession>>('CashSession/active');
  }

  getById(id: string) {
    return this.api.get<ResponseObject<CashSession>>(`CashSession/${id}`);
  }

  getAll(dateFrom: string, dateTo: string) {
    return this.api.get<ResponseArray<CashSession>>(`CashSession?dateFrom=${dateFrom}&dateTo=${dateTo}`);
  }

  open(request: OpenCashSessionRequest) {
    return this.api.post<ResponseObject<string>>('CashSession/open', request);
  }

  close(id: string, request: CloseCashSessionRequest) {
    return this.api.put<ResponseObject<CashSession>>(`CashSession/${id}/close`, request);
  }

  getSales(id: string) {
    return this.api.get<ResponseArray<SessionSale>>(`CashSession/${id}/sales`);
  }

  addMovement(sessionId: string, request: CashMovementRequest) {
    return this.api.post<ResponseObject<string>>(`CashSession/${sessionId}/movements`, request);
  }

  /** Configuración del cierre de caja; la pantalla de ajustes además recibe los medios de pago. */
  getCloseSettings() {
    return this.api.get<ResponseObject<CashCloseSettings & { Methods: PaymentMethod[] }>>('Settings/cash-close');
  }

  saveCloseSettings(payload: {
    noteThreshold: number;
    supervisorThreshold: number | null;
    maxAttempts: number | null;
    requireDenominations: boolean;
    methods: { id: string; requiresCount: boolean }[];
  }) {
    return this.api.put<ResponseObject<boolean>>('Settings/cash-close', payload);
  }
}
