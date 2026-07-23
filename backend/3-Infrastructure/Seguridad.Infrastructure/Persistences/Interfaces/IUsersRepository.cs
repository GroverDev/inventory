using Seguridad.Domain;
using Seguridad.Domain.Requests;

namespace Seguridad.Infrastructure;

public interface IUsersRepository
{
    public Task<List<UsersResponse>> GetUsers(UserSearchRequest userSearchRequest);
    public Task<string> CreateUser(Users user, int userId);
    public Task<bool> CreateUserOutPassword(Users user, int userId);
    public Task<UsersResponse> GetUser(Guid uuid);
    public Task<bool> UpdateUser(Users user, int modifiedBy);
    public Task<bool> DeleteUser(Guid uuid, int modifiedBy);
    public Task<List<Roles>> GetRolesByUserUuid(Guid uuid);
    public Task AssignRolesToUser(Guid uuid, List<int> roleIds, int modifiedBy);
    public Task<bool> ChangeUserPassword(Guid uuid, string hashedPassword, int modifiedBy);
    public Task<bool> ChangeOwnPassword(int userId, string currentPassword, string newHashedPassword);
}
