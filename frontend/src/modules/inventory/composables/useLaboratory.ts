import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type { Laboratory } from '@/modules/inventory/models/laboratory.model';

const { get, post, put, del } = useApi();

const useLaboratory = () => {

  const getLaboratories = async (name: string = ''): Promise<ResponseArray<Laboratory>> => {
    return await get<ResponseArray<Laboratory>>(`Laboratory?laboratoryName=${name}`);
  }

  const getLaboratoryById = async (id: string): Promise<ResponseObject<Laboratory>> => {
    return await get<ResponseObject<Laboratory>>(`Laboratory/${id}`);
  }

  const createLaboratory = async (lab: Laboratory): Promise<ResponseObject<string>> => {
    return await post<ResponseObject<string>>('Laboratory', {
      laboratoryName: lab.LaboratoryName,
      description: lab.Description,
      direction: lab.Direction,
      celular: lab.Celular,
      isActive: lab.IsActive,
    });
  }

  const updateLaboratory = async (lab: Laboratory): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Laboratory/${lab.Id}`, {
      id: lab.Id,
      laboratoryName: lab.LaboratoryName,
      description: lab.Description,
      direction: lab.Direction,
      celular: lab.Celular,
      isActive: lab.IsActive,
    });
  }

  const deleteLaboratory = async (id: string): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`Laboratory/${id}`);
  }

  return { getLaboratories, getLaboratoryById, createLaboratory, updateLaboratory, deleteLaboratory }
}
export default useLaboratory;
