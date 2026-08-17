/** Sustancia del catálogo: principio activo o excipiente. */
export interface PharmaSubstance {
  Id: string;
  SubstanceName: string;
  TherapeuticGroup: string | null;
}

/** Catálogo corto: forma farmacéutica o vía de administración. */
export interface PharmaCatalogItem {
  Id: string;
  Name: string;
}

/**
 * Un componente del producto con su concentración.
 *
 * `SubstanceId` vacío con `SubstanceName` cargado significa "sustancia nueva":
 * el servidor la da de alta al guardar. Es lo que evita tener que salir a otra
 * pantalla a crear el principio activo antes de poder cargar el producto.
 */
export class ProductComponent {
  public SubstanceId: string = '';
  public SubstanceName: string = '';
  public ConcentrationValue: number | null = null;
  public ConcentrationUnit: string = '';
  /** `false` para los excipientes: no cuentan para buscar equivalentes. */
  public IsActiveIngredient: boolean = true;
}

/** Ficha farmacéutica del producto. */
export class ProductPharma {
  public ProductId: string = '';
  public FormId: string = '';
  public RouteId: string = '';
  public FormName: string = '';
  public RouteName: string = '';
  public Presentation: string = '';
  /** Del prospecto. No es una recomendación del sistema. */
  public DosageReference: string = '';
  /** 'generico' | 'marca' | 'similar' */
  public ProductType: string = '';
  public SanitaryRegistry: string = '';
  /** ISO (yyyy-MM-dd). */
  public SanitaryRegistryExpiry: string = '';
  public Components: ProductComponent[] = [];
}

/** Un producto que puede ofrecerse en lugar de otro. */
export interface ProductEquivalent {
  ProductId: string;
  ProductName: string;
  SalePrice: number;
  CurrentStock: number;
  ProductType: string;
  Presentation: string;
  /** `true` si la definió la farmacia; `false` si se dedujo de la composición. */
  IsManual: boolean;
  Reason: string;
}

/** Unidades habituales de concentración, para sugerir sin encajonar. */
export const UNIDADES_CONCENTRACION = ['mg', 'g', 'mcg', 'ml', 'UI', '%', 'mg/ml', 'mg/5ml'];
