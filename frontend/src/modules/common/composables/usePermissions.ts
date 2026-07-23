import { useAuthStore } from '@/modules/auth/stores/auth.store';
import type { AccessMenu } from '@/modules/auth/models/acccessMenu.interface';

export type PermAction = 'create' | 'read' | 'update' | 'delete';

/**
 * Permisos granulares por formulario (can_create/read/update/delete).
 * La clave del formulario es su `route` (ej. 'products-admin'), que coincide
 * con `url` en el menú de accesos devuelto por el backend.
 */
export const usePermissions = () => {
  const authStore = useAuthStore();

  const findByRoute = (route: string, nodes?: AccessMenu[]): AccessMenu | undefined => {
    const list = nodes ?? authStore.getAccessMenu;
    for (const node of list) {
      if (node.url === route) return node;
      if (node.Children && node.Children.length) {
        const found = findByRoute(route, node.Children);
        if (found) return found;
      }
    }
    return undefined;
  };

  const can = (route: string, action: PermAction): boolean => {
    const node = findByRoute(route);
    // Si el formulario no está en el menú del usuario → sin acceso.
    if (!node) return false;

    let flag: boolean | undefined;
    switch (action) {
      case 'create': flag = node.CanCreate; break;
      case 'update': flag = node.CanUpdate; break;
      case 'delete': flag = node.CanDelete; break;
      default:       flag = node.CanRead;   break;
    }
    // Compatibilidad con menús cacheados antes de esta funcionalidad:
    // si la bandera no existe (undefined), se asume acceso total.
    return flag === undefined ? true : flag === true;
  };

  return { can, findByRoute };
};

export default usePermissions;
