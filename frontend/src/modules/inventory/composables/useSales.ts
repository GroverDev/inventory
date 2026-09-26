import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseObject } from '@/modules/common/models/response.model';
import type { Sale, SalesPagedResult } from '../models/sale.model';

const { get, post, del } = useApi();

const useSales = () => {

  /** Id con el que el servidor pide la firma de un supervisor por falta de stock. */
  const REQUIRES_SUPERVISOR_STOCK = 'requires-supervisor-stock';

  const saveSaleApi = async (sale: Sale) => {
    // Ese aviso lo maneja el punto de venta pidiendo el supervisor, no un modal de error.
    return await post<ResponseObject<string>>('Sales', sale, { silentMessageIds: [REQUIRES_SUPERVISOR_STOCK] });
  }

  /** branch: '' = sucursal activa, un id, o 'all' para el consolidado. */
  const getSales = async (dateInitial: string, dateEnd: string, page = 1, pageSize = 50, sellerName?: string, branch = '') => {
    const params = new URLSearchParams({
      saleDateInitial: dateInitial,
      saleDateEnd: dateEnd,
      page: String(page),
      pageSize: String(pageSize),
    });
    if (sellerName) params.append('sellerName', sellerName);
    if (branch) params.append('branch', branch);
    return await get<ResponseObject<SalesPagedResult>>(`Sales?${params}`);
  }

  const getSaleById = async (id: string) => {
    return await get<ResponseObject<Sale>>(`Sales/${id}`);
  }

  const deleteSale = async (id: string) => {
    return await del<ResponseObject<boolean>>(`Sales/${id}`);
  }

  return { REQUIRES_SUPERVISOR_STOCK, saveSaleApi, getSales, getSaleById, deleteSale }
}
export default useSales;
