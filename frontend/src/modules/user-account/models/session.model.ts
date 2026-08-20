export class Session {
  Id: number = 0;
  Device: string = '';
  LoginFrom: string = '';
  CreatedAt: string = '';
  ExpiresAt: string = '';
}

export class ConnectedUser extends Session {
  Uuid: string = '';
  FullName: string = '';
  Email: string = '';
}
