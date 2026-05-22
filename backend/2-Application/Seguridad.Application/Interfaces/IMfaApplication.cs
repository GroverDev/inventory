using Common.Utilities;
using Seguridad.Domain;

namespace Seguridad.Application;

public interface IMfaApplication
{
    Task<Response<TotpSetupResponse>> SetupTotp(int userId, string userEmail);
    Task<Response<MfaEnableResponse>> EnableTotp(int userId, string code);
    Task<Response<LoginResponse>> VerifyTotpAndCompleteLogin(int userId, TotpVerifyRequest request);
    Task<Response<LoginResponse>> VerifyRecoveryAndCompleteLogin(int userId, MfaRecoveryRequest request);
    Task<Response<bool>> DisableTotp(int userId);
    Task<Response<bool>> AdminResetMfa(Guid userUuid);
    Task<Response<bool>> AdminSetRequired(Guid userUuid, bool required);
}
