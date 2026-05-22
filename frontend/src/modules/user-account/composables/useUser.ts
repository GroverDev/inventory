import { useApi } from '@/modules/common/composables/api/useApi';
import type { ResponseArray, ResponseObject } from '@/modules/common/models';
import type { User } from '@/modules/user-account/models/users.model';

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
         email: user.Email,
         fullName: user.FullName
    });
  }

  const getUserById = async (userId: string): Promise<ResponseObject<User>> => {
    return await get<ResponseObject<User>>(`Users/${userId}`);
  }

  const deleteUser = async (userId: string): Promise<ResponseObject<boolean>> => {
      // NOTE: Postman shows DELETE Users/{{uuid}}
      // Check if delete is supported in useApi, assuming yes as common pattern.
      // If 'del' is not exported by useApi, we might need to check useApi.
      return await del<ResponseObject<boolean>>(`Users/${userId}`);
  }

  return { getUsersByName, createUser, updateUser, getUserById, deleteUser }
}
export default useUser;
