import { createWebHistory, createRouter } from 'vue-router'

import { useAuthStore } from '@/stores/auth'
import routes from './routes'

const router = createRouter({
  history: createWebHistory(),
  routes,
  // Simula el scroll nativo al volver/avanzar en el historial.
  scrollBehavior(_to, _from, savedPosition) {
    return savedPosition ?? { top: 0, left: 0 }
  },
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()

  // Pestaña nueva con sesión recordada: el token no se guarda en disco, así que
  // se renueva en silencio con la cookie antes de decidir.
  if (!auth.getToken) await auth.restoreSession()

  const authRequired = to.matched.some((route) => route.meta.authRequired)
  if (authRequired && !auth.getToken) {
    return { name: 'login', query: { redirectFrom: to.fullPath } }
  }

  // Con sesión iniciada no tiene sentido mostrar el login.
  if (to.name === 'login' && auth.getToken) return { name: 'default' }
})

router.afterEach((to) => {
  document.title = `${to.meta.title ?? ''} | Punto de Venta`
})

export default router
