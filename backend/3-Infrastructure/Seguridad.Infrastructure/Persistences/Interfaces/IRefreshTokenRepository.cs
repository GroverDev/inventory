using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public interface IRefreshTokenRepository
{
    /// <summary>Persiste un refresh token nuevo y devuelve su id.</summary>
    Task<long> Create(int userId, int tenantId, Guid branchId, int sessionId, string tokenHash, string device, string loginFrom, DateTime expiresAt);

    Task<RefreshToken?> GetByHash(string tokenHash);

    /// <summary>Fila puntual, solo si pertenece al tenant indicado.</summary>
    Task<RefreshToken?> GetByIdForTenant(long id, int tenantId);

    /// <summary>Sesiones activas (no revocadas, no vencidas) de un usuario, para su ficha de administración.</summary>
    Task<List<SessionResponse>> GetActiveForUser(int userId, int tenantId);

    /// <summary>Sesiones activas de todo el tenant, con datos del usuario, para el panel de "usuarios conectados".</summary>
    Task<List<ConnectedUserResponse>> GetActiveForTenant(int tenantId);

    /// <summary>
    /// Cambia la sucursal de la sesión vigente (la del SessionId del JWT), para
    /// que el próximo refresh la conserve. Devuelve las filas afectadas: 0 si la
    /// sesión no tiene refresh token (Postman, o 2FA a medio configurar).
    /// </summary>
    Task<int> SetBranchForSession(int userId, int sessionId, Guid branchId);

    /// <summary>Marca el token como revocado, opcionalmente indicando cuál lo reemplazó.</summary>
    Task Revoke(long id, long? replacedBy);

    /// <summary>
    /// Revoca todos los tokens activos del usuario y devuelve los SessionId que
    /// quedaron sin vigencia. Se usa ante reuso de un token ya rotado, que
    /// delata que alguien copió el token.
    /// </summary>
    Task<List<int>> RevokeAllForUser(int userId);

    /// <summary>Igual que <see cref="RevokeAllForUser"/>, acotado al tenant del admin que la pide.</summary>
    Task<List<int>> RevokeAllForUserInTenant(int userId, int tenantId);

    /// <summary>
    /// Datos del usuario necesarios para emitir un JWT nuevo. Conserva
    /// <paramref name="preferredBranch"/> si el usuario sigue habilitado en ella.
    /// </summary>
    Task<LoginResponse?> GetLoginDataForRefresh(int userId, string device, string loginFrom, Guid? preferredBranch);
}
