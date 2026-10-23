import router from '@/router'
import { useDialogStore } from '@/stores/dialog'
import { Message } from '@/models/response'

/** Primer detalle del ProblemDetails de ASP.NET, si lo hay. */
const primerErrorDeValidacion = (data: unknown): string | undefined => {
  const errores = (data as { errors?: Record<string, unknown> })?.errors
  if (!errores || typeof errores !== 'object') return undefined

  const primero = Object.values(errores)[0]
  if (Array.isArray(primero) && primero.length > 0) return String(primero[0])
  if (typeof primero === 'string') return primero
  return undefined
}

type MessageType = 'info' | 'error' | 'success' | 'warning' | 'question'

export default {
  async showErrorMessageApi(error: unknown, recargar: boolean = false): Promise<void> {
    const dialog = useDialogStore()
    let mensaje = 'Has experimentado un error técnico.'

    const axiosError = error as any

    if (axiosError?.response) {
      switch (axiosError.response.status) {
        case 400:
          // Dos formas de 400: la del backend viene envuelta en Response<T> con su
          // Message; la validación de modelo de ASP.NET responde un ProblemDetails
          // sin sobre, con el detalle real en `errors`.
          mensaje = axiosError.response.data?.Message?.Description
            || primerErrorDeValidacion(axiosError.response.data)
            || axiosError.response.data?.title
            || mensaje
          break
        case 401:
          await dialog.show({ message: 'Su sesión ha expirado. Por favor vuelva a iniciar sesión.', type: 'warning' })
          router.push({ name: 'login' })
          return
        case 404:
        case 500:
          mensaje = 'Has experimentado un error técnico. Pedimos disculpas.'
          break
        case 0:
          mensaje = 'El servicio no está disponible o no tiene conexión a Internet.'
          break
      }
    } else {
      mensaje = 'No hay respuesta del servidor o existen problemas de red.'
    }

    const confirmed = await dialog.show({ message: mensaje, type: 'error' })
    if (confirmed && recargar) location.reload()
  },

  /** Despliega el mensaje simple (con el ID del mensaje como título cuando existe). */
  async showMessage(mensaje: Message, redirect: string | null = null) {
    const dialog = useDialogStore()
    if (mensaje?.Description && mensaje.MessageType) {
      await dialog.show({
        message: mensaje.Description,
        type: mensaje.MessageType.toLowerCase() as MessageType,
        title: mensaje.Id && mensaje.Id !== '0' ? `ID: ${mensaje.Id}` : '',
      })
      if (redirect) router.push({ name: redirect })
    }
  },

  /** Despliega el mensaje modal. */
  async showMessageModal(mensaje: Message, redirect: string | null = null) {
    const dialog = useDialogStore()
    if (mensaje?.Description && mensaje.MessageType) {
      await dialog.show({
        message: mensaje.Description,
        type: mensaje.MessageType.toLowerCase() as MessageType,
      })
      if (redirect) router.push({ name: redirect })
    }
  },

  /** Despliega pregunta de confirmación. */
  async showMessageQuestion(mensaje: string) {
    const dialog = useDialogStore()
    return await dialog.show({
      message: mensaje,
      type: 'question',
      showCancel: true,
      confirmText: 'Aceptar',
      cancelText: 'Cancelar',
    })
  },
}
