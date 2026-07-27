import { ref } from 'vue';


import { getApi } from '@/modules/common/composables/api/getApi';
import { handleSessionExpired, tryRefreshSession } from '@/modules/common/composables/api/refreshSession';
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
      const send = (authHeaders: Record<string, string>): Promise<any> => {
        const requestConfig = {
          headers: authHeaders,
          timeout: finalConfig.timeout
        };
        switch (method) {
          case 'GET':
            return api.get(endpoint, requestConfig);
          case 'POST':
            return api.post(endpoint, body, requestConfig);
          case 'PUT':
            return api.put(endpoint, body, requestConfig);
          case 'DELETE':
            return api.delete(endpoint, requestConfig);
          case 'PATCH':
            return api.patch(endpoint, body, requestConfig);
          default:
            throw new Error(`Método HTTP no soportado: ${method}`);
        }
      };

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      let apiResponse: any;
      try {
        apiResponse = await send(headers);
      } catch (err) {
        // El access token dura poco: si venció se renueva con la cookie y se
        // reintenta una sola vez, sin que el usuario note nada.
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        if ((err as any)?.response?.status !== 401) throw err;

        if (!(await tryRefreshSession())) {
          // La sesión no se puede recuperar: se manda al login sin mostrar el
          // modal de error genérico, que solo sería ruido encima del redirect.
          await handleSessionExpired();
          response = {} as T;
          response.ok = false;
          return response;
        }

        apiResponse = await send({
          ...headers,
          'Authorization': `Bearer ${authStore.getToken}`
        });
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
