import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import {
  LotTraceabilityResponse, StockAdjustmentRequest, StockExpiryResponse, StockMovementResponse,
  StockSerialResponse, StockWriteOffRequest, WriteOffReportResponse,
} from './stock-movement.model';

@Injectable({ providedIn: 'root' })
export class StockMovementService {
  private readonly api = inject(ApiService);

  /** Sin `stockItemId`: historial completo del producto. Con él: kardex de un lote puntual. */
  getByProduct(productId: string, stockItemId?: string) {
    const query = stockItemId ? `?stockItemId=${encodeURIComponent(stockItemId)}` : '';
    return this.api.get<ResponseArray<StockMovementResponse>>(`StockMovement/${productId}${query}`);
  }

  createAdjustment(request: StockAdjustmentRequest) {
    return this.api.post<ResponseObject<boolean>>('StockMovement/adjust', request);
  }

  /** Dar de baja una existencia puntual (lote vencido/dañado/retirado). */
  createWriteOff(request: StockWriteOffRequest) {
    return this.api.post<ResponseObject<boolean>>('StockMovement/write-off', request);
  }

  /**
   * Reporte de mermas por vencimiento: cuánto se perdió en el período, por producto y en detalle.
   * `branch`: '' = sucursal activa, un id, o 'all' para el consolidado.
   */
  getWriteOffs(desde: string, hasta: string, productId?: string, branch = '') {
    const query = (productId ? `&productId=${encodeURIComponent(productId)}` : '')
      + (branch ? `&branch=${encodeURIComponent(branch)}` : '');
    return this.api.get<ResponseObject<WriteOffReportResponse>>(`StockMovement/write-offs?desde=${desde}&hasta=${hasta}${query}`);
  }

  /**
   * Existencias por vencer dentro de `dias`, de la más urgente a la menos.
   * Con `dias = 0` el servidor devuelve todas las que tienen vencimiento.
   */
  getExpiring(dias: number) {
    return this.api.get<ResponseArray<StockExpiryResponse>>(`StockMovement/expiring?dias=${dias}`);
  }

  /**
   * A quién se le vendió un lote. El servidor compara sin distinguir mayúsculas
   * ni espacios: en un retiro el código llega dictado o copiado de un correo.
   */
  getTraceability(lote: string) {
    return this.api.get<ResponseArray<LotTraceabilityResponse>>(`StockMovement/traceability?lote=${encodeURIComponent(lote)}`);
  }

  /** Unidades serializadas disponibles, para que el mostrador elija cuál entrega. */
  getAvailableSerials(productId: string) {
    return this.api.get<ResponseArray<StockSerialResponse>>(`StockMovement/serials/${productId}`);
  }
}
