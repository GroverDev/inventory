import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type { StockMovementResponse, StockAdjustmentRequest, StockExpiryResponse, LotTraceabilityResponse, StockSerialResponse, StockWriteOffRequest, WriteOffReportResponse } from '@/modules/inventory/models/stockMovement.model';

const { get, post } = useApi();

const useStockMovement = () => {

  /** Sin `stockItemId`: historial completo del producto. Con él: kardex de un lote puntual. */
  const getMovementsByProduct = async (productId: string, stockItemId?: string): Promise<ResponseArray<StockMovementResponse>> => {
    const query = stockItemId ? `?stockItemId=${encodeURIComponent(stockItemId)}` : '';
    return await get<ResponseArray<StockMovementResponse>>(`StockMovement/${productId}${query}`);
  };

  const createAdjustment = async (request: StockAdjustmentRequest): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>('StockMovement/adjust', request);
  };

  /** Dar de baja una existencia puntual (lote vencido/dañado/retirado). */
  const createWriteOff = async (request: StockWriteOffRequest): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>('StockMovement/write-off', request);
  };

  /** Reporte de mermas por vencimiento: cuánto se perdió en el período, por producto y en detalle. */
  const getWriteOffs = async (desde: string, hasta: string, productId?: string): Promise<ResponseObject<WriteOffReportResponse>> => {
    const query = productId ? `&productId=${encodeURIComponent(productId)}` : '';
    return await get<ResponseObject<WriteOffReportResponse>>(
      `StockMovement/write-offs?desde=${desde}&hasta=${hasta}${query}`);
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

  return { getMovementsByProduct, createAdjustment, createWriteOff, getWriteOffs, getExpiring, getTraceability, getAvailableSerials };
};

export default useStockMovement;
