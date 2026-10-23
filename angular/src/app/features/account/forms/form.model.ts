export interface AppForm {
  Id: number;
  FormId: number;
  NameForm: string;
  Description: string;
  Orden: number;
  Route: string;
  Controller: string;
  IconCss: string;
  ShowMenu: boolean;
  IsFormRegister: boolean;
  ModuleId: number;
}

export const newForm = (): AppForm => ({
  Id: 0, FormId: 0, NameForm: '', Description: '', Orden: 0, Route: '', Controller: '',
  IconCss: '', ShowMenu: true, IsFormRegister: false, ModuleId: 0,
});
