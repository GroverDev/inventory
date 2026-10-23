/** Sucursal en la que el usuario está habilitado. */
export interface BranchOption {
  BranchId: string;
  Name: string;
  IsDefault: boolean;
}

export class User {
  Uuid = '';
  SesionId = 0;
  FullName = '';
  UserName = '';
  Email = '';
  ChangePassword = false;
  Token = '';
  Id = 0;
  RolId = 0;
  RolName = '';
  RequireTotp = false;
  TotpSetupRequired = false;
  TotpSessionToken = '';
  /** Sucursal activa de la sesión: todo lo que se registra queda en ella. */
  BranchId = '';
  BranchName = '';
  Branches: BranchOption[] = [];
}
