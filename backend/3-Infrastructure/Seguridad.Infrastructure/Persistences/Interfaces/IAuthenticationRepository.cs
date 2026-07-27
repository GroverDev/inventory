using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public interface IAuthenticationRepository
{
    Task<LoginResponse> Login(LoginRequest login);
    Task<LoginResponse> CompleteLoginWithTotp(int userId, TotpVerifyRequest request);

    /// <summary>
    /// Intentos fallidos de <paramref name="email"/> dentro de los últimos
    /// <paramref name="withinMinutes"/> minutos, contados desde el último
    /// login exitoso (un acceso correcto reinicia la cuenta).
    /// </summary>
    Task<int> RecentFailedAttempts(string email, int withinMinutes);
}
