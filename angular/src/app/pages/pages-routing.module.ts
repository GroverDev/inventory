import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { CATALOG_ROUTES } from '../features/catalogs/catalogs.routes';
import { INVENTORY_ROUTES } from '../features/inventory/inventory.routes';
import { ACCOUNT_ROUTES } from '../features/account/account.routes';

const routes: Routes = [
  {
    path: '',
    title: 'Gestión de Inventario',
    loadComponent: () => import('../features/dashboard/dashboard.component').then(m => m.DashboardComponent),
  },
  ...CATALOG_ROUTES,
  ...INVENTORY_ROUTES,
  ...ACCOUNT_ROUTES,
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PagesRoutingModule { }
