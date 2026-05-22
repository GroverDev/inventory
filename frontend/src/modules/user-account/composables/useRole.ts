import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models';
import type { Role, RoleFormAssignment } from '@/modules/user-account/models/role.model';
import type { Form } from '@/modules/user-account/models/form.model';

const { post, get, put, del } = useApi();

const useRole = () => {

  const getRoles = async (nameRol: string = '', description: string = ''): Promise<ResponseArray<Role>> => {
    return await get<ResponseArray<Role>>(`Roles?nameRol=${nameRol}&description=${description}`);
  };

  const getRoleById = async (id: number): Promise<ResponseObject<Role>> => {
    return await get<ResponseObject<Role>>(`Roles/${id}`);
  };

  const createRole = async (role: Role): Promise<ResponseObject<number>> => {
    return await post<ResponseObject<number>>('Roles', {
      nameRol: role.NameRol,
      description: role.Description,
    });
  };

  const updateRole = async (role: Role): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Roles/${role.Id}`, {
      nameRol: role.NameRol,
      description: role.Description,
    });
  };

  const deleteRole = async (id: number): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`Roles/${id}`);
  };

  const getFormsByRole = async (rolId: number): Promise<ResponseArray<Form>> => {
    return await get<ResponseArray<Form>>(`Roles/${rolId}/forms`);
  };

  const assignForms = async (assignment: RoleFormAssignment): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>(`Roles/${assignment.RolId}/forms`, {
      rolId: assignment.RolId,
      formIds: assignment.FormIds,
    });
  };

  return { getRoles, getRoleById, createRole, updateRole, deleteRole, getFormsByRole, assignForms };
};

export default useRole;
