using Common.Utilities;
using Seguridad.Domain;

namespace Seguridad.Application;

public interface IAccessMenuApplication
{
    public Task<Response<List<AccessMenu>>> GetAccesMenuXUserId(int userId);
}
