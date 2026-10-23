export interface Role {
  Id: number;
  NameRol: string;
  Description: string;
  State: boolean;
}

export const newRole = (): Role => ({ Id: 0, NameRol: '', Description: '', State: true });
