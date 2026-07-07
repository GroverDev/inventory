using Common.Utilities;
using Seguridad.Domain.Entities.requests;

namespace Seguridad.Application;

public interface IAdminApplication
{
    Task<Response<bool>> ResetCompany(ResetCompanyRequest request, int userId);
}
