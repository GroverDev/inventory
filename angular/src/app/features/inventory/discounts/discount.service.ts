import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';

export interface Discount {
  Id: string;
  Name: string;
  /** 'Percentage' | 'FixedAmount' */
  Type: string;
  Value: number;
  Description: string;
  IsActive: boolean;
}

export const newDiscount = (): Discount => ({ Id: '', Name: '', Type: '', Value: 0, Description: '', IsActive: true });

/** Tope de descuento manual de una sucursal (BranchId null = toda la empresa). */
export interface DiscountLimitLevel {
  BranchId: string | null;
  BranchName: string;
  CashierMaxPct: number | null;
  CashierMaxAmount: number | null;
  GeneralMaxPct: number | null;
  GeneralMaxAmount: number | null;
}

export interface DiscountLimitPayload {
  branchId: string | null;
  cashierMaxPct: number | null;
  cashierMaxAmount: number | null;
  generalMaxPct: number | null;
  generalMaxAmount: number | null;
  branchName: string;
}

@Injectable({ providedIn: 'root' })
export class DiscountService {
  private readonly api = inject(ApiService);

  getAll() {
    return this.api.get<ResponseArray<Discount>>('Discounts');
  }

  getById(id: string) {
    return this.api.get<ResponseObject<Discount>>(`Discounts/${id}`);
  }

  create(request: Discount) {
    return this.api.post<ResponseObject<string>>('Discounts', request);
  }

  update(id: string, request: Discount) {
    return this.api.put<ResponseObject<boolean>>(`Discounts/${id}`, request);
  }

  remove(id: string) {
    return this.api.del<ResponseObject<boolean>>(`Discounts/${id}`);
  }

  getLimits() {
    return this.api.get<ResponseArray<DiscountLimitLevel>>('Settings/discount-limits');
  }

  saveLimits(payload: DiscountLimitPayload[]) {
    return this.api.put<ResponseObject<boolean>>('Settings/discount-limits', payload);
  }
}
