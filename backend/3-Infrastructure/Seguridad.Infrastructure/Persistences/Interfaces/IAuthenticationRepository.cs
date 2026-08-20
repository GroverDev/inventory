using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public interface IAuthenticationRepository
{
    Task<LoginResponse> Login(LoginRequest login);
    Task<LoginResponse> CompleteLoginWithTotp(int userId, TotpVerifyRequest request);

    /// <summary>
    /// Registra el login exitoso (auditoría en sec.users_login + last_access) y
    /// devuelve el id de sesión. Lo usa el camino de dispositivo de confianza:
    /// salta el TOTP, pero necesita el mismo registro que <see cref="Login"/> o
    /// <see cref="CompleteLoginWithTotp"/> hacen al completar sesión, o el JWT
    /// queda con SessionId 0 y todo endpoint autenticado lo rechaza.
    /// </summary>
    Task<int> RecordSuccessfulLogin(LoginRequest login, int userId);

    /// <summary>
    /// Intentos fallidos de <paramref name="email"/> dentro de los últimos
    /// <paramref name="withinMinutes"/> minutos, contados desde el último
    /// login exitoso (un acceso correcto reinicia la cuenta).
    /// </summary>
    Task<int> RecentFailedAttempts(string email, int withinMinutes);
}
