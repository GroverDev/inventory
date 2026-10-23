export class Discount {
  public Id: string = '';
  public Name: string = '';
  public Type: string = '';  // 'Percentage' | 'FixedAmount'
  public Value: number = 0;
  public Description: string = '';
  public IsActive: boolean = true;
}
