import { Type } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../core/models/api.models';

/** Entidad editable con identificador (vacío mientras no se haya creado). */
export interface CrudEntity {
  Id: string;
}

export type CrudFieldType = 'text' | 'email' | 'number' | 'switch';

export interface CrudField {
  key: string;
  label: string;
  type?: CrudFieldType;
  placeholder?: string;
  required?: boolean;
  minLength?: number;
  /** Texto bajo el campo cuando no pasa la validación. */
  invalidMessage?: string;
  /** Clases de columna Bootstrap; por defecto media fila. */
  col?: string;
  /** Ícono (Remixicon) a la izquierda del campo. */
  icon?: string;
  min?: number;
}

export interface CrudSection {
  title: string;
  /** Ícono (Remixicon) del encabezado de la sección. */
  icon: string;
  fields: CrudField[];
}

export interface CrudColumn {
  header: string;
  key: string;
  /** text = texto; bool = insignia Sí/No. */
  kind?: 'text' | 'bool';
  /** Estilo de la insignia cuando es verdadero (por defecto, verde). */
  trueClass?: string;
  /** Estilo de la insignia cuando es falso (por defecto, gris). */
  falseClass?: string;
  /** Texto principal en negrita. */
  strong?: boolean;
  /** Texto secundario y pequeño. */
  muted?: boolean;
  center?: boolean;
  /** Oculta la columna en pantallas menores a `lg`. */
  hideBelowLg?: boolean;
}

/** Una línea de texto bajo el título de la tarjeta móvil, con ícono opcional. */
export interface CrudCardLine<T extends CrudEntity> {
  text: (item: T) => string;
  /** Ícono (Remixicon) a la izquierda del texto. */
  icon?: string;
}

/** Insignia que solo aparece en la tarjeta móvil cuando la propiedad es verdadera. */
export interface CrudCardBadge {
  key: string;
  label: string;
  class: string;
}

/** Tarjeta que reemplaza a la fila de la tabla en pantallas menores a `md`. */
export interface CrudCard<T extends CrudEntity> {
  titleKey: string;
  /** Propiedad booleana que se muestra como insignia Activo/Inactivo junto al título. */
  activeKey?: string;
  lines: CrudCardLine<T>[];
  badges?: CrudCardBadge[];
}

/**
 * Descripción de un catálogo simple (lista con búsqueda por nombre + formulario
 * de alta/edición). Cada catálogo es un servicio que implementa esta interfaz y
 * se entrega a las rutas genéricas con `data: { crud: MiCatalogo }`.
 */
export interface CrudConfig<T extends CrudEntity> {
  /** Rutas de Angular (sin barra inicial); la de edición recibe el id como último segmento. */
  listRoute: string;
  editRoute: string;

  /** Migas de pan: «Inventario > Registro de Categorías». */
  module: string;
  listTitle: string;
  /** Título grande de la lista: «Gestión de CATEGORÍAS». */
  heading: string;
  /** «Categoría» / «Nueva Categoría» / «Editar Categoría». */
  singular: string;
  newTitle: string;
  editTitle: string;

  filterLabel: string;
  filterPlaceholder: string;
  emptyIcon: string;
  emptyText: string;
  newButton: string;
  /** «3 categoría(s) encontrada(s)». */
  foundText: (count: number) => string;

  confirmDelete: string;
  deleted: string;
  confirmSave: string;
  created: string;
  updated: string;

  columns: CrudColumn[];
  card: CrudCard<T>;
  sections: CrudSection[];

  newModel(): T;

  list(name: string): Promise<ResponseArray<T>>;
  get(id: string): Promise<ResponseObject<T>>;
  create(model: T): Promise<ResponseObject<unknown>>;
  update(model: T): Promise<ResponseObject<unknown>>;
  remove(id: string): Promise<ResponseObject<unknown>>;
}

/** Clave de `route.data` con la clase del catálogo a usar. */
export const CRUD_DATA_KEY = 'crud';

export type CrudConfigType<T extends CrudEntity = CrudEntity> = Type<CrudConfig<T>>;
