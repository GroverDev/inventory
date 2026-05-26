import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseObject } from '@/modules/common/models/response.model';
import type { SaleReturnRequest } from '../models/saleReturn.model';

const { post } = useApi();

const useSaleReturn = () => {
  const createReturn = async (request: SaleReturnRequest) =>
    await post<ResponseObject<string>>('SaleReturn', request);

  return { createReturn };
};

export default useSaleReturn;
