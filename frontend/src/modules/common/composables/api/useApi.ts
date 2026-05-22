import { ref } from 'vue';


import { getApi } from '@/modules/common/composables/api/getApi';
import { ResponseBase } from '@/modules/common/models';
import { useAuthStore } from '@/modules/auth/stores/auth.store';
import { useLoadingStore } from '@/modules/common/store/loadingStore';
import utils from '@/utils/msg';



class ApiConfig {
  showSuccessMessage?: boolean = false;
  customHeaders?: Record<string, string> = {};
  timeout?: number = 300000;
}

export const useApi = () => {
  //const $loading = useLoading();
  const isOnline = ref(navigator.onLine);
  const authStore = useAuthStore();
  const loading = useLoadingStore();
  const api = getApi();

  const apiConfig = new ApiConfig();
  apiConfig.showSuccessMessage = false;
  apiConfig.timeout = 300000;

  const apiCall = async <T extends ResponseBase>(
    endpoint: string,
    method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH' = 'GET',
    apiConfigRequest: ApiConfig = {},
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    body?: any
  ): Promise<T> => {
    const finalConfig = { ...apiConfig, ...apiConfigRequest };
    let response: T;

    //const loader = $loading.show(utils.configuracionLoading);
    try {
      loading.show();
      if (!isOnline.value) {
        response = {} as T;
        response.ok = false;

        utils.showMessageModal({
          Description: 'Error de conexión: Verifica tu acceso a internet.',
          MessageType: 'error'
        });

        return response!;
      }

      const headers = {
        'Authorization': `Bearer ${authStore.getToken}`,
        'Content-Type': 'application/json',
        ...finalConfig.customHeaders
      };

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      let apiResponse: any;
      const requestConfig = {
        headers,
        timeout: finalConfig.timeout
      };
      switch (method) {
        case 'GET':
          apiResponse = await api.get(endpoint, requestConfig);
          break;
        case 'POST':
          apiResponse = await api.post(endpoint, body, requestConfig);
          break;
        case 'PUT':
          apiResponse = await api.put(endpoint, body, requestConfig);
          break;
        case 'DELETE':
          apiResponse = await api.delete(endpoint, requestConfig);
          break;
        case 'PATCH':
          apiResponse = await api.patch(endpoint, body, requestConfig);
          break;
        default:
          throw new Error(`Método HTTP no soportado: ${method}`);
      }
      response = apiResponse.data as T;
      if (!response.ok) {
        utils.showMessageModal(response.Message);
      }

      //Mostrar mensaje de éxito si se especifica
      if (finalConfig.showSuccessMessage) {
        utils.showMessageModal(response.Message);
      }

    } catch (apiError) {
      response = {} as T;
      response.ok = false;
      utils.showErrorMessageApi(apiError);
    } finally {
      loading.hide();
    }

    return response!;
  }

  const get = async <T extends ResponseBase>(
    endpoint: string,
    config?: ApiConfig
  ): Promise<T> => await apiCall<T>(endpoint, 'GET', config);

  const post = <T extends ResponseBase>(
    endpoint: string,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    body: any,
    config?: ApiConfig
  ): Promise<T> => apiCall<T>(endpoint, 'POST', config, body);

  const put = <T extends ResponseBase>(
    endpoint: string,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    body: any,
    config?: ApiConfig
  ): Promise<T> => apiCall<T>(endpoint, 'PUT', config, body);

  const del = <T extends ResponseBase>(
    endpoint: string,
    config?: ApiConfig
  ): Promise<T> => apiCall<T>(endpoint, 'DELETE', config);

  const patch = <T extends ResponseBase>(
    endpoint: string,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    body: any,
    config?: ApiConfig
  ): Promise<T> => apiCall<T>(endpoint, 'PATCH', config, body);

  // Detectar cambios en la conexión
  const handleConnectionChange = () => {
    isOnline.value = navigator.onLine;
  };

  // Listeners para conexión
  window.addEventListener('online', handleConnectionChange);
  window.addEventListener('offline', handleConnectionChange);

  return {
    isOnline,

    get,
    post,
    put,
    del,
    patch,
  };
};
