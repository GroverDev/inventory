import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import { UserBranch } from '../branches/branch.model';
import { Role } from '../roles/role.model';
import { AppUser } from './user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly api = inject(ApiService);

  getByName(fullName: string, email: string, includeInactive = false) {
    return this.api.post<ResponseArray<AppUser>>('Users/GetUsers', {
      Email: email,
      FullName: fullName,
      // true = solo activos, null = todos (activos + inactivos)
      IsActive: includeInactive ? null : true,
    });
  }

  create(user: AppUser) {
    return this.api.post<ResponseObject<string>>('Users', { email: user.Email, password: user.Password, fullName: user.FullName });
  }

  update(user: AppUser) {
    return this.api.put<ResponseObject<boolean>>(`Users/${user.Uuid}`, {
      userName: user.UserName, email: user.Email, fullName: user.FullName,
    });
  }

  getById(uuid: string) {
    return this.api.get<ResponseObject<AppUser>>(`Users/${uuid}`);
  }

  remove(uuid: string) {
    return this.api.del<ResponseObject<boolean>>(`Users/${uuid}`);
  }

  getRoles(uuid: string) {
    return this.api.get<ResponseArray<Role>>(`Users/${uuid}/roles`);
  }

  assignRoles(uuid: string, roleIds: number[]) {
    return this.api.put<ResponseObject<boolean>>(`Users/${uuid}/roles`, { roleIds });
  }

  getBranches(uuid: string) {
    return this.api.get<ResponseArray<UserBranch>>(`Users/${uuid}/branches`);
  }

  assignBranches(uuid: string, branchIds: string[], defaultBranchId: string) {
    return this.api.put<ResponseObject<boolean>>(`Users/${uuid}/branches`, { branchIds, defaultBranchId });
  }

  changePassword(uuid: string, newPassword: string) {
    return this.api.put<ResponseObject<boolean>>(`Users/${uuid}/password`, { newPassword });
  }

  resetMfa(uuid: string) {
    return this.api.post<ResponseObject<boolean>>(`Users/${uuid}/mfa/reset`, {});
  }

  requireMfa(uuid: string) {
    return this.api.put<ResponseObject<boolean>>(`Users/${uuid}/mfa/required`, {});
  }

  unrequireMfa(uuid: string) {
    return this.api.del<ResponseObject<boolean>>(`Users/${uuid}/mfa/required`);
  }
}
