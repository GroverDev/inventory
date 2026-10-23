import { Type } from '@angular/core';
import { Route } from '@angular/router';

import { CRUD_DATA_KEY } from '../../shared/crud/crud.types';
import { CategoryCrud } from './category.crud';
import { CustomerCrud } from './customer.crud';
import { LaboratoryCrud } from './laboratory.crud';
import { ProviderCrud } from './provider.crud';
import { UnitOfMeasurementCrud } from './unit-of-measurement.crud';

const list = () => import('../../shared/crud/crud-list.component').then(m => m.CrudListComponent);
const edit = () => import('../../shared/crud/crud-edit.component').then(m => m.CrudEditComponent);

/** Un catálogo son dos rutas (lista y edición) que comparten la misma configuración. */
const catalog = (
  crud: Type<unknown>,
  listRoute: string,
  editRoute: string,
  listTitle: string,
  editTitle: string,
): Route[] => [
  { path: listRoute, title: `PV - ${listTitle}`, loadComponent: list, data: { [CRUD_DATA_KEY]: crud } },
  { path: `${editRoute}/:id`, title: `PV - ${editTitle}`, loadComponent: edit, data: { [CRUD_DATA_KEY]: crud } },
];

/** Rutas planas, con el mismo nombre que las del frontend Vue (coinciden con `url` del menú). */
export const CATALOG_ROUTES: Route[] = [
  ...catalog(CategoryCrud, 'categories-admin', 'category-edit', 'Registro de Categorías', 'Categoría'),
  ...catalog(LaboratoryCrud, 'laboratories-admin', 'laboratory-edit', 'Registro de Laboratorios', 'Laboratorio'),
  ...catalog(UnitOfMeasurementCrud, 'uom-admin', 'uom-edit', 'Unidades de Medida', 'Unidad de Medida'),
  ...catalog(ProviderCrud, 'providers-admin', 'provider-edit', 'Registro de Proveedores', 'Proveedor'),
  ...catalog(CustomerCrud, 'customers-admin', 'customer-edit', 'Registro de Clientes', 'Cliente'),
];
