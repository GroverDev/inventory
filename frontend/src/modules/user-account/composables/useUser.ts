import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models';
import type { User } from '@/modules/user-account/models/users.model';
import type { Role } from '@/modules/user-account/models/role.model';

const { post, get, put, del } = useApi();

const useUser = () => {

  const getUsersByName = async (searchUserName: string, searchEmail: string): Promise<ResponseArray<User>> => {
    return await post<ResponseArray<User>>(
      `Users/GetUsers`,
      {
        Email: searchEmail,
        FullName: searchUserName,
        // IsActive: true // Postman example body: { "email": "", "fullName": "" }
      }
    );
  }

  const createUser = async (user: User): Promise<ResponseObject<string>> => {
    return await post<ResponseObject<string>>('Users', {
      email: user.Email,
      password: user.Password,
      fullName: user.FullName
    });
  }

  const updateUser = async (user: User): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Users/${user.Uuid}`, {
         userName: user.UserName,
         email: user.Email,
         fullName: user.FullName
    });
  }

  const getUserById = async (userId: string): Promise<ResponseObject<User>> => {
    return await get<ResponseObject<User>>(`Users/${userId}`);
  }

  const deleteUser = async (userId: string): Promise<ResponseObject<boolean>> => {
      return await del<ResponseObject<boolean>>(`Users/${userId}`);
  }

  const getUserRoles = async (uuid: string): Promise<ResponseArray<Role>> => {
    return await get<ResponseArray<Role>>(`Users/${uuid}/roles`);
  }

  const assignRolesToUser = async (uuid: string, roleIds: number[]): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Users/${uuid}/roles`, { roleIds });
  }

  const changeUserPassword = async (uuid: string, newPassword: string): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Users/${uuid}/password`, { newPassword });
  }

  const adminResetMfa = async (uuid: string): Promise<ResponseObject<boolean>> => {
    return await post<ResponseObject<boolean>>(`Users/${uuid}/mfa/reset`, {});
  }

  const adminRequireMfa = async (uuid: string): Promise<ResponseObject<boolean>> => {
    return await put<ResponseObject<boolean>>(`Users/${uuid}/mfa/required`, {});
  }

  const adminUnrequireMfa = async (uuid: string): Promise<ResponseObject<boolean>> => {
    return await del<ResponseObject<boolean>>(`Users/${uuid}/mfa/required`);
  }

  return { getUsersByName, createUser, updateUser, getUserById, deleteUser, getUserRoles, assignRolesToUser, changeUserPassword, adminResetMfa, adminRequireMfa, adminUnrequireMfa }
}
export default useUser;
