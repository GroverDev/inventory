/** Cómo identifica el sistema cada unidad del producto. */
export type TrackingMode = 'none' | 'lot' | 'serial';

export interface Product {
  Id: string;
  ProductCode: string;
  ProductName: string;
  Description: string;
  /** Precio en la sucursal activa (su excepción, o el base). */
  SalePrice: number;
  /** Precio del catálogo, igual en todas las sucursales. Es el que se edita. */
  BaseSalePrice?: number;
  BarCode: string;
  CurrentStock: number;
  MinReorderQuantity: number;
  BaseMinReorderQuantity?: number;
  AvailableInPos: boolean;
  /** La venta exige respaldo: receta, permiso. Del núcleo, no de un rubro. */
  RequiresAuthorization: boolean;
  UomId: string;
  UnitName: string;
  LaboratoryId: string;
  LaboratoryName: string;
  CategoryId: string;
  CategoryName: string;
  InitialStock: number;
  IsActive: boolean;
  /**
   * Seguimiento de existencias. Lo fija el servidor: la ficha no lo edita, se
   * cambia con la acción de activar lotes.
   */
  TrackingMode: TrackingMode;
  /**
   * Ruta relativa de la imagen, o null. La fija el servidor al subirla: no se
   * envía en el guardado de la ficha. Para mostrarla, `mediaUrl()`.
   */
  ImagePath: string | null;
}

export const newProduct = (): Product => ({
  Id: '',
  ProductCode: '',
  ProductName: '',
  Description: '',
  SalePrice: 0,
  BarCode: '',
  CurrentStock: 0,
  MinReorderQuantity: 0,
  AvailableInPos: false,
  RequiresAuthorization: false,
  UomId: '',
  UnitName: '',
  LaboratoryId: '',
  LaboratoryName: '',
  CategoryId: '',
  CategoryName: '',
  InitialStock: 0,
  IsActive: false,
  TrackingMode: 'none',
  ImagePath: null,
});

export interface ProductBulkUpdate {
  Id: string;
  ProductCode: string;
  ProductName: string;
  SalePrice: number;
  MinReorderQuantity: number;
  AvailableInPos: boolean;
  IsActive: boolean;
  BarCode: string;
}
