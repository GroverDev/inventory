import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseObject } from '@/modules/common/models';

export interface ResetCompanyPayload {
  CurrentPassword: string;
  ConfirmationPhrase: string;
  SkipBackup: boolean;
}

const { post } = useApi();

const useAdmin = () => {

  const resetCompany = async (payload: ResetCompanyPayload): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>('Admin/ResetCompany', payload);
  };

  return { resetCompany };
};

export default useAdmin;
