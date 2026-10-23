import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from '../services/auth.service';

// Las dependencias se inyectan antes de cualquier `await`: pasado el primer
// await ya no hay contexto de inyección.

/**
 * Exige sesión. En una pestaña nueva con sesión recordada el token no está en
 * disco, así que se renueva en silencio con la cookie antes de decidir.
 */
export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.token()) await auth.restoreSession();

  return auth.token() ? true : router.createUrlTree(['/auth/login']);
};

/**
 * Si ya hay sesión, no tiene sentido mostrar el login: manda a la app. Se usa
 * solo en login; totp y totp-setup se visitan con un token ya emitido (2FA
 * obligatorio pendiente) y el guard ahí empujaría de vuelta a quien está a
 * mitad de esa configuración.
 */
export const noAuthGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.token()) await auth.restoreSession();

  return auth.token() ? router.createUrlTree(['/']) : true;
};
