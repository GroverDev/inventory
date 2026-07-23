using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public interface IRolesRepository
{
    Task<List<Roles>> GetRolesXUserId(int userId);
    Task<List<Roles>> GetRoles(RolSearchRequest rolSearch);
    Task<Roles> GetRoleById(int id);
    Task<int> CreateRole(Roles role);
    Task<int> UpdateRole(Roles role);
    Task<int> DeleteRole(int id, int modifiedBy);
    Task AssignFormsToRole(int rolId, List<int> formIds, int userId);
    Task<bool> HasFormPermission(int userId, string formRoute, string action);
}
