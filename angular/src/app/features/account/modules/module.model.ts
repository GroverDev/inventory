export interface AppModule {
  Id: number;
  NameModule: string;
  ShowOrder: number;
  Route: string;
  IconCss: string;
}

export const newModule = (): AppModule => ({ Id: 0, NameModule: '', ShowOrder: 0, Route: '', IconCss: '' });
