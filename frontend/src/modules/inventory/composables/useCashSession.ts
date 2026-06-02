import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseObject, ResponseArray } from '@/modules/common/models/response.model';
import type { CashSession, OpenCashSessionRequest, CloseCashSessionRequest, SessionSale } from '../models/cashSession.model';

const { get, post, put } = useApi();

const useCashSession = () => {

  const getActiveSession = () =>
    get<ResponseObject<CashSession>>('CashSession/active');

  const getSessionById = (id: string) =>
    get<ResponseObject<CashSession>>(`CashSession/${id}`);

  const getSessions = (dateFrom: string, dateTo: string) =>
    get<ResponseArray<CashSession>>(`CashSession?dateFrom=${dateFrom}&dateTo=${dateTo}`);

  const openSession = (request: OpenCashSessionRequest) =>
    post<ResponseObject<string>>('CashSession/open', request);

  const closeSession = (id: string, request: CloseCashSessionRequest) =>
    put<ResponseObject<CashSession>>(`CashSession/${id}/close`, request);

  const getSessionSales = (id: string) =>
    get<ResponseArray<SessionSale>>(`CashSession/${id}/sales`);

  return { getActiveSession, getSessionById, getSessions, openSession, closeSession, getSessionSales };
};

export default useCashSession;
