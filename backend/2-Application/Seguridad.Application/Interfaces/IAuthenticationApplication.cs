using Common.Utilities;
using Seguridad.Domain;

namespace Seguridad.Application;

public interface IAuthenticationApplication
{
    Task<Response<LoginResponse>> Login(LoginRequest login);

    /// <summary>
    /// Intentos fallidos de esa cuenta dentro de la ventana de
    /// <see cref="LoginSettings.LockoutMinutes"/>, posteriores al último acceso
    /// correcto. Es la misma señal que usa el bloqueo por cuenta; se expone
    /// para decidir si el login debe resolver un captcha antes de seguir.
    /// </summary>
    Task<int> RecentFailedAttempts(string email);

    /// <summary>
    /// Registra el login exitoso (auditoría + last_access) para el camino de
    /// dispositivo de confianza, que salta el TOTP y por eso no pasa por el
    /// registro que hacen Login o el flujo de verificación TOTP. Devuelve el id
    /// de sesión a embeber en el JWT.
    /// </summary>
    Task<int> RecordSuccessfulLogin(LoginRequest login, int userId);

    /// <summary>
    /// Emite y persiste un refresh token nuevo. Devuelve el valor en claro,
    /// que solo se entrega al cliente en esta llamada. <paramref name="sessionId"/>
    /// es el SessionId (sec.users_login) vigente al emitirlo: queda ligado a la
    /// fila para poder revocar en memoria el access token correspondiente si
    /// esta sesión se cierra desde el panel de administración.
    /// </summary>
    Task<string> IssueRefreshToken(int userId, int tenantId, int sessionId, string device, string loginFrom, int days);

    /// <summary>
    /// Canjea un refresh token por datos de sesión frescos y rota el token.
    /// El <see cref="LoginResponse.Token"/> (JWT) lo completa el controlador.
    /// </summary>
    Task<Response<LoginResponse>> Refresh(RefreshTokenRequest request, int days);

    /// <summary>Revoca un refresh token (cierre de sesión explícito).</summary>
    Task<Response<bool>> RevokeRefreshToken(string refreshToken);

    /// <summary>
    /// Emite y persiste un token de dispositivo de confianza. Devuelve el valor
    /// en claro, que solo se entrega al cliente en esta llamada.
    /// </summary>
    Task<string> IssueTrustedDevice(int userId, int tenantId, string device, int days);

    /// <summary>
    /// True si el token corresponde a un dispositivo de confianza vigente y no
    /// revocado de ese usuario. Cualquier duda (token vacío, hash sin
    /// coincidencia, vencido, revocado, de otro usuario) resuelve en false.
    /// </summary>
    Task<bool> IsTrustedDevice(int userId, string rawToken);

    /// <summary>
    /// Revoca todos los dispositivos de confianza del usuario. Se usa ante
    /// cambio de contraseña o reset de MFA.
    /// </summary>
    Task RevokeAllTrustedDevicesForUser(int userId);

    /// <summary>Dispositivos de confianza activos del propio usuario, para su autogestión.</summary>
    Task<Response<List<TrustedDeviceResponse>>> GetTrustedDevices(int userId);

    /// <summary>Olvida un dispositivo de confianza puntual, solo si pertenece al propio usuario.</summary>
    Task<Response<bool>> RevokeTrustedDevice(long id, int userId);

    /// <summary>Sesiones activas de un usuario, para su ficha de administración.</summary>
    Task<Response<List<SessionResponse>>> GetActiveSessions(int userId, int tenantId);

    /// <summary>Sesiones activas de todo el tenant, con datos del usuario, para el panel de "usuarios conectados".</summary>
    Task<Response<List<ConnectedUserResponse>>> GetConnectedUsers(int tenantId);

    /// <summary>
    /// Cierra una sesión puntual: revoca su refresh token y tumba de inmediato
    /// el access token ya emitido para ese SessionId.
    /// </summary>
    Task<Response<bool>> CloseSession(long id, int tenantId);

    /// <summary>Cierra todas las sesiones activas de un usuario dentro del tenant del admin que la pide.</summary>
    Task<Response<bool>> CloseAllSessions(int userId, int tenantId);
}
