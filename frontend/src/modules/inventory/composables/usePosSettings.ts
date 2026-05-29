import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseObject } from '@/modules/common/models/response.model';

export interface PosSettings {
  MaxCashierDiscountPct: number;
  MaxCashierDiscountAmount: number;
}

const { get } = useApi();

const usePosSettings = () => {
  const getPosSettings = () =>
    get<ResponseObject<PosSettings>>('Settings/pos');

  return { getPosSettings };
};

export default usePosSettings;
