import type { RouteRecordRaw } from 'vue-router'

/**
 * Rutas del módulo user-account. El `name` de cada ruta coincide con el `url` del formulario en el
 * menú de accesos, que también es la clave de sus permisos.
 * Generado desde frontend/ por la migración: agrega aquí las rutas nuevas a mano.
 */
const routes: RouteRecordRaw[] = [
  {
    path: '/account',
    name: 'user-dashboard',
    component: () => import('@/modules/user-account/views/DashboardAccountView.vue'),
    meta: { authRequired: true, title: 'Cuentas de Usuario' },
  },
  {
    path: '/account/users-admin',
    name: 'users-admin',
    component: () => import('@/modules/user-account/views/users/UsersAdminView.vue'),
    meta: { authRequired: true, title: 'Registro de Usuarios' },
  },
  {
    path: '/account/user-edit/:id',
    name: 'user-edit',
    component: () => import('@/modules/user-account/views/users/UserEditView.vue'),
    meta: { authRequired: true, title: 'Editar Usuario' },
  },
  {
    path: '/account/forms-admin',
    name: 'forms-admin',
    component: () => import('@/modules/user-account/views/forms/FormsAdminView.vue'),
    meta: { authRequired: true, title: 'Registro de Formularios' },
  },
  {
    path: '/account/form-edit/:id',
    name: 'form-edit',
    component: () => import('@/modules/user-account/views/forms/FormEditView.vue'),
    meta: { authRequired: true, title: 'Editar Formulario' },
  },
  {
    path: '/account/modules-admin',
    name: 'modules-admin',
    component: () => import('@/modules/user-account/views/modules/ModulesAdminView.vue'),
    meta: { authRequired: true, title: 'Registro de Módulos' },
  },
  {
    path: '/account/module-edit/:id',
    name: 'module-edit',
    component: () => import('@/modules/user-account/views/modules/ModuleEditView.vue'),
    meta: { authRequired: true, title: 'Editar Módulo' },
  },
  {
    path: '/account/roles-admin',
    name: 'roles-admin',
    component: () => import('@/modules/user-account/views/roles/RolesAdminView.vue'),
    meta: { authRequired: true, title: 'Registro de Roles' },
  },
  {
    path: '/account/role-edit/:id',
    name: 'role-edit',
    component: () => import('@/modules/user-account/views/roles/RoleEditView.vue'),
    meta: { authRequired: true, title: 'Editar Rol' },
  },
  {
    path: '/account/branches-admin',
    name: 'branches-admin',
    component: () => import('@/modules/user-account/views/branches/BranchesAdminView.vue'),
    meta: { authRequired: true, title: 'Sucursales' },
  },
  {
    path: '/account/branch-edit/:id',
    name: 'branch-edit',
    component: () => import('@/modules/user-account/views/branches/BranchEditView.vue'),
    meta: { authRequired: true, title: 'Sucursales' },
  },
  {
    path: '/account/active-sessions',
    name: 'active-sessions',
    component: () => import('@/modules/user-account/views/sessions/SessionsAdminView.vue'),
    meta: { authRequired: true, title: 'Sesiones Activas' },
  },
  {
    path: '/account/company-create',
    name: 'company-create',
    component: () => import('@/modules/user-account/views/admin/CompanyCreateView.vue'),
    meta: { authRequired: true, title: 'Nueva Empresa' },
  },
  {
    path: '/account/company-reset',
    name: 'company-reset',
    component: () => import('@/modules/user-account/views/admin/CompanyResetView.vue'),
    meta: { authRequired: true, title: 'Resetear Empresa' },
  },
]

export default routes
