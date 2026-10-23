import { useApi } from '@/api/useApi';
import type { ResponseObject } from '@/models/response';
import type { CashMovementRequest } from '../models/cashMovement.model';

const { post } = useApi();

const useCashMovement = () => {

  const addMovement = (sessionId: string, request: CashMovementRequest) =>
    post<ResponseObject<string>>(`CashSession/${sessionId}/movements`, request);

  return { addMovement };
};

export default useCashMovement;
