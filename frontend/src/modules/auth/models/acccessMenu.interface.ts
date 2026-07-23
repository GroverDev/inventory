

export enum ClassIcon {
  Ninguno = "ninguno",
}

export enum ClassItem {
  NavLinkCollapsed = "nav-link collapsed",
  NavLinkLink = "nav-link link",
}

export class AccessMenu {
  public IdFormulario: number = 0;
  public IdFormularioPadre: number = 0;
  public titulo: string = '';
  public classIcon: ClassIcon = ClassIcon.Ninguno;
  public classItem: ClassItem = ClassItem.NavLinkCollapsed;
  public dataToggle: boolean = false;
  public dataTarget: number = 0;
  public identacion: string = '';
  public url: string = '';
  public SeMuestraEnMenu: boolean = false;
  public EsFormulario: boolean = false;
  public CanCreate: boolean = false;
  public CanRead: boolean = false;
  public CanUpdate: boolean = false;
  public CanDelete: boolean = false;
  public Children: AccessMenu[] = [];

}
