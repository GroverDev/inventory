import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type { StockMovementResponse, StockAdjustmentRequest, StockExpiryResponse } from '@/modules/inventory/models/stockMovement.model';

const { get, post } = useApi();

const useStockMovement = () => {

  const getMovementsByProduct = async (productId: string): Promise<ResponseArray<StockMovementResponse>> => {
    return await get<ResponseArray<StockMovementResponse>>(`StockMovement/${productId}`);
  };

  const createAdjustment = async (request: StockAdjustmentRequest): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>('StockMovement/adjust', request);
  };

  /**
   * Existencias por vencer dentro de `dias`, de la más urgente a la menos.
   * Con `dias = 0` el servidor devuelve todas las que tienen vencimiento.
   */
  const getExpiring = async (dias: number): Promise<ResponseArray<StockExpiryResponse>> => {
    return await get<ResponseArray<StockExpiryResponse>>(`StockMovement/expiring?dias=${dias}`);
  };

  return { getMovementsByProduct, createAdjustment, getExpiring };
};

export default useStockMovement;
