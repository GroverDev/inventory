import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';

import { Message } from '../models/api.models';
import { DialogService, DialogType } from './dialog.service';

/** Primer detalle del ProblemDetails de ASP.NET, si lo hay. */
const primerErrorDeValidacion = (data: unknown): string | undefined => {
  const errores = (data as { errors?: Record<string, unknown> })?.errors;
  if (!errores || typeof errores !== 'object') return undefined;

  const primero = Object.values(errores)[0];
  if (Array.isArray(primero) && primero.length > 0) return String(primero[0]);
  if (typeof primero === 'string') return primero;
  return undefined;
};

/** Equivale a utils/msg.ts del frontend Vue. */
@Injectable({ providedIn: 'root' })
export class MsgService {
  private readonly dialog = inject(DialogService);
  private readonly router = inject(Router);

  async showErrorMessageApi(error: unknown, recargar = false): Promise<void> {
    let mensaje = 'Has experimentado un error técnico.';

    if (error instanceof HttpErrorResponse) {
      switch (error.status) {
        case 400:
          // La validación de modelo de ASP.NET responde un ProblemDetails sin
          // sobre; el detalle real está en `errors`.
          mensaje = error.error?.Message?.Description
            || primerErrorDeValidacion(error.error)
            || error.error?.title
            || mensaje;
          break;
        case 401:
          await this.dialog.show({
            message: 'Su sesión ha expirado. Por favor vuelva a iniciar sesión.',
            type: 'warning',
          });
          this.router.navigate(['/auth/login']);
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

    const confirmed = await this.dialog.show({ message: mensaje, type: 'error' });
    if (confirmed && recargar) location.reload();
  }

  /** Muestra el mensaje del backend; si trae `redirect`, navega luego. */
  async showMessage(mensaje: Message, redirect: string | null = null): Promise<void> {
    if (!mensaje?.Description || !mensaje?.MessageType) return;

    await this.dialog.show({
      message: mensaje.Description,
      type: mensaje.MessageType.toLowerCase() as DialogType,
      title: mensaje.Id && mensaje.Id !== '0' ? `ID: ${mensaje.Id}` : '',
    });
    if (redirect) this.router.navigateByUrl(redirect);
  }

  async showMessageModal(mensaje: Message, redirect: string | null = null): Promise<void> {
    if (!mensaje?.Description || !mensaje?.MessageType) return;

    await this.dialog.show({
      message: mensaje.Description,
      type: mensaje.MessageType.toLowerCase() as DialogType,
    });
    if (redirect) this.router.navigateByUrl(redirect);
  }

  showMessageQuestion(mensaje: string): Promise<boolean> {
    return this.dialog.show({
      message: mensaje,
      type: 'question',
      showCancel: true,
      confirmText: 'Aceptar',
      cancelText: 'Cancelar',
    });
  }
}
