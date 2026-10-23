export class User {
  Uuid: string = '';
  UserName: string = '';
  ChangePassword?: boolean;
  IsActive: boolean = true;
  LastAccess?: Date;
  Email: string = '';
  FullName: string = '';
  Password?: string;
  MfaEnabled: boolean = false;
  MfaRequired: boolean = false;
}


