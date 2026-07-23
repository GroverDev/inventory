using Common.Utilities;
using Seguridad.Domain;

namespace Seguridad.Application;

public interface IRolesApplication
{
    Task<Response<List<Roles>>> GetRolesXUserId(int userId);
    Task<Response<List<Roles>>> GetRoles(RolSearchRequest rolSearch);
    Task<Response<Roles>> GetRoleById(int id);
    Task<Response<int>> CreateRole(RolesRequest request, int createdBy);
    Task<Response<bool>> UpdateRole(RolesRequest request, int modifiedBy);
    Task<Response<bool>> DeleteRole(int id, int modifiedBy);
    Task<Response<bool>> AssignFormsToRole(RolesFormsRequest request, int userId);
    Task<bool> HasFormPermission(int userId, string formRoute, string action);
}
