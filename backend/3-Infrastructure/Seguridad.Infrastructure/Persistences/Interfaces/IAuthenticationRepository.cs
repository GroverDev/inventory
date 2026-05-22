using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public interface IAuthenticationRepository
{
    Task<LoginResponse> Login(LoginRequest login);
    Task<LoginResponse> CompleteLoginWithTotp(int userId, TotpVerifyRequest request);
}
