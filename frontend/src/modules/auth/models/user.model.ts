/** Sucursal en la que el usuario está habilitado. */
export interface BranchOption {
  BranchId: string;
  Name: string;
  IsDefault: boolean;
}

export class User {
  public Uuid: string = '';
  public SesionId: number = 0;
  public FullName: string = '';
  public UserName: string = '';
  public Email: string = '';
  public ChangePassword: boolean = false;
  public Token: string = '';
  public Id: number = 0;
  public RolId: number = 0;
  public RolName: string = '';
  public RequireTotp: boolean = false;
  public TotpSetupRequired: boolean = false;
  public TotpSessionToken: string = '';
  /** Sucursal activa de la sesión: todo lo que se registra queda en ella. */
  public BranchId: string = '';
  public BranchName: string = '';
  public Branches: BranchOption[] = [];
}

