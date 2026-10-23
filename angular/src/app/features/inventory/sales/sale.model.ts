/** Medio de pago configurado en la empresa. */
export interface PaymentMethod {
  Id: string;
  Name: string;
  IconCss: string;
  RequiresChanges: boolean;
  /** Si el cobro entra al cajón. Un reintegro por este medio mueve la caja. */
  AffectsCash: boolean;
  /** Se declara al cerrar la caja. El efectivo se arquea siempre. */
  RequiresCount: boolean;
}

export interface SalePayment {
  Id: string;
  PaymentMethodId: string;
  PaymentMethodName: string;
  IconCss: string;
  AmountGiven: number;
  AmountReturned: number;
}

export interface SaleReturnDetail {
  Id: string;
  SaleDetailId: string;
  ProductId: string;
  ProductName: string;
  QuantityReturned: number;
  UnitPrice: number;
  LineTotal: number;
}

export interface SaleReturn {
  Id: string;
  SaleId: string;
  ReturnDate: string;
  Reason: string | null;
  TotalReturned: number;
  IsFullReturn: boolean;
  /** Medio por el que se reintegró. Vacío en devoluciones anteriores a 2026-08-24. */
  PaymentMethodName: string;
  Detail: SaleReturnDetail[];
}

export interface SaleReturnDetailRequest {
  SaleDetailId: string;
  ProductId: string;
  QuantityReturned: number;
  UnitPrice: number;
}

export interface SaleReturnRequest {
  SaleId: string;
  Reason: string | null;
  /** Medio de reintegro. El servidor decide con él si sale plata del cajón. */
  PaymentMethodId: string;
  Detail: SaleReturnDetailRequest[];
}

export interface SaleDetail {
  Id: string;
  SaleId: string;
  ProductId: string;
  Quantity: number;
  UnitPrice: number;
  LineSubtotal: number;
  LineTotalDiscounts: number;
  LineTotal: number;
  /**
   * Precio unitario efectivamente cobrado: el de lista menos el descuento de la
   * línea y menos la parte prorrateada del descuento global. Lo calcula el
   * servidor; es el que se reembolsa al devolver.
   */
  EffectiveUnitPrice: number;
  ProductName: string;
  LaboratoryName: string;
  /**
   * Lote del que salió la línea, elegido por FEFO al vender. Vacío si el
   * producto no usa lotes. Una venta que abarca varios lotes se parte en una
   * línea por lote, así que este campo identifica exactamente qué se entregó.
   */
  LotCode: string | null;
  ExpiryDate: string | null;
  /** Número de serie de la unidad entregada. Responde un reclamo de garantía. */
  SerialNumber: string | null;
  /**
   * Series elegidas en el mostrador al vender un producto serializado. Vacío
   * significa que el servidor elige por FEFO.
   */
  SerialNumbers: string[];
  isSelected: boolean;
  DiscountId: string;
  // Solo para la UI (se usan para recalcular al cambiar la cantidad)
  DiscountLabel: string;
  /** 'Percentage' | 'FixedAmount' */
  DiscountType: string;
  DiscountValue: number;
}

/** Lo que entró por un medio de pago: cobrado (sin el vuelto) menos lo devuelto. */
export interface PaymentMethodTotal {
  PaymentMethodId: string | null;
  Name: string;
  IconCss: string;
  Collected: number;
  Refunded: number;
  Net: number;
}

export interface Sale {
  Id: string;
  CustomerId: string;
  CustomerName: string;
  /** Medios con que se pagó, por ejemplo "Efectivo + QR". Solo en listados. */
  PaymentMethodsLabel?: string;
  /** Sucursal de la venta (reportes consolidados). */
  BranchName?: string;
  SellerName: string;
  SaleDate: string;
  Subtotal: number;
  TotalDiscounts: number;
  Total: number;
  IsActive: boolean;
  /** Suma de las devoluciones de la venta. Total no la descuenta; NetTotal sí. */
  TotalReturned: number;
  NetTotal: number;
  /** activa | con_devolucion | anulada (derivado en v_sales_net). */
  SaleStatus: string;
  CashSessionId: string;
  HeaderDiscountId: string;
  HeaderDiscountAmount: number;
  HeaderDiscountType: string;
  HeaderDiscountValue: number;
  SupervisorAuthToken: string;
  Detail: SaleDetail[];
  Payments: SalePayment[];
  Returns: SaleReturn[];
}

export interface SalesPagedResult {
  Items: Sale[];
  TotalCount: number;
  PeriodSubtotal: number;
  PeriodDiscounts: number;
  PeriodTotal: number;
  /** Lo devuelto en el período: PeriodTotal no lo descuenta. */
  PeriodReturned: number;
  /** PeriodTotal − PeriodReturned. */
  PeriodNet: number;
  /** PeriodNet repartido por medio de pago (todo el período, no solo la página). */
  PeriodByPaymentMethod?: PaymentMethodTotal[];
}

export const newSaleDetail = (): SaleDetail => ({
  Id: '',
  SaleId: '',
  ProductId: '',
  Quantity: 0,
  UnitPrice: 0,
  LineSubtotal: 0,
  LineTotalDiscounts: 0,
  LineTotal: 0,
  EffectiveUnitPrice: 0,
  ProductName: '',
  LaboratoryName: '',
  LotCode: null,
  ExpiryDate: null,
  SerialNumber: null,
  SerialNumbers: [],
  isSelected: false,
  DiscountId: '',
  DiscountLabel: '',
  DiscountType: '',
  DiscountValue: 0,
});

export const newSalePayment = (): SalePayment => ({
  Id: '',
  PaymentMethodId: '',
  PaymentMethodName: '',
  IconCss: '',
  AmountGiven: 0,
  AmountReturned: 0,
});

export const newSale = (): Sale => ({
  Id: '',
  CustomerId: '',
  CustomerName: '',
  SellerName: '',
  SaleDate: '',
  Subtotal: 0,
  TotalDiscounts: 0,
  Total: 0,
  IsActive: false,
  TotalReturned: 0,
  NetTotal: 0,
  SaleStatus: '',
  CashSessionId: '',
  HeaderDiscountId: '',
  HeaderDiscountAmount: 0,
  HeaderDiscountType: '',
  HeaderDiscountValue: 0,
  SupervisorAuthToken: '',
  Detail: [],
  Payments: [],
  Returns: [],
});
