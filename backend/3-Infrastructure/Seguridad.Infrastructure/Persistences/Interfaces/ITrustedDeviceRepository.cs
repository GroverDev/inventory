using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public interface ITrustedDeviceRepository
{
    /// <summary>Persiste un token de dispositivo de confianza nuevo y devuelve su id.</summary>
    Task<long> Create(int userId, int tenantId, string tokenHash, string deviceLabel, DateTime expiresAt);

    Task<TrustedDevice?> GetByHash(string tokenHash);

    /// <summary>
    /// Revoca todos los dispositivos de confianza del usuario. Se usa ante
    /// cambio de contraseña o reset de MFA: ninguno debe sobrevivir a eso.
    /// </summary>
    Task RevokeAllForUser(int userId);

    /// <summary>Dispositivos activos del propio usuario, para su autogestión.</summary>
    Task<List<TrustedDeviceResponse>> GetActiveForUser(int userId);

    /// <summary>Fila puntual, solo si pertenece al usuario indicado.</summary>
    Task<TrustedDevice?> GetByIdForUser(long id, int userId);

    /// <summary>Revoca un dispositivo puntual.</summary>
    Task Revoke(long id);
}
