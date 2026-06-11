import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseObject } from '@/modules/common/models/response.model';
import type { Sale, SalesPagedResult } from '../models/sale.model';

const { get, post, del } = useApi();

const useSales = () => {

  const saveSaleApi = async (sale: Sale) => {
    return await post<ResponseObject<string>>('Sales', sale);
  }

  const getSales = async (dateInitial: string, dateEnd: string, page = 1, pageSize = 50, sellerName?: string) => {
    const params = new URLSearchParams({
      saleDateInitial: dateInitial,
      saleDateEnd: dateEnd,
      page: String(page),
      pageSize: String(pageSize),
    });
    if (sellerName) params.append('sellerName', sellerName);
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
