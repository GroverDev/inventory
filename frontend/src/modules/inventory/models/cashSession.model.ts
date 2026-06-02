import type { CashMovement } from './cashMovement.model';

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
  public TotalSales: number = 0;
  public TotalExpenses: number = 0;
  public TotalWithdrawals: number = 0;
  public TotalIncome: number = 0;
  public Movements: CashMovement[] = [];
}

export class OpenCashSessionRequest {
  public OpeningAmount: number = 0;
}

export class CloseCashSessionRequest {
  public DeclaredAmount: number = 0;
  public Notes: string = '';
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
  Detail: SessionSaleDetail[];
  Payments: SessionSalePayment[];
}
