import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { CrudConfig, CrudEntity } from '../../shared/crud/crud.types';

export interface Laboratory extends CrudEntity {
  LaboratoryName: string;
  Description: string;
  Direction: string;
  Celular: string;
  IsActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class LaboratoryCrud implements CrudConfig<Laboratory> {
  private readonly api = inject(ApiService);

  listRoute = 'laboratories-admin';
  editRoute = 'laboratory-edit';

  module = 'Inventario';
  listTitle = 'Registro de Laboratorios';
  heading = 'Laboratorios';
  singular = 'Laboratorio';
  newTitle = 'Nuevo Laboratorio';
  editTitle = 'Editar Laboratorio';

  filterLabel = 'Nombre del laboratorio';
  filterPlaceholder = 'Ingrese el nombre del laboratorio...';
  emptyIcon = 'ri-flask-line';
  emptyText = 'Ingrese un nombre para buscar laboratorios en el sistema';
  newButton = 'Nuevo Laboratorio';
  foundText = (n: number) => `${n} laboratorio(s) encontrado(s)`;

  confirmDelete = '¿Desea eliminar el laboratorio?';
  deleted = 'El laboratorio se eliminó correctamente.';
  confirmSave = '¿Desea guardar el laboratorio?';
  created = 'El laboratorio se creó correctamente.';
  updated = 'El laboratorio se actualizó correctamente.';

  columns = [
    { header: 'Nombre', key: 'LaboratoryName', strong: true },
    { header: 'Descripción', key: 'Description', muted: true, hideBelowLg: true },
    { header: 'Dirección', key: 'Direction', muted: true, hideBelowLg: true },
    { header: 'Celular', key: 'Celular' },
    { header: 'Activo', key: 'IsActive', kind: 'bool' as const, center: true },
  ];

  card = {
    titleKey: 'LaboratoryName',
    activeKey: 'IsActive',
    lines: [
      { text: (l: Laboratory) => l.Description },
      { text: (l: Laboratory) => l.Celular, icon: 'ri-phone-line' },
    ],
  };

  sections = [{
    title: 'Datos del Laboratorio',
    icon: 'ri-flask-line',
    fields: [
      { key: 'LaboratoryName', label: 'Nombre', placeholder: 'Nombre del laboratorio', required: true },
      { key: 'Description', label: 'Descripción', placeholder: 'Descripción' },
      { key: 'Direction', label: 'Dirección', placeholder: 'Dirección' },
      { key: 'Celular', label: 'Celular', placeholder: 'Número de celular' },
      { key: 'IsActive', label: 'Activo', type: 'switch' as const, col: 'col-12 col-sm-2' },
    ],
  }];

  newModel = (): Laboratory => ({ Id: '', LaboratoryName: '', Description: '', Direction: '', Celular: '', IsActive: true });

  list = (name: string) =>
    this.api.get<ResponseArray<Laboratory>>(`Laboratory?laboratoryName=${encodeURIComponent(name)}`);

  get = (id: string) => this.api.get<ResponseObject<Laboratory>>(`Laboratory/${id}`);

  create = (l: Laboratory) => this.api.post<ResponseObject<string>>('Laboratory', this.body(l));

  update = (l: Laboratory) => this.api.put<ResponseObject<boolean>>(`Laboratory/${l.Id}`, { id: l.Id, ...this.body(l) });

  remove = (id: string) => this.api.del<ResponseObject<boolean>>(`Laboratory/${id}`);

  private body(l: Laboratory) {
    return {
      laboratoryName: l.LaboratoryName,
      description: l.Description,
      direction: l.Direction,
      celular: l.Celular,
      isActive: l.IsActive,
    };
  }
}
