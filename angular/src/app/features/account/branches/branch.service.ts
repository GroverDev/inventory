import { Injectable, inject } from '@angular/core';

import { ResponseArray, ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';
import { Branch } from './branch.model';

@Injectable({ providedIn: 'root' })
export class BranchService {
  private readonly api = inject(ApiService);

  getAll(name = '', includeInactive = false) {
    return this.api.get<ResponseArray<Branch>>(`Branch?name=${encodeURIComponent(name)}&includeInactive=${includeInactive}`);
  }

  getById(id: string) {
    return this.api.get<ResponseObject<Branch>>(`Branch/${id}`);
  }

  create(branch: Branch) {
    return this.api.post<ResponseObject<string>>('Branch', this.body(branch));
  }

  update(branch: Branch) {
    return this.api.put<ResponseObject<boolean>>(`Branch/${branch.Id}`, { id: branch.Id, ...this.body(branch) });
  }

  remove(id: string) {
    return this.api.del<ResponseObject<boolean>>(`Branch/${id}`);
  }

  private body(branch: Branch) {
    return {
      name: branch.Name,
      code: branch.Code,
      address: branch.Address,
      phone: branch.Phone,
      isActive: branch.IsActive,
    };
  }
}
