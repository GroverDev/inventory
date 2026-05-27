import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseObject } from '@/modules/common/models/response.model';
import type { CashMovementRequest } from '../models/cashMovement.model';

const { post } = useApi();

const useCashMovement = () => {

  const addMovement = (sessionId: string, request: CashMovementRequest) =>
    post<ResponseObject<string>>(`CashSession/${sessionId}/movements`, request);

  return { addMovement };
};

export default useCashMovement;
