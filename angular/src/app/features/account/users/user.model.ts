export interface AppUser {
  Uuid: string;
  UserName: string;
  ChangePassword?: boolean;
  IsActive: boolean;
  LastAccess?: string;
  Email: string;
  FullName: string;
  Password?: string;
  MfaEnabled: boolean;
  MfaRequired: boolean;
}

export const newUser = (): AppUser => ({
  Uuid: '', UserName: '', IsActive: true, Email: '', FullName: '', MfaEnabled: false, MfaRequired: false,
});
