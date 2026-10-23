import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../core/models/api.models';
import { ApiService } from '../../core/services/api.service';
import { CrudConfig, CrudEntity } from '../../shared/crud/crud.types';

export interface Customer extends CrudEntity {
  FullName: string;
  DocumentNumber: string;
  Email: string;
  Cellphone: string;
  IsActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class CustomerCrud implements CrudConfig<Customer> {
  private readonly api = inject(ApiService);

  listRoute = 'customers-admin';
  editRoute = 'customer-edit';

  module = 'Inventario';
  listTitle = 'Registro de Clientes';
  heading = 'Clientes';
  singular = 'Cliente';
  newTitle = 'Nuevo Cliente';
  editTitle = 'Editar Cliente';

  filterLabel = 'Nombre del cliente';
  filterPlaceholder = 'Ingrese el nombre del cliente...';
  emptyIcon = 'ri-group-line';
  emptyText = 'Ingrese un nombre para buscar clientes en el sistema';
  newButton = 'Nuevo Cliente';
  foundText = (n: number) => `${n} cliente(s) encontrado(s)`;

  confirmDelete = '¿Desea eliminar el cliente?';
  deleted = 'El cliente se eliminó correctamente.';
  confirmSave = '¿Desea guardar el cliente?';
  created = 'El cliente se creó correctamente.';
  updated = 'El cliente se actualizó correctamente.';

  columns = [
    { header: 'Nombre Completo', key: 'FullName', strong: true },
    { header: 'Nro. Documento', key: 'DocumentNumber' },
    { header: 'Correo', key: 'Email', muted: true, hideBelowLg: true },
    { header: 'Celular', key: 'Cellphone', hideBelowLg: true },
    { header: 'Activo', key: 'IsActive', kind: 'bool' as const, center: true },
  ];

  card = {
    titleKey: 'FullName',
    activeKey: 'IsActive',
    lines: [
      { text: (c: Customer) => c.DocumentNumber, icon: 'ri-profile-line' },
      { text: (c: Customer) => c.Email },
      { text: (c: Customer) => c.Cellphone, icon: 'ri-phone-line' },
    ],
  };

  sections = [
    {
      title: 'Datos del Cliente',
      icon: 'ri-user-line',
      fields: [
        { key: 'FullName', label: 'Nombre completo', placeholder: 'Nombre completo del cliente', required: true },
        { key: 'DocumentNumber', label: 'Nro. de documento', placeholder: 'CI / NIT', required: true },
      ],
    },
    {
      title: 'Contacto',
      icon: 'ri-contacts-book-line',
      fields: [
        { key: 'Email', label: 'Correo Electrónico', type: 'email' as const, placeholder: 'correo@ejemplo.com', icon: 'ri-mail-line' },
        { key: 'Cellphone', label: 'Celular', placeholder: 'Número de celular', icon: 'ri-phone-line' },
        { key: 'IsActive', label: 'Activo', type: 'switch' as const, col: 'col-12 col-sm-2' },
      ],
    },
  ];

  newModel = (): Customer => ({ Id: '', FullName: '', DocumentNumber: '', Email: '', Cellphone: '', IsActive: true });

  list = (name: string) =>
    this.api.get<ResponseArray<Customer>>(`Customers?CustomerName=${encodeURIComponent(name)}`);

  get = (id: string) => this.api.get<ResponseObject<Customer>>(`Customers/${id}`);

  /** Devuelve el Id del cliente recién creado. */
  create = (c: Customer) => this.api.post<ResponseObject<string>>('Customers', this.body(c));

  update = (c: Customer) => this.api.put<ResponseObject<boolean>>(`Customers/${c.Id}`, { id: c.Id, ...this.body(c) });

  remove = (id: string) => this.api.del<ResponseObject<boolean>>(`Customers/${id}`);

  /** El cliente genérico del tenant activo, que el POS precarga por defecto. */
  getDefault = () => this.api.get<ResponseObject<Customer>>('Customers/default');

  private body(c: Customer) {
    return {
      fullName: c.FullName,
      documentNumber: c.DocumentNumber,
      email: c.Email,
      cellphone: c.Cellphone,
      isActive: c.IsActive,
    };
  }
}
