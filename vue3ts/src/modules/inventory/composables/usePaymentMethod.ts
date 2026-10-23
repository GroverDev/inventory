import { useApi } from '@/api/useApi';
import type { ResponseArray } from '@/models/response';
import type { PaymentMethod } from '@/modules/inventory/models/paymentMethod.model';

const { get } = useApi();

const usePaymentMethod = () => {

  const getPaymentMethods = async (): Promise<ResponseArray<PaymentMethod>> => {
    return await get<ResponseArray<PaymentMethod>>('PaymentMethod');
  };

  return { getPaymentMethods };
};

export default usePaymentMethod;
