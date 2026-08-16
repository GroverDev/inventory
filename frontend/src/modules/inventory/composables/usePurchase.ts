import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type { Purchase, PurchaseDelivery, PurchaseStatus } from '@/modules/inventory/models/purchase.model';

const { get, post, put, del } = useApi();

const usePurchase = () => {

  const getPurchases = async (
    dateInitial: string,
    dateEnd: string,
    statusId: number
  ): Promise<ResponseArray<Purchase>> => {
    return await get<ResponseArray<Purchase>>(
      `Purchases?purchaseDateInitial=${dateInitial}&purchaseDateEnd=${dateEnd}&purchaseStatus=${statusId}`
    );
  }

  const getPurchaseById = async (id: string): Promise<ResponseObject<Purchase>> => {
    return await get<ResponseObject<Purchase>>(`Purchases/${id}`);
  }

  const createPurchase = async (purchase: Purchase): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>('Purchases', {
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

  const updatePurchase = async (purchase: Purchase): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Purchases/${purchase.Id}`, {
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

  const deletePurchase = async (id: string): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`Purchases/${id}`);
  }

  const receivePurchase = async (purchaseId: string, delivery: PurchaseDelivery): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Purchases/reciveOrders/${purchaseId}`, {
      purchaseId: purchaseId,
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
  const closePurchase = async (id: string): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Purchases/close/${id}`, {});
  }

  /** Anula una orden que todavía no recibió mercadería. */
  const cancelPurchase = async (id: string): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Purchases/cancel/${id}`, {});
  }

  const getPurchaseStatuses = async (): Promise<ResponseArray<PurchaseStatus>> => {
    return await get<ResponseArray<PurchaseStatus>>('PurchaseStatus');
  }

  return {
    getPurchases,
    getPurchaseById,
    createPurchase,
    updatePurchase,
    deletePurchase,
    receivePurchase,
    closePurchase,
    cancelPurchase,
    getPurchaseStatuses,
  }
}
export default usePurchase;
