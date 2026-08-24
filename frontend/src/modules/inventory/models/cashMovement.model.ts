export class CashMovement {
  public Id: string = '';
  public CashSessionId: string = '';
  public MovementType: 'expense' | 'withdrawal' | 'income' | 'return' = 'expense';
  public Amount: number = 0;
  public Description: string = '';
  public Created: string = '';
  public CreatedByName: string = '';
}

export class CashMovementRequest {
  public CashSessionId: string = '';
  public MovementType: 'expense' | 'withdrawal' | 'income' = 'expense';
  public Amount: number = 0;
  public Description: string = '';
}

export const MovementTypeLabels: Record<string, string> = {
  expense: 'Gasto',
  withdrawal: 'Retiro',
  income: 'Ingreso',
  // Lo genera la devolución, no el usuario: no aparece en el alta de movimientos.
  return: 'Devolución',
};

export const MovementTypeIcons: Record<string, string> = {
  expense: 'fal fa-receipt',
  withdrawal: 'fal fa-arrow-circle-up',
  income: 'fal fa-arrow-circle-down',
  return: 'fal fa-undo',
};
