import { computed } from 'vue'
import type { RouteLocationRaw, Router } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import type { AccessMenu } from '@/models/access-menu'

/** Enlace a un formulario: `route` es el `url` del backend, que también es el nombre de la ruta. */
export interface MenuLeaf {
  id: number
  label: string
  route: string
  /** Formularios con `/:id` en la url reciben su IdFormulario como parámetro. */
  withId: boolean
}

export interface MenuItem {
  id: number
  label: string
  icon?: string
  /** Título de sección (no navega). */
  isTitle?: boolean
  /** Grupo desplegable con sus formularios. */
  subItems?: MenuLeaf[]
  /** Formulario suelto, sin grupo. */
  leaf?: MenuLeaf
}

/** Ícono (Remixicon) según palabras del título; la API no manda íconos. */
const ICON_BY_KEYWORD: [RegExp, string][] = [
  [/inventario|stock|almac/i, 'ri-archive-line'],
  [/venta|punto de venta/i, 'ri-shopping-cart-2-line'],
  [/compra|pedido/i, 'ri-shopping-bag-3-line'],
  [/reporte|informe/i, 'ri-bar-chart-2-line'],
  [/caja/i, 'ri-wallet-3-line'],
  [/cliente/i, 'ri-user-3-line'],
  [/proveedor/i, 'ri-truck-line'],
  [/producto|catálogo|catalogo/i, 'ri-medicine-bottle-line'],
  [/seguridad|usuario|rol|cuenta|acceso/i, 'ri-shield-user-line'],
  [/sucursal|empresa/i, 'ri-store-2-line'],
  [/descuento|oferta/i, 'ri-price-tag-3-line'],
  [/config|ajuste/i, 'ri-settings-3-line'],
]

export const iconFor = (title: string): string =>
  ICON_BY_KEYWORD.find(([pattern]) => pattern.test(title))?.[1] ?? 'ri-folder-2-line'

const toLeaf = (node: AccessMenu): MenuLeaf => ({
  id: node.IdFormulario,
  label: node.titulo,
  route: node.url.replace('/:id', ''),
  withId: node.url.includes('/:id'),
})

/** Convierte el árbol de accesos del usuario en el menú de la plantilla. */
export const buildMenu = (accessMenu: AccessMenu[]): MenuItem[] => {
  const items: MenuItem[] = [{ id: 0, label: 'Inicio', icon: 'ri-dashboard-2-line', leaf: { id: 0, label: 'Inicio', route: 'inventory-dashboard', withId: false } }]

  for (const section of accessMenu) {
    items.push({ id: section.IdFormulario, label: section.titulo, isTitle: true })

    for (const parent of section.Children ?? []) {
      const children = (parent.Children ?? []).filter((c) => !!c.url)
      if (children.length === 0) {
        if (parent.url) items.push({ id: parent.IdFormulario, label: parent.titulo, icon: iconFor(parent.titulo), leaf: toLeaf(parent) })
        continue
      }
      items.push({
        id: parent.IdFormulario,
        label: parent.titulo,
        icon: iconFor(parent.titulo),
        subItems: children.map(toLeaf),
      })
    }
  }
  return items
}

/**
 * Destino de un formulario, o `null` si esa pantalla todavía no existe en esta app
 * (durante la migración el menú ya trae todo, pero las rutas se van portando de a poco).
 */
export const resolveLeaf = (router: Router, leaf: MenuLeaf): RouteLocationRaw | null => {
  if (!router.hasRoute(leaf.route)) return null
  const target: RouteLocationRaw = leaf.withId ? { name: leaf.route, params: { id: leaf.id } } : { name: leaf.route }
  try {
    // Una ruta que exige parámetros que el menú no trae no debe romper todo el menú: se muestra como pendiente.
    router.resolve(target)
    return target
  } catch {
    return null
  }
}

export const useMenu = () => {
  const auth = useAuthStore()
  const items = computed(() => buildMenu(auth.getAccessMenu))
  /** Grupos y formularios sueltos (sin los títulos de sección): lo que usa el layout de dos columnas. */
  const groups = computed(() => items.value.filter((i) => !i.isTitle))
  return { items, groups }
}
