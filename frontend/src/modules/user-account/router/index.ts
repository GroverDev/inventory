
//import { isNotAuthenticatedGuard } from '@/shared/guards/authGuard';

import { isAuthenticatedGuard } from '@/guards/authGuard';

export default {
  name: 'account',
  component: () => import(/* webpackChunkName: "user-account" */ '@/modules/user-account/layout/UserAccountLayout.vue'),
  children: [
    {
      path: '',
      name: 'user-dashboard',
      component: () => import(/* webpackChunkName: "user-dashboard" */ '@/modules/user-account/views/DashboardAccountView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        title: 'Cuentas de Usuario' + ' - Dashboard',
      },
    },
    {
      path: 'users-admin',
      name: 'users-admin',
      component: () => import(/* webpackChunkName: "users-admin" */ '@/modules/user-account/views/users/UsersAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        //roles: ['admin', 'super-admin']
        title: 'PV - Registro de Usuarios',
        titleForm: 'Registro de Usuarios'
      },
    },
    {
      path: 'user-edit/:id',
      name: 'user-edit',
      component: () => import(/* webpackChunkName: "user-edit" */ '@/modules/user-account/views/users/UserEditView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Editar Usuario',
        titleForm: 'Editar Usuario'
      },
    },
    {
      path: 'forms-admin',
      name: 'forms-admin',
      component: () => import(/* webpackChunkName: "forms-admin" */ '@/modules/user-account/views/forms/FormsAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Registro de Formularios',
        titleForm: 'Registro de Formularios'
      },
    },
    {
      path: 'form-edit/:id',
      name: 'form-edit',
      component: () => import(/* webpackChunkName: "form-edit" */ '@/modules/user-account/views/forms/FormEditView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Editar Formulario',
        titleForm: 'Editar Formulario'
      },
    },
    {
      path: 'modules-admin',
      name: 'modules-admin',
      component: () => import(/* webpackChunkName: "modules-admin" */ '@/modules/user-account/views/modules/ModulesAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Registro de Módulos',
        titleForm: 'Registro de Módulos'
      },
    },
    {
      path: 'module-edit/:id',
      name: 'module-edit',
      component: () => import(/* webpackChunkName: "module-edit" */ '@/modules/user-account/views/modules/ModuleEditView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Editar Módulo',
        titleForm: 'Editar Módulo'
      },
    },
    {
      path: 'roles-admin',
      name: 'roles-admin',
      component: () => import(/* webpackChunkName: "roles-admin" */ '@/modules/user-account/views/roles/RolesAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Registro de Roles',
        titleForm: 'Registro de Roles'
      },
    },
    {
      path: 'role-edit/:id',
      name: 'role-edit',
      component: () => import(/* webpackChunkName: "role-edit" */ '@/modules/user-account/views/roles/RoleEditView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Editar Rol',
        titleForm: 'Editar Rol'
      },
    },
    {
      path: 'active-sessions',
      name: 'active-sessions',
      component: () => import(/* webpackChunkName: "active-sessions" */ '@/modules/user-account/views/sessions/SessionsAdminView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Sesiones Activas',
        titleForm: 'Sesiones Activas'
      },
    },
    {
      path: 'company-create',
      name: 'company-create',
      component: () => import(/* webpackChunkName: "company-create" */ '@/modules/user-account/views/admin/CompanyCreateView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Nueva Empresa',
        titleForm: 'Nueva Empresa'
      },
    },
    {
      path: 'company-reset',
      name: 'company-reset',
      component: () => import(/* webpackChunkName: "company-reset" */ '@/modules/user-account/views/admin/CompanyResetView.vue'),
      beforeEnter: [isAuthenticatedGuard],
      meta: {
        requiresAuth: true,
        title: 'PV - Resetear Empresa',
        titleForm: 'Resetear Empresa'
      },
    },
  ],
};
