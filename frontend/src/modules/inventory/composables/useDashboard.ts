import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseObject } from '@/modules/common/models/response.model';
import type { DashboardKpi } from '../models/dashboard.model';

const useDashboard = () => {
  const { get } = useApi();

  const getDashboard = async () => {
    return await get<ResponseObject<DashboardKpi>>('Dashboard');
  };

  return { getDashboard };
};

export default useDashboard;
