import { Route } from '@angular/router';

/** Rutas planas, con el mismo nombre que las del frontend Vue (coinciden con `url` del menú). */
export const ACCOUNT_ROUTES: Route[] = [
  {
    path: 'users-admin',
    title: 'PV - Registro de Usuarios',
    loadComponent: () => import('./users/users-admin.component').then(m => m.UsersAdminComponent),
  },
  {
    path: 'user-edit/:id',
    title: 'PV - Registro de Usuarios',
    loadComponent: () => import('./users/user-edit.component').then(m => m.UserEditComponent),
  },
  {
    path: 'roles-admin',
    title: 'PV - Registro de Roles',
    loadComponent: () => import('./roles/roles-admin.component').then(m => m.RolesAdminComponent),
  },
  {
    path: 'role-edit/:id',
    title: 'PV - Registro de Roles',
    loadComponent: () => import('./roles/role-edit.component').then(m => m.RoleEditComponent),
  },
  {
    path: 'forms-admin',
    title: 'PV - Registro de Formularios',
    loadComponent: () => import('./forms/forms-admin.component').then(m => m.FormsAdminComponent),
  },
  {
    path: 'form-edit/:id',
    title: 'PV - Registro de Formularios',
    loadComponent: () => import('./forms/form-edit.component').then(m => m.FormEditComponent),
  },
  {
    path: 'modules-admin',
    title: 'PV - Registro de Módulos',
    loadComponent: () => import('./modules/modules-admin.component').then(m => m.ModulesAdminComponent),
  },
  {
    path: 'module-edit/:id',
    title: 'PV - Registro de Módulos',
    loadComponent: () => import('./modules/module-edit.component').then(m => m.ModuleEditComponent),
  },
  {
    path: 'branches-admin',
    title: 'PV - Sucursales',
    loadComponent: () => import('./branches/branches-admin.component').then(m => m.BranchesAdminComponent),
  },
  {
    path: 'branch-edit/:id',
    title: 'PV - Sucursales',
    loadComponent: () => import('./branches/branch-edit.component').then(m => m.BranchEditComponent),
  },
  {
    path: 'active-sessions',
    title: 'PV - Sesiones Activas',
    loadComponent: () => import('./sessions/active-sessions.component').then(m => m.ActiveSessionsComponent),
  },
  {
    path: 'company-create',
    title: 'PV - Alta de Empresa',
    loadComponent: () => import('./admin/company-create.component').then(m => m.CompanyCreateComponent),
  },
  {
    path: 'company-reset',
    title: 'PV - Reiniciar Datos',
    loadComponent: () => import('./admin/company-reset.component').then(m => m.CompanyResetComponent),
  },
];
