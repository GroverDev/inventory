import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type { StockTransfer, TransferLine } from '@/modules/inventory/models/stockTransfer.model';

const { get, post, put, del } = useApi();

const toRequest = (destBranchId: string, notes: string, lines: TransferLine[]) => ({
  destBranchId,
  notes,
  detail: lines.map(l => ({ productId: l.ProductId, quantity: l.Quantity })),
});

const useStockTransfer = () => {

  /** Fechas como días del calendario (yyyy-mm-dd). Los pendientes vienen siempre. */
  const getTransfers = async (dateFrom: string, dateTo: string, status = ''): Promise<ResponseArray<StockTransfer>> => {
    return await get<ResponseArray<StockTransfer>>(
      `StockTransfer?dateFrom=${dateFrom}&dateTo=${dateTo}&status=${status}`);
  }

  const getTransfer = async (id: string): Promise<ResponseObject<StockTransfer>> => {
    return await get<ResponseObject<StockTransfer>>(`StockTransfer/${id}`);
  }

  const createDraft = async (destBranchId: string, notes: string, lines: TransferLine[]): Promise<ResponseObject<string>> => {
    return await post<ResponseObject<string>>('StockTransfer', toRequest(destBranchId, notes, lines));
  }

  const updateDraft = async (id: string, destBranchId: string, notes: string, lines: TransferLine[]): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`StockTransfer/${id}`, toRequest(destBranchId, notes, lines));
  }

  const sendTransfer = async (id: string): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>(`StockTransfer/${id}/send`, {});
  }

  /** received: cantidad recibida por id de lote. Los que no figuran se reciben completos. */
  const receiveTransfer = async (id: string, received: Record<string, number>): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>(`StockTransfer/${id}/receive`, {
      lots: Object.entries(received).map(([lotId, quantityReceived]) => ({ lotId, quantityReceived })),
    });
  }

  const cancelTransfer = async (id: string): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`StockTransfer/${id}`);
  }

  return { getTransfers, getTransfer, createDraft, updateDraft, sendTransfer, receiveTransfer, cancelTransfer }
}
export default useStockTransfer;
