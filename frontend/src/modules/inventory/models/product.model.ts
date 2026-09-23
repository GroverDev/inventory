export class Product {

  public Id: string = '';
  public ProductCode: string = '';
  public ProductName: string = '';
  public Description: string = '';
  /** Precio en la sucursal activa (su excepción, o el base). */
  public SalePrice: number = 0;
  /** Precio del catálogo, igual en todas las sucursales. Es el que se edita. */
  public BaseSalePrice?: number;
  public BarCode: string = '';
  public CurrentStock: number = 0;
  public MinReorderQuantity: number = 0;
  public BaseMinReorderQuantity?: number;
  public AvailableInPos: boolean = false;
  /** La venta exige respaldo: receta, permiso. Del núcleo, no de un rubro. */
  public RequiresAuthorization: boolean = false;
  public UomId: string = '';
  public UnitName: string = '';
  public LaboratoryId: string = '';
  public LaboratoryName: string = '';
  public CategoryId: string = '';
  public CategoryName: string = '';
  public InitialStock: number = 0;
  public IsActive: boolean = false;
  /**
   * Seguimiento de existencias. Lo fija el servidor: la ficha no lo edita, se
   * cambia con la acción de activar lotes.
   */
  public TrackingMode: TrackingMode = 'none';
}

/** Cómo identifica el sistema cada unidad del producto. */
export type TrackingMode = 'none' | 'lot' | 'serial';

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
