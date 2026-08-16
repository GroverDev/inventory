import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseObject } from '@/modules/common/models';

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

const { post } = useApi();

const useAdmin = () => {

  const resetCompany = async (payload: ResetCompanyPayload): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>('Admin/ResetCompany', payload);
  };

  /**
   * Crea la farmacia, su rol SuperAdmin con todos los permisos, sus datos
   * maestros y su usuario administrador, todo en una transacción del servidor.
   * Es una operación de plataforma: el backend exige `is_platform_admin`, que
   * no es lo mismo que ser SuperAdmin de una farmacia.
   */
  const createTenant = async (payload: CreateTenantPayload): Promise<ResponseObject<CreateTenantResult>> => {
    return await post<ResponseObject<CreateTenantResult>>('Admin/tenants', payload);
  };

  return { resetCompany, createTenant };
};

export default useAdmin;
