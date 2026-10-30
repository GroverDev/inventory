import { NgModule } from '@angular/core';
import { Route, RouterModule, Routes } from '@angular/router';

import { LayoutComponent } from './layouts/layout.component';
import { authGuard } from './core/guards/auth.guard';
import { PosLayoutComponent } from './features/inventory/pos/pos-layout.component';
import { posLeaveGuard } from './features/inventory/pos/pos-leave.guard';

/**
 * El punto de venta va a pantalla completa, fuera del layout con menú. `point-sale-admin`
 * es la ruta del menú (coincide con `url` del formulario) y `pos` es el alias del Vue.
 */
const posRoute = (path: string): Route => ({
  path,
  component: PosLayoutComponent,
  canActivate: [authGuard],
  children: [{
    path: '',
    title: 'PV - Punto de venta',
    canDeactivate: [posLeaveGuard],
    loadComponent: () => import('./features/inventory/pos/pos.component').then(m => m.PosComponent),
  }],
});

const routes: Routes = [
  // Antes que '': el '**' de las rutas hijas de '' también casa con /auth/*, y con el guard
  // fallando redirigiría a /auth/login una y otra vez.
  { path: 'auth', loadChildren: () => import('./account/account.routes').then(m => m.ACCOUNT_ROUTES) },
  posRoute('point-sale-admin'),
  posRoute('pos'),
  {
    path: '',
    component: LayoutComponent,
    loadChildren: () => import('./pages/pages.module').then(m => m.PagesModule),
    canActivate: [authGuard],
  },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
