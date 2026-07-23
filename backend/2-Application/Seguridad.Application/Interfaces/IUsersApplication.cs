using Common.Utilities;
using Seguridad.Domain;
using Seguridad.Domain.Entities.requests;
using Seguridad.Domain.Requests;

namespace Seguridad.Application;

public interface IUsersApplication
{
    public Task<Response<List<UsersResponse>>> GetUsers(UserSearchRequest userSearchRequest);
    public Task<Response<string>> CreateUser(UserRequest user, int userId);
    public Task<Response<UsersResponse>> GetUser(Guid uuid);
    public Task<Response<bool>> UpdateUser(Guid uuid, UserUpdateRequest user, int modifiedBy);
    public Task<Response<bool>> DeleteUser(Guid uuid, int modifiedBy);
    public Task<Response<bool>> ChangeUserPassword(Guid uuid, string newPassword, int modifiedBy);
    public Task<Response<bool>> ChangeOwnPassword(int userId, string currentPassword, string newPassword);
    public Task<Response<bool>> AdminResetMfa(Guid userUuid);
    public Task<Response<bool>> AdminSetMfaRequired(Guid userUuid, bool required);
    public Task<Response<List<Roles>>> GetRolesByUser(Guid uuid);
    public Task<Response<bool>> AssignRolesToUser(Guid uuid, List<int> roleIds, int modifiedBy);
}
