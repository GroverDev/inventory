export class PaymentMethod {
  public Id: string = '';
  public Name: string = '';
  public IconCss: string = '';
  public RequiresChanges: boolean = false;
  /** Si el cobro entra al cajón. Un reintegro por este medio mueve la caja. */
  public AffectsCash: boolean = false;
}

export class SalePayment {
  public Id: string = '';
  public PaymentMethodId: string = '';
  public PaymentMethodName: string = '';
  public IconCss: string = '';
  public AmountGiven: number = 0;
  public AmountReturned: number = 0;
}
