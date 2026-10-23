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
export interface ProductComponent {
  SubstanceId: string;
  SubstanceName: string;
  ConcentrationValue: number | null;
  ConcentrationUnit: string;
  /** `false` para los excipientes: no cuentan para buscar equivalentes. */
  IsActiveIngredient: boolean;
}

export const newProductComponent = (): ProductComponent => ({
  SubstanceId: '',
  SubstanceName: '',
  ConcentrationValue: null,
  ConcentrationUnit: '',
  IsActiveIngredient: true,
});

/** Ficha farmacéutica del producto. */
export interface ProductPharma {
  ProductId: string;
  FormId: string;
  RouteId: string;
  FormName: string;
  RouteName: string;
  Presentation: string;
  /** Del prospecto. No es una recomendación del sistema. */
  DosageReference: string;
  /** 'generico' | 'marca' | 'similar' */
  ProductType: string;
  SanitaryRegistry: string;
  /** ISO (yyyy-MM-dd). */
  SanitaryRegistryExpiry: string;
  Components: ProductComponent[];
}

export const newProductPharma = (): ProductPharma => ({
  ProductId: '',
  FormId: '',
  RouteId: '',
  FormName: '',
  RouteName: '',
  Presentation: '',
  DosageReference: '',
  ProductType: '',
  SanitaryRegistry: '',
  SanitaryRegistryExpiry: '',
  Components: [],
});

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
  /** Posición fijada a mano; 0 es sin fijar, o sea que se ordena sola. */
  ShowOrder: number;
}

/** Unidades habituales de concentración, para sugerir sin encajonar. */
export const UNIDADES_CONCENTRACION = ['mg', 'g', 'mcg', 'ml', 'UI', '%', 'mg/ml', 'mg/5ml'];
