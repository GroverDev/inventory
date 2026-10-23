import { useApi } from '@/api/useApi';
import type { ResponseObject } from '@/models/response';
import type { SaleReturnRequest } from '../models/saleReturn.model';

const { post } = useApi();

const useSaleReturn = () => {
  const createReturn = async (request: SaleReturnRequest) =>
    await post<ResponseObject<string>>('SaleReturn', request);

  return { createReturn };
};

export default useSaleReturn;
