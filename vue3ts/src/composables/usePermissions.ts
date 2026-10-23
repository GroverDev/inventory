import { useAuthStore } from '@/stores/auth'
import type { AccessMenu } from '@/models/access-menu'

export type PermAction = 'create' | 'read' | 'update' | 'delete'

/**
 * Permisos granulares por formulario (can_create/read/update/delete).
 * La clave del formulario es su `route` (ej. 'products-admin'), que coincide
 * con `url` en el menú de accesos devuelto por el backend.
 */
export const usePermissions = () => {
  const auth = useAuthStore()

  const findByRoute = (route: string, nodes: AccessMenu[] = auth.getAccessMenu): AccessMenu | undefined => {
    for (const node of nodes) {
      if (node.url === route) return node
      if (node.Children?.length) {
        const found = findByRoute(route, node.Children)
        if (found) return found
      }
    }
    return undefined
  }

  const can = (route: string, action: PermAction): boolean => {
    const node = findByRoute(route)
    // Si el formulario no está en el menú del usuario → sin acceso.
    if (!node) return false

    let flag: boolean | undefined
    switch (action) {
      case 'create': flag = node.CanCreate; break
      case 'update': flag = node.CanUpdate; break
      case 'delete': flag = node.CanDelete; break
      default: flag = node.CanRead; break
    }
    // Menús cacheados antes de esta funcionalidad: sin bandera se asume acceso total.
    return flag === undefined ? true : flag === true
  }

  return { can, findByRoute }
}

export default usePermissions
