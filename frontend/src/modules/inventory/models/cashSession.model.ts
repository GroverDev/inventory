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
