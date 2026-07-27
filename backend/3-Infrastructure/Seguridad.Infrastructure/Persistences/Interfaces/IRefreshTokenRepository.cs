using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public interface IRefreshTokenRepository
{
    /// <summary>Persiste un refresh token nuevo y devuelve su id.</summary>
    Task<long> Create(int userId, string tokenHash, string device, string loginFrom, DateTime expiresAt);

    Task<RefreshToken?> GetByHash(string tokenHash);

    /// <summary>Marca el token como revocado, opcionalmente indicando cuál lo reemplazó.</summary>
    Task Revoke(long id, long? replacedBy);

    /// <summary>
    /// Revoca todos los tokens activos del usuario. Se usa ante reuso de un
    /// token ya rotado, que delata que alguien copió el token.
    /// </summary>
    Task RevokeAllForUser(int userId);

    /// <summary>Datos del usuario necesarios para emitir un JWT nuevo.</summary>
    Task<LoginResponse?> GetLoginDataForRefresh(int userId, string device, string loginFrom);
}
