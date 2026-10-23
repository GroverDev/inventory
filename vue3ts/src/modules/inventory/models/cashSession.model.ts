import type { CashMovement } from './cashMovement.model';
import type { PaymentMethodTotal } from './sale.model';

export class CashSession {
  public Id: string = '';
  public UserId: number = 0;
  public UserFullName: string = '';
  public OpenedAt: string = '';
  public ClosedAt: string | null = null;
  public IsOpen: boolean = true;
  public OpeningAmount: number = 0;
  public DeclaredAmount: number | null = null;
  public ExpectedAmount: number | null = null;
  public Difference: number | null = null;
  public Notes: string = '';
  /** Ventas de la sesión, todos los métodos de pago. Informativo. */
  public TotalSales: number = 0;
  /** Lo cobrado por métodos que entran al cajón, ya sin el vuelto: es lo que suma al esperado. */
  public TotalCashSales: number = 0;
  public TotalExpenses: number = 0;
  public TotalWithdrawals: number = 0;
  public TotalIncome: number = 0;
  /** Efectivo reintegrado por devoluciones en la sesión: resta al esperado. */
  public TotalReturns: number = 0;
  /** Neto del turno por medio de pago (para cuadrar QR y tarjeta contra el banco). */
  public ByPaymentMethod: PaymentMethodTotal[] = [];
  /** Arqueo del cierre por medio. Vacío mientras está abierta. */
  public Counts: CashCount[] = [];
  /** Cierres rechazados por diferencia sin observación antes del definitivo. */
  public CloseAttempts: number = 0;
  /** Supervisor que autorizó el cierre, si lo necesitó. */
  public CloseAuthorizedBy: number | null = null;
  public CloseAuthorizedByName: string = '';
  /** Conteo del efectivo por billete y moneda, si se declaró. */
  public Denominations: DenominationCount[] = [];
  /** Solo en un cierre rechazado: 'note' o 'supervisor', lo que falta para cerrar. */
  public CloseRequires: '' | 'note' | 'supervisor' = '';
  public Movements: CashMovement[] = [];
}

export class OpenCashSessionRequest {
  public OpeningAmount: number = 0;
}

export class CloseCashSessionRequest {
  /** Solo para clientes anteriores al arqueo por medio; si va Counts, se ignora. */
  public DeclaredAmount: number = 0;
  public Notes: string = '';
  /** Lo declarado por cada medio que se arquea, sin ver lo esperado. */
  public Counts: { PaymentMethodId: string; Declared: number }[] = [];
  /** Efectivo por billete y moneda; si va, tiene que sumar lo declarado en efectivo. */
  public Denominations?: DenominationCount[];
  /** Sesión del supervisor que autoriza un cierre con diferencia grande o intentos agotados. */
  public SupervisorAuthToken?: string;
}

export interface DenominationCount {
  Value: number;
  Quantity: number;
}

/** Billetes y monedas bolivianos, de mayor a menor. */
export const BOB_DENOMINATIONS = [200, 100, 50, 20, 10, 5, 2, 1, 0.5, 0.2, 0.1];

/** Configuración del cierre de caja de la empresa (Settings/cash-close). */
export interface CashCloseSettings {
  NoteThreshold: number;
  SupervisorThreshold: number | null;
  MaxAttempts: number | null;
  RequireDenominations: boolean;
}

/** Arqueo de un medio en el cierre: esperado, declarado y diferencia (declarado − esperado). */
export interface CashCount {
  PaymentMethodId: string;
  Name: string;
  IconCss: string;
  Expected: number;
  Declared: number;
  Difference: number;
}

export interface SessionSaleDetail {
  ProductName: string;
  Quantity: number;
  UnitPrice: number;
  LineSubtotal: number;
  LineTotalDiscounts: number;
  LineTotal: number;
}

export interface SessionSalePayment {
  PaymentMethodName: string;
  AmountGiven: number;
  AmountReturned: number;
}

export interface SessionSale {
  Id: string;
  CustomerName: string;
  SellerName: string;
  SaleDate: string;
  Subtotal: number;
  TotalDiscounts: number;
  HeaderDiscountAmount: number;
  Total: number;
  IsActive: boolean;

  /** Suma de las devoluciones de la venta. Total no la descuenta; NetTotal sí. */
  TotalReturned: number;
  NetTotal: number;
  /** activa | con_devolucion | anulada. Vacío en APIs viejas que no lo mandan. */
  SaleStatus: string;
  Detail: SessionSaleDetail[];
  Payments: SessionSalePayment[];
}
