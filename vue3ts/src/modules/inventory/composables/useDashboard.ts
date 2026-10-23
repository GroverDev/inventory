import { useApi } from '@/api/useApi';
import type { ResponseObject } from '@/models/response';
import type { DashboardKpi } from '../models/dashboard.model';

const useDashboard = () => {
  const { get } = useApi();

  const getDashboard = async () => {
    return await get<ResponseObject<DashboardKpi>>('Dashboard');
  };

  return { getDashboard };
};

export default useDashboard;
