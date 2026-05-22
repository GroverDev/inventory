export class User {
  public Uuid: string = '';
  public SesionId: number = 0;
  public FullName: string = '';
  public UserName: string = '';
  public Email: string = '';
  public ChangePassword: boolean = false;
  public Token: string = '';
  public Id: number = 0;
public RequireTotp: boolean = false;
  public TotpSetupRequired: boolean = false;
  public TotpSessionToken: string = '';

}
