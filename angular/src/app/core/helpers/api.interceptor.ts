import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

/**
 * Adjunta el Bearer a las llamadas a la API y envía la cookie HttpOnly de
 * refresh (`withCredentials`), necesaria al ser la API otro origen. La
 * renovación ante un 401 la resuelve ApiService, no este interceptor.
 */
export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.apiUrl)) return next(req);

  // El refresh se hace con la cookie; mandar el Bearer vencido no aporta nada.
  const token = req.url.endsWith('Login/refresh') ? null : inject(AuthService).token();
  return next(req.clone({
    withCredentials: true,
    setHeaders: token ? { Authorization: `Bearer ${token}` } : {},
  }));
};
