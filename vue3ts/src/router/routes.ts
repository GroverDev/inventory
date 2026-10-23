import inventoryRoutes from "@/modules/inventory/router";
import reportsRoutes from "@/modules/reports/router";
import userAccountRoutes from "@/modules/user-account/router";

export default [
  {
    path: "/login",
    name: "login",
    component: () => import("../views/account/login.vue"),
    meta: { title: "Iniciar sesión" },
  },
  {
    // totp y totp-setup se visitan a mitad del login (TOTP pendiente o recién emitido el token).
    path: "/totp",
    name: "totp",
    component: () => import("../views/account/totp.vue"),
    meta: { title: "Verificación en dos pasos" },
  },
  {
    path: "/totp-setup",
    name: "totp-setup",
    component: () => import("../views/account/totp-setup.vue"),
    meta: { title: "Configurar verificación en dos pasos" },
  },
  {
    path: "/",
    name: "default",
    redirect: { name: "inventory-dashboard" },
    meta: { title: "Dashboard", authRequired: true },
  },
  ...inventoryRoutes,
  ...reportsRoutes,
  ...userAccountRoutes,
  // Cualquier otra ruta: página 404 (debe ir al final).
  {
    path: "/:pathMatch(.*)*",
    name: "NotFound",
    component: () => import("../views/auth/errors/404-basic.vue"),
    meta: { title: "Página no encontrada" },
  },
];
