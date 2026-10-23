import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';

/** Topes del descuento manual vigentes en la sucursal activa. */
export interface PosSettings {
  /** Tope del cajero: por encima pide autorización de supervisor. */
  MaxCashierDiscountPct: number;
  MaxCashierDiscountAmount: number;
  /** Tope máximo para cualquier rol, ni con autorización. Ausente = sin tope. */
  MaxDiscountPct?: number | null;
  MaxDiscountAmount?: number | null;
}

@Injectable({ providedIn: 'root' })
export class PosSettingsService {
  private readonly api = inject(ApiService);

  get() {
    return this.api.get<ResponseObject<PosSettings>>('Settings/pos');
  }
}

/** Venta en espera, como la lista el servidor. */
export interface HeldSale {
  Id: string;
  Label: string;
  CustomerName: string;
  UserName: string;
  Created: string;
  ItemsCount: number;
  Total: number;
  /** Solo al retomarla: el carrito guardado, en JSON. */
  Payload?: string;
}

@Injectable({ providedIn: 'root' })
export class HeldSaleService {
  private readonly api = inject(ApiService);

  getAll() {
    return this.api.get<ResponseArray<HeldSale>>('HeldSale');
  }

  hold(label: string, customerId: string | null, itemsCount: number, total: number, payload: unknown) {
    return this.api.post<ResponseObject<string>>('HeldSale', {
      label, customerId, itemsCount, total, payload: JSON.stringify(payload),
    });
  }

  /** La saca de la espera y devuelve su carrito. Falla si otra caja la retomó antes. */
  take(id: string) {
    return this.api.post<ResponseObject<HeldSale>>(`HeldSale/${id}/take`, {});
  }

  discard(id: string) {
    return this.api.del<ResponseObject<boolean>>(`HeldSale/${id}`);
  }
}
