using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public interface IRefreshTokenRepository
{
    /// <summary>Persiste un refresh token nuevo y devuelve su id.</summary>
    Task<long> Create(int userId, int tenantId, int sessionId, string tokenHash, string device, string loginFrom, DateTime expiresAt);

    Task<RefreshToken?> GetByHash(string tokenHash);

    /// <summary>Fila puntual, solo si pertenece al tenant indicado.</summary>
    Task<RefreshToken?> GetByIdForTenant(long id, int tenantId);

    /// <summary>Sesiones activas (no revocadas, no vencidas) de un usuario, para su ficha de administración.</summary>
    Task<List<SessionResponse>> GetActiveForUser(int userId, int tenantId);

    /// <summary>Sesiones activas de todo el tenant, con datos del usuario, para el panel de "usuarios conectados".</summary>
    Task<List<ConnectedUserResponse>> GetActiveForTenant(int tenantId);

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

    /// <summary>Datos del usuario necesarios para emitir un JWT nuevo.</summary>
    Task<LoginResponse?> GetLoginDataForRefresh(int userId, string device, string loginFrom);
}
