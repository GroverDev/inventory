import { Injectable, inject } from '@angular/core';

import { ResponseObject } from '../../../core/models/api.models';
import { ApiService } from '../../../core/services/api.service';

export interface ResetCompanyPayload {
  CurrentPassword: string;
  ConfirmationPhrase: string;
  SkipBackup: boolean;
}

/** Alta de una farmacia con su administrador inicial. */
export interface CreateTenantPayload {
  Name: string;
  Slug: string;
  AdminEmail: string;
  AdminFullName: string;
  AdminPassword: string;
}

export interface CreateTenantResult {
  TenantId: number;
  Name: string;
  Slug: string;
  AdminEmail: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly api = inject(ApiService);

  resetCompany(payload: ResetCompanyPayload) {
    return this.api.post<ResponseObject<boolean>>('Admin/ResetCompany', payload);
  }

  /**
   * Crea la farmacia, su rol SuperAdmin con todos los permisos, sus datos
   * maestros y su usuario administrador, todo en una transacción del servidor.
   * Es una operación de plataforma: el backend exige `is_platform_admin`, que
   * no es lo mismo que ser SuperAdmin de una farmacia.
   */
  createTenant(payload: CreateTenantPayload) {
    return this.api.post<ResponseObject<CreateTenantResult>>('Admin/tenants', payload);
  }
}
