export interface Branch {
  Id: string;
  Name: string;
  Code: string;
  Address: string;
  Phone: string;
  IsActive: boolean;
  /** Usuarios activos habilitados en la sucursal. Solo lectura. */
  UsersCount: number;
}

export const newBranch = (): Branch => ({
  Id: '',
  Name: '',
  Code: '',
  Address: '',
  Phone: '',
  IsActive: true,
  UsersCount: 0,
});

/** Una sucursal de la farmacia y si el usuario está habilitado en ella. */
export interface UserBranch {
  BranchId: string;
  Name: string;
  Enabled: boolean;
  IsDefault: boolean;
}
