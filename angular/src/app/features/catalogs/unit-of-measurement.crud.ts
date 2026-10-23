import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { CrudConfig, CrudEntity } from '../../shared/crud/crud.types';

export interface UnitOfMeasurement extends CrudEntity {
  UnitName: string;
  Proportion: number;
  PrecisionRounding: number;
  IsLargeThanDefault: boolean;
  IsDefault: boolean;
  IsActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class UnitOfMeasurementCrud implements CrudConfig<UnitOfMeasurement> {
  private readonly api = inject(ApiService);

  listRoute = 'uom-admin';
  editRoute = 'uom-edit';

  module = 'Inventario';
  listTitle = 'Unidades de Medida';
  heading = 'Unidades de Medida';
  singular = 'Unidad de Medida';
  newTitle = 'Nueva Unidad de Medida';
  editTitle = 'Editar Unidad de Medida';

  filterLabel = 'Nombre de la unidad';
  filterPlaceholder = 'Ingrese el nombre de la unidad...';
  emptyIcon = 'ri-ruler-line';
  emptyText = 'Ingrese un nombre para buscar unidades de medida';
  newButton = 'Nueva Unidad de Medida';
  foundText = (n: number) => `${n} unidad(es) encontrada(s)`;

  confirmDelete = '¿Desea eliminar la unidad de medida?';
  deleted = 'La unidad de medida se eliminó correctamente.';
  confirmSave = '¿Desea guardar la unidad de medida?';
  created = 'La unidad de medida se creó correctamente.';
  updated = 'La unidad de medida se actualizó correctamente.';

  columns = [
    { header: 'Nombre', key: 'UnitName', strong: true },
    { header: 'Proporción', key: 'Proportion', center: true },
    { header: 'Redondeo', key: 'PrecisionRounding', center: true },
    { header: 'Mayor que default', key: 'IsLargeThanDefault', kind: 'bool' as const, trueClass: 'bg-info', falseClass: 'bg-light text-secondary border', center: true },
    { header: 'Default', key: 'IsDefault', kind: 'bool' as const, trueClass: 'bg-warning text-dark', falseClass: 'bg-light text-secondary border', center: true },
    { header: 'Activo', key: 'IsActive', kind: 'bool' as const, center: true },
  ];

  card = {
    titleKey: 'UnitName',
    activeKey: 'IsActive',
    lines: [{ text: (u: UnitOfMeasurement) => `Proporción: ${u.Proportion} | Redondeo: ${u.PrecisionRounding}` }],
    badges: [
      { key: 'IsDefault', label: 'Default', class: 'bg-warning text-dark' },
      { key: 'IsLargeThanDefault', label: 'Mayor', class: 'bg-info' },
    ],
  };

  sections = [
    {
      title: 'Datos de la Unidad de Medida',
      icon: 'ri-ruler-line',
      fields: [
        { key: 'UnitName', label: 'Nombre', placeholder: 'Nombre de la unidad', required: true, minLength: 3, invalidMessage: 'Requerido (mínimo 3 caracteres).' },
      ],
    },
    {
      title: 'Configuración',
      icon: 'ri-settings-3-line',
      fields: [
        { key: 'Proportion', label: 'Proporción', type: 'number' as const, placeholder: '0', min: 0, col: 'col-12 col-sm-4 col-md-3' },
        { key: 'PrecisionRounding', label: 'Redondeo', type: 'number' as const, placeholder: '0', min: 0, col: 'col-12 col-sm-4 col-md-3' },
        { key: 'IsLargeThanDefault', label: 'Mayor que default', type: 'switch' as const, col: 'col-12 col-sm-4 col-md-2' },
        { key: 'IsDefault', label: 'Default', type: 'switch' as const, col: 'col-12 col-sm-4 col-md-2' },
        { key: 'IsActive', label: 'Activo', type: 'switch' as const, col: 'col-12 col-sm-4 col-md-2' },
      ],
    },
  ];

  newModel = (): UnitOfMeasurement => ({
    Id: '', UnitName: '', Proportion: 0, PrecisionRounding: 0, IsLargeThanDefault: false, IsDefault: false, IsActive: true,
  });

  list = (name: string) =>
    this.api.get<ResponseArray<UnitOfMeasurement>>(`UnitOfMeasurement?UnitOfMeasurementName=${encodeURIComponent(name)}`);

  get = (id: string) => this.api.get<ResponseObject<UnitOfMeasurement>>(`UnitOfMeasurement/${id}`);

  create = (u: UnitOfMeasurement) => this.api.post<ResponseObject<string>>('UnitOfMeasurement', this.body(u));

  update = (u: UnitOfMeasurement) =>
    this.api.put<ResponseObject<boolean>>(`UnitOfMeasurement/${u.Id}`, { id: u.Id, ...this.body(u) });

  remove = (id: string) => this.api.del<ResponseObject<boolean>>(`UnitOfMeasurement/${id}`);

  private body(u: UnitOfMeasurement) {
    return {
      name: u.UnitName,
      proportion: u.Proportion,
      precisionRounding: u.PrecisionRounding,
      isLargeThanDefault: u.IsLargeThanDefault,
      isDefault: u.IsDefault,
      isActive: u.IsActive,
    };
  }
}
