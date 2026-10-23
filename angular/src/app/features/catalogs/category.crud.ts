import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { CrudConfig, CrudEntity } from '../../shared/crud/crud.types';

export interface Category extends CrudEntity {
  CategoryName: string;
  Description: string;
  IsActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class CategoryCrud implements CrudConfig<Category> {
  private readonly api = inject(ApiService);

  listRoute = 'categories-admin';
  editRoute = 'category-edit';

  module = 'Inventario';
  listTitle = 'Registro de Categorías';
  heading = 'Categorías';
  singular = 'Categoría';
  newTitle = 'Nueva Categoría';
  editTitle = 'Editar Categoría';

  filterLabel = 'Nombre de la categoría';
  filterPlaceholder = 'Ingrese el nombre de la categoría...';
  emptyIcon = 'ri-price-tag-3-line';
  emptyText = 'Ingrese un nombre para buscar categorías en el sistema';
  newButton = 'Nueva Categoría';
  foundText = (n: number) => `${n} categoría(s) encontrada(s)`;

  confirmDelete = '¿Desea eliminar la categoría?';
  deleted = 'La categoría se eliminó correctamente.';
  confirmSave = '¿Desea guardar la categoría?';
  created = 'La categoría se creó correctamente.';
  updated = 'La categoría se actualizó correctamente.';

  columns = [
    { header: 'Nombre', key: 'CategoryName', strong: true },
    { header: 'Descripción', key: 'Description', muted: true, hideBelowLg: true },
    { header: 'Activo', key: 'IsActive', kind: 'bool' as const, center: true },
  ];

  card = {
    titleKey: 'CategoryName',
    activeKey: 'IsActive',
    lines: [{ text: (c: Category) => c.Description }],
  };

  sections = [{
    title: 'Datos de la Categoría',
    icon: 'ri-price-tag-3-line',
    fields: [
      { key: 'CategoryName', label: 'Nombre', placeholder: 'Nombre de la categoría', required: true },
      { key: 'Description', label: 'Descripción', placeholder: 'Descripción' },
      { key: 'IsActive', label: 'Activo', type: 'switch' as const, col: 'col-12 col-sm-2' },
    ],
  }];

  newModel = (): Category => ({ Id: '', CategoryName: '', Description: '', IsActive: true });

  list = (name: string) =>
    this.api.get<ResponseArray<Category>>(`Category?categoryName=${encodeURIComponent(name)}`);

  get = (id: string) => this.api.get<ResponseObject<Category>>(`Category/${id}`);

  create = (c: Category) => this.api.post<ResponseObject<boolean>>('Category', this.body(c));

  update = (c: Category) => this.api.put<ResponseObject<boolean>>(`Category/${c.Id}`, { id: c.Id, ...this.body(c) });

  remove = (id: string) => this.api.del<ResponseObject<boolean>>(`Category/${id}`);

  private body(c: Category) {
    return {
      categoryName: c.CategoryName,
      description: c.Description,
      isActive: c.IsActive,
    };
  }
}
