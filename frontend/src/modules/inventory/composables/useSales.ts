import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseObject, ResponseArray } from '@/modules/common/models/response.model';
import type { Sale } from '../models/sale.model';

const { get, post, del } = useApi();

const useSales = () => {

  const saveSaleApi = async (sale: Sale) => {
    return await post<ResponseObject<string>>('Sales', sale);
  }

  const getSales = async (dateInitial: string, dateEnd: string) => {
    return await get<ResponseArray<Sale>>(`Sales?saleDateInitial=${dateInitial}&saleDateEnd=${dateEnd}`);
  }

  const getSaleById = async (id: string) => {
    return await get<ResponseObject<Sale>>(`Sales/${id}`);  // returns SaleProductResponse shape, compatible with Sale+CustomerName+Detail
  }

  const deleteSale = async (id: string) => {
    return await del<ResponseObject<boolean>>(`Sales/${id}`);
  }

  return { saveSaleApi, getSales, getSaleById, deleteSale }
}
export default useSales;
