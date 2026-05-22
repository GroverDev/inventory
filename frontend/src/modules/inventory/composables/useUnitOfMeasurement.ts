import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type { UnitOfMeasurement } from '@/modules/inventory/models/unitOfMeasurement.model';

const { get, post, put, del } = useApi();

const useUnitOfMeasurement = () => {

  const getUnitsOfMeasurement = async (name: string = ''): Promise<ResponseArray<UnitOfMeasurement>> => {
    return await get<ResponseArray<UnitOfMeasurement>>(`UnitOfMeasurement?UnitOfMeasurementName=${name}`);
  }

  const getUnitOfMeasurementById = async (id: string): Promise<ResponseObject<UnitOfMeasurement>> => {
    return await get<ResponseObject<UnitOfMeasurement>>(`UnitOfMeasurement/${id}`);
  }

  const createUnitOfMeasurement = async (uom: UnitOfMeasurement): Promise<ResponseObject<string>> => {
    return await post<ResponseObject<string>>('UnitOfMeasurement', {
      name: uom.UnitName,
      proportion: uom.Proportion,
      precisionRounding: uom.PrecisionRounding,
      isLargeThanDefault: uom.IsLargeThanDefault,
      isDefault: uom.IsDefault,
      isActive: uom.IsActive,
    });
  }

  const updateUnitOfMeasurement = async (uom: UnitOfMeasurement): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`UnitOfMeasurement/${uom.Id}`, {
      id: uom.Id,
      name: uom.UnitName,
      proportion: uom.Proportion,
      precisionRounding: uom.PrecisionRounding,
      isLargeThanDefault: uom.IsLargeThanDefault,
      isDefault: uom.IsDefault,
      isActive: uom.IsActive,
    });
  }

  const deleteUnitOfMeasurement = async (id: string): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`UnitOfMeasurement/${id}`);
  }

  return { getUnitsOfMeasurement, getUnitOfMeasurementById, createUnitOfMeasurement, updateUnitOfMeasurement, deleteUnitOfMeasurement }
}
export default useUnitOfMeasurement;
