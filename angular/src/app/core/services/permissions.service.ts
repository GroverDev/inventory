import { Injectable, inject } from '@angular/core';

import { AccessMenu } from '../models/access-menu.model';
import { AuthService } from './auth.service';

export type PermAction = 'create' | 'read' | 'update' | 'delete';

/**
 * Permisos granulares por formulario (can_create/read/update/delete).
 * La clave del formulario es su `route` (ej. 'products-admin'), que coincide
 * con `url` en el menú de accesos devuelto por el backend.
 */
@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private readonly auth = inject(AuthService);

  findByRoute(route: string, nodes: AccessMenu[] = this.auth.accessMenu()): AccessMenu | undefined {
    for (const node of nodes) {
      if (node.url === route) return node;
      if (node.Children?.length) {
        const found = this.findByRoute(route, node.Children);
        if (found) return found;
      }
    }
    return undefined;
  }

  can(route: string, action: PermAction): boolean {
    const node = this.findByRoute(route);
    // Si el formulario no está en el menú del usuario: sin acceso.
    if (!node) return false;

    let flag: boolean | undefined;
    switch (action) {
      case 'create': flag = node.CanCreate; break;
      case 'update': flag = node.CanUpdate; break;
      case 'delete': flag = node.CanDelete; break;
      default: flag = node.CanRead; break;
    }
    // Menús cacheados antes de esta funcionalidad no traen la bandera: acceso total.
    return flag === undefined ? true : flag === true;
  }
}
