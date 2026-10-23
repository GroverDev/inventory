import { Routes } from '@angular/router';

import { noAuthGuard } from '../core/guards/auth.guard';
import { AuthLayoutComponent } from './auth-layout/auth-layout.component';

export const ACCOUNT_ROUTES: Routes = [
  {
    path: '',
    component: AuthLayoutComponent,
    children: [
      {
        path: 'login',
        title: 'Punto de Venta - Inicio de sesión',
        // Solo en login, no en toda /auth: totp y totp-setup se visitan con un
        // token ya emitido (2FA pendiente) y el guard ahí lo empujaría a la app.
        canActivate: [noAuthGuard],
        loadComponent: () => import('./login/login.component').then(m => m.LoginComponent),
      },
      {
        path: 'totp',
        title: 'Punto de Venta - Verificación TOTP',
        loadComponent: () => import('./totp/totp.component').then(m => m.TotpComponent),
      },
      {
        path: 'totp-setup',
        title: 'Punto de Venta - Configurar TOTP',
        loadComponent: () => import('./totp-setup/totp-setup.component').then(m => m.TotpSetupComponent),
      },
      { path: '', pathMatch: 'full', redirectTo: 'login' },
    ],
  },
];
