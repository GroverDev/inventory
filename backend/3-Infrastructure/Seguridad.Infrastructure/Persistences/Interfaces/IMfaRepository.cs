using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public interface IMfaRepository
{
    Task<MfaInfo?> GetTotpMfa(int userId);
    Task<MfaInfo?> GetTotpMfaByUuid(Guid userUuid);
    Task<int?> GetUserIdByUuid(Guid userUuid);
    Task UpsertTotpSecret(int userId, string encryptedSecret);
    Task<List<string>> ActivateTotp(int userId);
    Task DisableTotp(int userId);
    Task RecordFailure(int mfaId, int maxAttempts, int lockoutMinutes);
    Task ResetAttempts(int mfaId);
    Task<bool> UseRecoveryCode(int userId, string normalizedCode);
    Task AdminResetMfa(int userId);
    Task AdminSetRequired(int userId, bool required);
}
