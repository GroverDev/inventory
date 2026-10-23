import { PaymentMethodTotal } from '../sales/sale.model';

export type MovementType = 'expense' | 'withdrawal' | 'income' | 'return';

export interface CashMovement {
  Id: string;
  CashSessionId: string;
  MovementType: MovementType;
  Amount: number;
  Description: string;
  Created: string;
  CreatedByName: string;
}

export interface CashMovementRequest {
  CashSessionId: string;
  MovementType: 'expense' | 'withdrawal' | 'income';
  Amount: number;
  Description: string;
}

export const MovementTypeLabels: Record<string, string> = {
  expense: 'Gasto',
  withdrawal: 'Retiro',
  income: 'Ingreso',
  // Lo genera la devolución, no el usuario: no aparece en el alta de movimientos.
  return: 'Devolución',
};

/** Íconos Remixicon equivalentes a los del Vue (receipt, arrow-circle-up/down, undo). */
export const MovementTypeIcons: Record<string, string> = {
  expense: 'ri-receipt-line',
  withdrawal: 'ri-arrow-up-circle-line',
  income: 'ri-arrow-down-circle-line',
  return: 'ri-arrow-go-back-line',
};

export interface DenominationCount {
  Value: number;
  Quantity: number;
}

/** Billetes y monedas bolivianos, de mayor a menor. */
export const BOB_DENOMINATIONS = [200, 100, 50, 20, 10, 5, 2, 1, 0.5, 0.2, 0.1];

/** Arqueo de un medio en el cierre: esperado, declarado y diferencia (declarado − esperado). */
export interface CashCount {
  PaymentMethodId: string;
  Name: string;
  IconCss: string;
  Expected: number;
  Declared: number;
  Difference: number;
}

export interface CashSession {
  Id: string;
  UserId: number;
  UserFullName: string;
  OpenedAt: string;
  ClosedAt: string | null;
  IsOpen: boolean;
  OpeningAmount: number;
  DeclaredAmount: number | null;
  ExpectedAmount: number | null;
  Difference: number | null;
  Notes: string;
  /** Ventas de la sesión, todos los métodos de pago. Informativo. */
  TotalSales: number;
  /** Lo cobrado por métodos que entran al cajón, ya sin el vuelto: es lo que suma al esperado. */
  TotalCashSales: number;
  TotalExpenses: number;
  TotalWithdrawals: number;
  TotalIncome: number;
  /** Efectivo reintegrado por devoluciones en la sesión: resta al esperado. */
  TotalReturns: number;
  /** Neto del turno por medio de pago (para cuadrar QR y tarjeta contra el banco). */
  ByPaymentMethod: PaymentMethodTotal[];
  /** Arqueo del cierre por medio. Vacío mientras está abierta. */
  Counts: CashCount[];
  /** Cierres rechazados por diferencia sin observación antes del definitivo. */
  CloseAttempts: number;
  /** Supervisor que autorizó el cierre, si lo necesitó. */
  CloseAuthorizedBy: number | null;
  CloseAuthorizedByName: string;
  /** Conteo del efectivo por billete y moneda, si se declaró. */
  Denominations?: DenominationCount[];
  /** Solo en un cierre rechazado: 'note' o 'supervisor', lo que falta para cerrar. */
  CloseRequires: '' | 'note' | 'supervisor';
  Movements: CashMovement[];
}

export interface OpenCashSessionRequest {
  OpeningAmount: number;
}

export interface CloseCashSessionRequest {
  /** Solo para clientes anteriores al arqueo por medio; si va Counts, se ignora. */
  DeclaredAmount: number;
  Notes: string;
  /** Lo declarado por cada medio que se arquea, sin ver lo esperado. */
  Counts: { PaymentMethodId: string; Declared: number }[];
  /** Efectivo por billete y moneda; si va, tiene que sumar lo declarado en efectivo. */
  Denominations?: DenominationCount[];
  /** Sesión del supervisor que autoriza un cierre con diferencia grande o intentos agotados. */
  SupervisorAuthToken?: string;
}

/** Configuración del cierre de caja de la empresa (Settings/cash-close). */
export interface CashCloseSettings {
  NoteThreshold: number;
  SupervisorThreshold: number | null;
  MaxAttempts: number | null;
  RequireDenominations: boolean;
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
