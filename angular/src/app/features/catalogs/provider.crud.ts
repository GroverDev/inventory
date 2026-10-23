import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { CrudConfig, CrudEntity } from '../../shared/crud/crud.types';

export interface Provider extends CrudEntity {
  ProviderName: string;
  Description: string;
  Direction: string;
  Celular: string;
}

@Injectable({ providedIn: 'root' })
export class ProviderCrud implements CrudConfig<Provider> {
  private readonly api = inject(ApiService);

  listRoute = 'providers-admin';
  editRoute = 'provider-edit';

  module = 'Inventario';
  listTitle = 'Registro de Proveedores';
  heading = 'Proveedores';
  singular = 'Proveedor';
  newTitle = 'Nuevo Proveedor';
  editTitle = 'Editar Proveedor';

  filterLabel = 'Nombre del proveedor';
  filterPlaceholder = 'Ingrese el nombre del proveedor...';
  emptyIcon = 'ri-truck-line';
  emptyText = 'Ingrese un nombre para buscar proveedores en el sistema';
  newButton = 'Nuevo Proveedor';
  foundText = (n: number) => `${n} proveedor(es) encontrado(s)`;

  confirmDelete = '¿Desea eliminar el proveedor?';
  deleted = 'El proveedor se eliminó correctamente.';
  confirmSave = '¿Desea guardar el proveedor?';
  created = 'El proveedor se creó correctamente.';
  updated = 'El proveedor se actualizó correctamente.';

  columns = [
    { header: 'Nombre', key: 'ProviderName', strong: true },
    { header: 'Descripción', key: 'Description', muted: true, hideBelowLg: true },
    { header: 'Dirección', key: 'Direction', muted: true, hideBelowLg: true },
    { header: 'Celular', key: 'Celular' },
  ];

  card = {
    titleKey: 'ProviderName',
    lines: [
      { text: (p: Provider) => p.Description },
      { text: (p: Provider) => p.Direction, icon: 'ri-map-pin-line' },
      { text: (p: Provider) => p.Celular, icon: 'ri-phone-line' },
    ],
  };

  sections = [
    {
      title: 'Datos del Proveedor',
      icon: 'ri-truck-line',
      fields: [
        { key: 'ProviderName', label: 'Nombre', placeholder: 'Nombre del proveedor', required: true },
        { key: 'Description', label: 'Descripción', placeholder: 'Descripción' },
      ],
    },
    {
      title: 'Contacto',
      icon: 'ri-contacts-book-line',
      fields: [
        { key: 'Direction', label: 'Dirección', placeholder: 'Dirección', icon: 'ri-map-pin-line' },
        { key: 'Celular', label: 'Celular', placeholder: 'Número de celular', icon: 'ri-phone-line' },
      ],
    },
  ];

  newModel = (): Provider => ({ Id: '', ProviderName: '', Description: '', Direction: '', Celular: '' });

  list = (name: string) =>
    this.api.get<ResponseArray<Provider>>(`Provider?providerName=${encodeURIComponent(name)}`);

  get = (id: string) => this.api.get<ResponseObject<Provider>>(`Provider/${id}`);

  create = (p: Provider) => this.api.post<ResponseObject<boolean>>('Provider', this.body(p));

  update = (p: Provider) => this.api.put<ResponseObject<boolean>>(`Provider/${p.Id}`, { id: p.Id, ...this.body(p) });

  remove = (id: string) => this.api.del<ResponseObject<boolean>>(`Provider/${id}`);

  private body(p: Provider) {
    return {
      providerName: p.ProviderName,
      description: p.Description,
      direction: p.Direction,
      celular: p.Celular,
    };
  }
}
