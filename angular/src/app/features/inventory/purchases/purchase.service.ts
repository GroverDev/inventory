import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import { Purchase, PurchaseDelivery, PurchaseStatus } from './purchase.model';

@Injectable({ providedIn: 'root' })
export class PurchaseService {
  private readonly api = inject(ApiService);

  /** `branch`: '' = sucursal activa, un id, o 'all' para el consolidado. */
  getAll(dateInitial: string, dateEnd: string, statusId: number, branch = '') {
    return this.api.get<ResponseArray<Purchase>>(
      `Purchases?purchaseDateInitial=${dateInitial}&purchaseDateEnd=${dateEnd}&purchaseStatus=${statusId}`
      + (branch ? `&branch=${encodeURIComponent(branch)}` : ''));
  }

  getById(id: string) {
    return this.api.get<ResponseObject<Purchase>>(`Purchases/${id}`);
  }

  create(purchase: Purchase) {
    return this.api.post<ResponseObject<boolean>>('Purchases', {
      purchaseDate: purchase.PurchaseDate,
      total: purchase.Total,
      isActive: purchase.IsActive,
      providerId: purchase.ProviderId,
      estimatedDeliveryDate: purchase.EstimatedDeliveryDate,
      purchaseStatusId: purchase.PurchaseStatusId,
      detail: purchase.Detail.map(d => ({
        productId: d.ProductId,
        orderUnitPrice: d.OrderUnitPrice,
        orderedQuantity: d.OrderedQuantity,
        orderFinalPrice: d.OrderFinalPrice,
        purchaseStatusId: d.PurchaseStatusId,
      })),
    });
  }

  update(purchase: Purchase) {
    return this.api.put<ResponseObject<boolean>>(`Purchases/${purchase.Id}`, {
      id: purchase.Id,
      purchaseDate: purchase.PurchaseDate,
      total: purchase.Total,
      isActive: purchase.IsActive,
      providerId: purchase.ProviderId,
      estimatedDeliveryDate: purchase.EstimatedDeliveryDate,
      purchaseStatusId: purchase.PurchaseStatusId,
      detail: purchase.Detail.map(d => ({
        id: d.Id,
        purchaseId: d.PurchaseId,
        productId: d.ProductId,
        orderUnitPrice: d.OrderUnitPrice,
        orderedQuantity: d.OrderedQuantity,
        orderFinalPrice: d.OrderFinalPrice,
        purchaseStatusId: d.PurchaseStatusId,
      })),
    });
  }

  remove(id: string) {
    return this.api.del<ResponseObject<boolean>>(`Purchases/${id}`);
  }

  receive(purchaseId: string, delivery: PurchaseDelivery) {
    return this.api.put<ResponseObject<boolean>>(`Purchases/reciveOrders/${purchaseId}`, {
      purchaseId,
      isActive: delivery.IsActive,
      deliveryDate: delivery.DeliveryDate,
      operationUid: delivery.OperationUid,
      // Solo las líneas con mercadería recibida. El estado resultante lo deriva
      // el servidor de los saldos: no se envía.
      detail: delivery.Detail
        .filter(d => d.DeliveryQuantity > 0)
        .map(d => ({
          productId: d.ProductId,
          deliveryQuantity: d.DeliveryQuantity,
          unitPrice: d.UnitPrice,
          // Van siempre, no solo cuando el producto usa lotes: el servidor
          // ignora los vacíos y los exige cuando corresponde. Este payload se
          // arma campo por campo, así que lo que no se nombre acá se pierde en
          // silencio por más que el modelo y la pantalla lo tengan.
          lotCode: d.LotCode || null,
          expiryDate: d.ExpiryDate || null,
          serialNumbers: d.SerialNumbers.length > 0 ? d.SerialNumbers : null,
        })),
    });
  }

  /** Cierra con faltante una orden parcialmente recibida. */
  close(id: string) {
    return this.api.put<ResponseObject<boolean>>(`Purchases/close/${id}`, {});
  }

  /** Anula una orden que todavía no recibió mercadería. */
  cancel(id: string) {
    return this.api.put<ResponseObject<boolean>>(`Purchases/cancel/${id}`, {});
  }

  getStatuses() {
    return this.api.get<ResponseArray<PurchaseStatus>>('PurchaseStatus');
  }
}
