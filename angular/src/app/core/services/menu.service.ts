import { Injectable, computed, inject } from '@angular/core';

import { MenuItem } from '../../layouts/sidebar/menu.model';
import { AccessMenu } from '../models/access-menu.model';
import { AuthService } from './auth.service';

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
];

const iconFor = (title: string): string =>
  ICON_BY_KEYWORD.find(([pattern]) => pattern.test(title))?.[1] ?? 'ri-folder-2-line';

/** La ruta de Angular se llama igual que el formulario (`url`), igual que el nombre de ruta del Vue. */
export const routeLink = (node: AccessMenu): string => {
  const route = node.url.replace('/:id', '');
  if (route === 'inventory-dashboard') return '/';
  return node.url.includes('/:id') ? `/${route}/${node.IdFormulario}` : `/${route}`;
};

/** Convierte el menú de accesos del usuario (3 niveles) en el menú del template. */
@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly auth = inject(AuthService);

  readonly items = computed<MenuItem[]>(() => {
    const items: MenuItem[] = [
      { id: 0, label: 'Inicio', icon: 'ri-dashboard-2-line', link: '/' },
    ];

    for (const section of this.auth.accessMenu()) {
      items.push({ id: section.IdFormulario, label: section.titulo, isTitle: true });

      for (const parent of section.Children ?? []) {
        const children = (parent.Children ?? []).filter(c => !!c.url);
        if (children.length === 0) {
          if (parent.url) {
            items.push({ id: parent.IdFormulario, label: parent.titulo, icon: iconFor(parent.titulo), link: routeLink(parent) });
          }
          continue;
        }

        items.push({
          id: parent.IdFormulario,
          label: parent.titulo,
          icon: iconFor(parent.titulo),
          isCollapsed: true,
          subItems: children.map(child => ({
            id: child.IdFormulario,
            parentId: parent.IdFormulario,
            label: child.titulo,
            link: routeLink(child),
          })),
        });
      }
    }
    return items;
  });
}
