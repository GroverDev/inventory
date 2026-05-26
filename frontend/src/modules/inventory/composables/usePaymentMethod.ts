import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray } from '@/modules/common/models/response.model';
import type { PaymentMethod } from '@/modules/inventory/models/paymentMethod.model';

const { get } = useApi();

const usePaymentMethod = () => {

  const getPaymentMethods = async (): Promise<ResponseArray<PaymentMethod>> => {
    return await get<ResponseArray<PaymentMethod>>('PaymentMethod');
  };

  return { getPaymentMethods };
};

export default usePaymentMethod;
