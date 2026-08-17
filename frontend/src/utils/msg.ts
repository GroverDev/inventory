import router from '@/router';
import { useDialogStore } from '@/stores/dialogStore';
import { Message } from '@/modules/common/models/message.model';

/** Primer detalle del ProblemDetails de ASP.NET, si lo hay. */
const primerErrorDeValidacion = (data: unknown): string | undefined => {
  const errores = (data as { errors?: Record<string, unknown> })?.errors;
  if (!errores || typeof errores !== 'object') return undefined;

  const primero = Object.values(errores)[0];
  if (Array.isArray(primero) && primero.length > 0) return String(primero[0]);
  if (typeof primero === 'string') return primero;
  return undefined;
};

export default {
  async showErrorMessageApi(error: unknown, recargar: boolean = false): Promise<void> {
    const dialog = useDialogStore();
    let mensaje = 'Has experimentado un error técnico.';
    let type: 'error' | 'warning' = 'error';

    const axiosError = error as any;

    if (axiosError?.response) {
      switch (axiosError.response.status) {
        case 400:
          // Dos formas distintas de 400. La del backend viene envuelta en
          // Response<T> con su Message; la validación de modelo de ASP.NET
          // responde un ProblemDetails, sin sobre, y el detalle real está en
          // `errors`. Sin leerlo, un campo mal enviado se veía como
          // "Has experimentado un error técnico" y no había forma de saber cuál.
          mensaje = axiosError.response.data?.Message?.Description
                 || primerErrorDeValidacion(axiosError.response.data)
                 || axiosError.response.data?.title
                 || mensaje;
          break;
        case 401:
          mensaje = 'Su sesión ha expirado. Por favor vuelva a iniciar sesión.';
          type = 'warning';
          await dialog.show({ message: mensaje, type });
          router.push({ name: 'login' });
          return;
        case 404:
        case 500:
          mensaje = 'Has experimentado un error técnico. Pedimos disculpas.';
          break;
        case 0:
          mensaje = 'El servicio no está disponible o no tiene conexión a Internet.';
          break;
      }
    } else {
      mensaje = 'No hay respuesta del servidor o existen problemas de red.';
    }

    const confirmed = await dialog.show({ message: mensaje, type: 'error' });
    if (confirmed && recargar) {
      location.reload();
    }
  },

  /**
   * Despliega el mensaje simple.
   */
  async showMessage(mensaje: Message, redirect: string | null = null) {
    const dialog = useDialogStore();
    if (mensaje.Description && mensaje.MessageType) {
      const typeStr = mensaje.MessageType.toLowerCase();
      const type = (typeStr === 'info' ? 'info' : typeStr) as 'info' | 'error' | 'success' | 'warning' | 'question';
      
      await dialog.show({
        message: mensaje.Description,
        type: type,
        title: mensaje.Id && mensaje.Id !== '0' ? `ID: ${mensaje.Id}` : ''
      });
      if (redirect) router.push({ name: redirect as any });
    }
  },

  /**
   * Despliega el mensaje modal.
   */
  async showMessageModal(mensaje: Message, redirect: string | null = null) {
    const dialog = useDialogStore();
    if (mensaje.Description && mensaje.MessageType) {
      const typeStr = mensaje.MessageType.toLowerCase();
       const type = (typeStr === 'info' ? 'info' : typeStr) as 'info' | 'error' | 'success' | 'warning' | 'question';

      await dialog.show({
        message: mensaje.Description,
        type: type
      });
      if (redirect) router.push(redirect);
    }
  },

  /**
   * Despliega pregunta de confirmación.
   */
  async showMessageQuestion(mensaje: string) {
    const dialog = useDialogStore();
    return await dialog.show({
      message: mensaje,
      type: 'question',
      showCancel: true,
      confirmText: 'Aceptar',
      cancelText: 'Cancelar'
    });
  },

  configuracionLoading: {
    canCancel: false,
    zIndex: 99999999999,
    color: '#00619C',
    backgroundColor: '#00619C',
    opacity: 0.1,
  },
};
