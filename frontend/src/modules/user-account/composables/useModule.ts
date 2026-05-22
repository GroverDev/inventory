import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models';
import type { Module } from '@/modules/user-account/models/module.model';

const { post, get, put, del } = useApi();

const useModule = () => {

  const getModules = async (nameModule: string): Promise<ResponseArray<Module>> => {
    return await get<ResponseArray<Module>>(`Modules?nameModule=${nameModule}`);
  }

  const getModuleById = async (id: number): Promise<ResponseObject<Module>> => {
    return await get<ResponseObject<Module>>(`Modules/${id}`);
  }

  const createModule = async (module: Module): Promise<ResponseObject<number>> => {
    return await post<ResponseObject<number>>('Modules', {
        nameModule: module.NameModule,
        showOrder: module.ShowOrder,
        route: module.Route,
        iconCss: module.IconCss
    });
  }

  const updateModule = async (module: Module): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Modules/${module.Id}`, {
        id: module.Id,
        nameModule: module.NameModule,
        showOrder: module.ShowOrder,
        route: module.Route,
        iconCss: module.IconCss
    });
  }

  const deleteModule = async (id: number): Promise<ResponseObject<boolean>> => {
      return await del<ResponseObject<boolean>>(`Modules/${id}`);
  }

  return { getModules, getModuleById, createModule, updateModule, deleteModule }
}
export default useModule;
