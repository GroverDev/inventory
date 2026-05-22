using Common.Utilities;
using Seguridad.Domain;

namespace Seguridad.Application;

public interface IAuthenticationApplication
{
    Task<Response<LoginResponse>> Login(LoginRequest login);
}
