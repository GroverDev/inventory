import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseObject } from '@/modules/common/models/response.model';
import type { Sale, SalesPagedResult } from '../models/sale.model';

const { get, post, del } = useApi();

const useSales = () => {

  const saveSaleApi = async (sale: Sale) => {
    return await post<ResponseObject<string>>('Sales', sale);
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

  return { saveSaleApi, getSales, getSaleById, deleteSale }
}
export default useSales;
