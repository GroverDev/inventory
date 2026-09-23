export class Branch {
  public Id: string = '';
  public Name: string = '';
  public Code: string = '';
  public Address: string = '';
  public Phone: string = '';
  public IsActive: boolean = true;
  /** Usuarios activos habilitados en la sucursal. Solo lectura. */
  public UsersCount: number = 0;
}

/** Una sucursal de la farmacia y si el usuario está habilitado en ella. */
export interface UserBranch {
  BranchId: string;
  Name: string;
  Enabled: boolean;
  IsDefault: boolean;
}
