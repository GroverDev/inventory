import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type { Branch } from '@/modules/user-account/models/branch.model';

const { get, post, put, del } = useApi();

const useBranch = () => {

  const getBranches = async (name = '', includeInactive = false): Promise<ResponseArray<Branch>> => {
    return await get<ResponseArray<Branch>>(
      `Branch?name=${encodeURIComponent(name)}&includeInactive=${includeInactive}`);
  }

  const getBranchById = async (id: string): Promise<ResponseObject<Branch>> => {
    return await get<ResponseObject<Branch>>(`Branch/${id}`);
  }

  const createBranch = async (branch: Branch): Promise<ResponseObject<string>> => {
    return await post<ResponseObject<string>>('Branch', {
      name: branch.Name,
      code: branch.Code,
      address: branch.Address,
      phone: branch.Phone,
      isActive: branch.IsActive,
    });
  }

  const updateBranch = async (branch: Branch): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Branch/${branch.Id}`, {
      id: branch.Id,
      name: branch.Name,
      code: branch.Code,
      address: branch.Address,
      phone: branch.Phone,
      isActive: branch.IsActive,
    });
  }

  const deleteBranch = async (id: string): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`Branch/${id}`);
  }

  return { getBranches, getBranchById, createBranch, updateBranch, deleteBranch }
}
export default useBranch;
