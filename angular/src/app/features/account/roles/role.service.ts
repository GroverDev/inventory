import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import { AppForm } from '../forms/form.model';
import { Role } from './role.model';

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly api = inject(ApiService);

  getAll(nameRol = '', description = '') {
    return this.api.get<ResponseArray<Role>>(
      `Roles?nameRol=${encodeURIComponent(nameRol)}&description=${encodeURIComponent(description)}`);
  }

  getById(id: number) {
    return this.api.get<ResponseObject<Role>>(`Roles/${id}`);
  }

  create(role: Role) {
    return this.api.post<ResponseObject<number>>('Roles', { nameRol: role.NameRol, description: role.Description });
  }

  update(role: Role) {
    return this.api.put<ResponseObject<boolean>>(`Roles/${role.Id}`, { nameRol: role.NameRol, description: role.Description });
  }

  remove(id: number) {
    return this.api.del<ResponseObject<boolean>>(`Roles/${id}`);
  }

  getForms(rolId: number) {
    return this.api.get<ResponseArray<AppForm>>(`Roles/${rolId}/forms`);
  }

  assignForms(rolId: number, formIds: number[]) {
    return this.api.post<ResponseObject<boolean>>(`Roles/${rolId}/forms`, { rolId, formIds });
  }
}
