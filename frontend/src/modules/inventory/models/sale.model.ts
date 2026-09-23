import type { SaleDetail } from "./saleDetail.model";
import type { SalePayment } from "./paymentMethod.model";
import type { SaleReturn } from "./saleReturn.model";

/** Lo que entró por un medio de pago: cobrado (sin el vuelto) menos lo devuelto. */
export interface PaymentMethodTotal {
  PaymentMethodId: string | null;
  Name: string;
  IconCss: string;
  Collected: number;
  Refunded: number;
  Net: number;
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

export class Sale {
  public Id: string = '';
  public CustomerId: string = '';
  public CustomerName: string = '';
  /** Medios con que se pagó, por ejemplo "Efectivo + QR". Solo en listados. */
  public PaymentMethodsLabel?: string;
  /** Sucursal de la venta (reportes consolidados). */
  public BranchName?: string;
  public SellerName: string = '';
  public SaleDate: string = '';
  public Subtotal: number = 0;
  public TotalDiscounts: number = 0;
  public Total: number = 0;
  public IsActive: boolean = false;
  /** Suma de las devoluciones de la venta. Total no la descuenta; NetTotal sí. */
  public TotalReturned: number = 0;
  public NetTotal: number = 0;
  /** activa | con_devolucion | anulada (derivado en v_sales_net). */
  public SaleStatus: string = "";
  public CashSessionId: string = '';
  public HeaderDiscountId: string = '';
  public HeaderDiscountAmount: number = 0;
  public HeaderDiscountType: string = '';
  public HeaderDiscountValue: number = 0;
  public SupervisorAuthToken: string = '';
  public Detail: SaleDetail[] = [];
  public Payments: SalePayment[] = [];
  public Returns: SaleReturn[] = [];
}

