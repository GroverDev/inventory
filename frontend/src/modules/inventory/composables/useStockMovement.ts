import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type { StockMovementResponse, StockAdjustmentRequest } from '@/modules/inventory/models/stockMovement.model';

const { get, post } = useApi();

const useStockMovement = () => {

  const getMovementsByProduct = async (productId: string): Promise<ResponseArray<StockMovementResponse>> => {
    return await get<ResponseArray<StockMovementResponse>>(`StockMovement/${productId}`);
  };

  const createAdjustment = async (request: StockAdjustmentRequest): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>('StockMovement/adjust', request);
  };

  return { getMovementsByProduct, createAdjustment };
};

export default useStockMovement;
