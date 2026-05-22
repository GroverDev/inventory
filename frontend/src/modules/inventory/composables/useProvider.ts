import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models/response.model';
import type { Provider } from '@/modules/inventory/models/provider.model';

const { get, post, put, del } = useApi();

const useProvider = () => {

  const getProviders = async (name: string = ''): Promise<ResponseArray<Provider>> => {
    return await get<ResponseArray<Provider>>(`Provider?providerName=${name}`);
  }

  const getProviderById = async (id: string): Promise<ResponseObject<Provider>> => {
    return await get<ResponseObject<Provider>>(`Provider/${id}`);
  }

  const createProvider = async (provider: Provider): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>('Provider', {
      providerName: provider.ProviderName,
      description: provider.Description,
      direction: provider.Direction,
      celular: provider.Celular,
    });
  }

  const updateProvider = async (provider: Provider): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Provider/${provider.Id}`, {
      id: provider.Id,
      providerName: provider.ProviderName,
      description: provider.Description,
      direction: provider.Direction,
      celular: provider.Celular,
    });
  }

  const deleteProvider = async (id: string): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`Provider/${id}`);
  }

  return { getProviders, getProviderById, createProvider, updateProvider, deleteProvider }
}
export default useProvider;
