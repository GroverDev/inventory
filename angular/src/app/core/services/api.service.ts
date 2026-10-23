import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, Injector, inject } from '@angular/core';
import { firstValueFrom, timeout } from 'rxjs';

import { ResponseBase } from '../models/api.models';
import { apiUrl } from './api-url';
import { AuthService } from './auth.service';
import { LoadingService } from './loading.service';
import { MsgService } from './msg.service';

export interface ApiConfig {
  showSuccessMessage?: boolean;
  customHeaders?: Record<string, string>;
  /** Milisegundos. Por defecto 5 minutos. */
  timeout?: number;
  /**
   * Ids de `Message` que quien llama maneja por su cuenta: la respuesta vuelve
   * igual, pero sin el modal genérico de error (p. ej. "falta la firma de un supervisor").
   */
  silentMessageIds?: string[];
}

type Method = 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';

/**
 * Capa de acceso a la API. Equivale a useApi.ts del frontend Vue: pantalla de
 * carga, detección offline, renovación del token ante un 401 y modal de error.
 * Nunca lanza: ante un fallo devuelve `{ ok: false }`.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly loading = inject(LoadingService);
  private readonly msg = inject(MsgService);
  // AuthService depende de ApiService; se resuelve al usarlo para no formar un ciclo.
  private readonly injector = inject(Injector);

  get<T extends ResponseBase>(endpoint: string, config?: ApiConfig): Promise<T> {
    return this.call<T>(endpoint, 'GET', config);
  }

  post<T extends ResponseBase>(endpoint: string, body: unknown, config?: ApiConfig): Promise<T> {
    return this.call<T>(endpoint, 'POST', config, body);
  }

  put<T extends ResponseBase>(endpoint: string, body: unknown, config?: ApiConfig): Promise<T> {
    return this.call<T>(endpoint, 'PUT', config, body);
  }

  del<T extends ResponseBase>(endpoint: string, config?: ApiConfig): Promise<T> {
    return this.call<T>(endpoint, 'DELETE', config);
  }

  patch<T extends ResponseBase>(endpoint: string, body: unknown, config?: ApiConfig): Promise<T> {
    return this.call<T>(endpoint, 'PATCH', config, body);
  }

  /** POST multipart (subida de archivos): el navegador fija el Content-Type con su boundary. */
  postForm<T extends ResponseBase>(endpoint: string, form: FormData, config?: ApiConfig): Promise<T> {
    return this.call<T>(endpoint, 'POST', config, form);
  }

  private async call<T extends ResponseBase>(
    endpoint: string,
    method: Method,
    config: ApiConfig = {},
    body?: unknown,
  ): Promise<T> {
    const failed = { ok: false } as T;

    this.loading.show();
    try {
      if (!navigator.onLine) {
        this.notify(() => this.msg.showMessageModal({
          Description: 'Error de conexión: Verifica tu acceso a internet.',
          MessageType: 'error',
        }));
        return failed;
      }

      let response: T;
      try {
        response = await this.send<T>(endpoint, method, config, body);
      } catch (err) {
        const auth = this.injector.get(AuthService);
        // El access token dura poco: si venció se renueva con la cookie y se
        // reintenta una sola vez. Sin token no hay nada que renovar (p. ej. un login fallido).
        if (!(err instanceof HttpErrorResponse) || err.status !== 401 || !auth.token()) throw err;

        if (!(await auth.tryRefreshSession())) {
          await auth.handleSessionExpired();
          return failed;
        }
        response = await this.send<T>(endpoint, method, config, body);
      }

      if (!response.ok && !config.silentMessageIds?.includes(response.Message?.Id ?? '')) {
        this.notify(() => this.msg.showMessageModal(response.Message));
      }
      if (config.showSuccessMessage) {
        this.notify(() => this.msg.showMessageModal(response.Message));
      }
      return response;
    } catch (apiError) {
      this.notify(() => this.msg.showErrorMessageApi(apiError));
      return failed;
    } finally {
      this.loading.hide();
    }
  }

  /**
   * Muestra un aviso sin esperar a que el usuario lo cierre (igual que useApi):
   * la pantalla de carga se oculta al terminar la llamada, no al cerrar el popup.
   */
  private notify(show: () => Promise<unknown>): void {
    void show();
  }

  private send<T>(endpoint: string, method: Method, config: ApiConfig, body?: unknown): Promise<T> {
    const request$ = this.http.request<T>(method, apiUrl(endpoint), {
      body,
      headers: config.customHeaders,
    });
    return firstValueFrom(request$.pipe(timeout(config.timeout ?? 300000)));
  }
}
