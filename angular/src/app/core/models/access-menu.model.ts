export interface AccessMenu {
  IdFormulario: number;
  IdFormularioPadre: number;
  titulo: string;
  url: string;
  SeMuestraEnMenu: boolean;
  EsFormulario: boolean;
  CanCreate: boolean;
  CanRead: boolean;
  CanUpdate: boolean;
  CanDelete: boolean;
  Children: AccessMenu[];
}
