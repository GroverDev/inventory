import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type { StockMovementResponse, StockAdjustmentRequest, StockExpiryResponse, LotTraceabilityResponse, StockSerialResponse } from '@/modules/inventory/models/stockMovement.model';

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

  /**
   * A quién se le vendió un lote. El servidor compara sin distinguir mayúsculas
   * ni espacios: en un retiro el código llega dictado o copiado de un correo.
   */
  const getTraceability = async (lote: string): Promise<ResponseArray<LotTraceabilityResponse>> => {
    return await get<ResponseArray<LotTraceabilityResponse>>(
      `StockMovement/traceability?lote=${encodeURIComponent(lote)}`);
  };

  /** Unidades serializadas disponibles, para que el mostrador elija cuál entrega. */
  const getAvailableSerials = async (productId: string): Promise<ResponseArray<StockSerialResponse>> => {
    return await get<ResponseArray<StockSerialResponse>>(`StockMovement/serials/${productId}`);
  };

  return { getMovementsByProduct, createAdjustment, getExpiring, getTraceability, getAvailableSerials };
};

export default useStockMovement;
