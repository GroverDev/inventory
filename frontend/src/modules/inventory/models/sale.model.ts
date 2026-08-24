import type { SaleDetail } from "./saleDetail.model";
import type { SalePayment } from "./paymentMethod.model";
import type { SaleReturn } from "./saleReturn.model";

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
}

export class Sale {
  public Id: string = '';
  public CustomerId: string = '';
  public CustomerName: string = '';
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

