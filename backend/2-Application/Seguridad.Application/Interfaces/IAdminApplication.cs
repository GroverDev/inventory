using Common.Utilities;
using Seguridad.Domain.Entities.requests;
using Seguridad.Domain.Entities.responses;

namespace Seguridad.Application;

public interface IAdminApplication
{
    Task<Response<bool>> ResetCompany(ResetCompanyRequest request, int userId);

    Task<Response<CreateTenantResponse>> CreateTenant(CreateTenantRequest request, int userId);
}
