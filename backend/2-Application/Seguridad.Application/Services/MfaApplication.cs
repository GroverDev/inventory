using Common.Utilities;
using Common.Utilities.CustomCryptography;
using Common.Utilities.Exceptions;
using Microsoft.Extensions.Options;
using Seguridad.Domain;
using Seguridad.Infrastructure;

namespace Seguridad.Application;

public class MfaApplication(
    IMfaRepository _mfaRepository,
    IAuthenticationRepository _authRepository,
    IOptions<MfaSettings> _options) : IMfaApplication
{
    public async Task<Response<TotpSetupResponse>> SetupTotp(int userId, string userEmail)
    {
        var resp = new Response<TotpSetupResponse>() { Data = new TotpSetupResponse() };
        try
        {
            var mfa = await _mfaRepository.GetTotpMfa(userId);
            if (mfa?.IsEnabled == true)
                throw new CustomException("TOTP ya está habilitado. Deshabilítelo primero para reconfigurarlo.");

            var settings = _options.Value;
            string secret = TotpHelper.GenerateSecret();
            string encryptedSecret = EncryptionHelper.Encrypt(secret, EncryptionHelper.KeyFromHex(settings.EncryptionKeyHex));

            await _mfaRepository.UpsertTotpSecret(userId, encryptedSecret);

            resp.Data = new TotpSetupResponse
            {
                SecretKey = secret,
                QrCodeUri = TotpHelper.GetQrCodeUri(secret, userEmail, settings.Issuer)
            };
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }

    public async Task<Response<MfaEnableResponse>> EnableTotp(int userId, string code)
    {
        var resp = new Response<MfaEnableResponse>() { Data = new MfaEnableResponse() };
        try
        {
            var mfa = await _mfaRepository.GetTotpMfa(userId);
            if (mfa == null || string.IsNullOrEmpty(mfa.SecretEncrypted))
                throw new CustomException("No existe una configuración TOTP pendiente. Inicie el proceso de configuración primero.");
            if (mfa.IsEnabled)
                throw new CustomException("TOTP ya está habilitado para este usuario.");

            var settings = _options.Value;
            string secret = EncryptionHelper.Decrypt(mfa.SecretEncrypted, EncryptionHelper.KeyFromHex(settings.EncryptionKeyHex));

            if (!TotpHelper.VerifyCode(secret, code))
                throw new CustomException("Código TOTP incorrecto. Verifique que la hora del dispositivo sea correcta.");

            var recoveryCodes = await _mfaRepository.ActivateTotp(userId);
            resp.Data = new MfaEnableResponse { RecoveryCodes = recoveryCodes };
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }

    public async Task<Response<LoginResponse>> VerifyTotpAndCompleteLogin(int userId, TotpVerifyRequest request)
    {
        var resp = new Response<LoginResponse>() { Data = new LoginResponse() };
        try
        {
            var settings = _options.Value;
            var mfa = await _mfaRepository.GetTotpMfa(userId);
            if (mfa == null || !mfa.IsEnabled || string.IsNullOrEmpty(mfa.SecretEncrypted))
                throw new CustomException("TOTP no está habilitado para este usuario.");

            // Lockout check with auto-unlock after expiry
            if (mfa.LockedUntil.HasValue)
            {
                if (mfa.LockedUntil.Value > DateTime.UtcNow)
                {
                    var remaining = Math.Ceiling((mfa.LockedUntil.Value - DateTime.UtcNow).TotalMinutes);
                    throw new CustomException($"Cuenta bloqueada por demasiados intentos fallidos. Intente en {remaining} minuto(s) o use un código de recuperación.");
                }
                await _mfaRepository.ResetAttempts(mfa.Id);
            }

            string secret = EncryptionHelper.Decrypt(mfa.SecretEncrypted, EncryptionHelper.KeyFromHex(settings.EncryptionKeyHex));

            if (!TotpHelper.VerifyCode(secret, request.TotpCode))
            {
                await _mfaRepository.RecordFailure(mfa.Id, settings.MaxFailedAttempts, settings.LockoutMinutes);
                throw new CustomException("Código TOTP incorrecto o expirado.");
            }

            await _mfaRepository.ResetAttempts(mfa.Id);
            resp.Data = await _authRepository.CompleteLoginWithTotp(userId, request);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }

    public async Task<Response<LoginResponse>> VerifyRecoveryAndCompleteLogin(int userId, MfaRecoveryRequest request)
    {
        var resp = new Response<LoginResponse>() { Data = new LoginResponse() };
        try
        {
            var normalized = request.RecoveryCode.Replace("-", "").Replace(" ", "").ToUpperInvariant();
            bool valid = await _mfaRepository.UseRecoveryCode(userId, normalized);

            if (!valid)
                throw new CustomException("Código de recuperación inválido o ya utilizado.");

            var loginRequest = new TotpVerifyRequest { Device = "", LoginFrom = Seguridad.Domain.Enums.InicioSesionDesde.Web };
            resp.Data = await _authRepository.CompleteLoginWithTotp(userId, loginRequest);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }

    public async Task<Response<bool>> DisableTotp(int userId)
    {
        var resp = new Response<bool>();
        try
        {
            await _mfaRepository.DisableTotp(userId);
            resp.Data = true;
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }

    public async Task<Response<bool>> AdminResetMfa(Guid userUuid)
    {
        var resp = new Response<bool>();
        try
        {
            var mfa = await _mfaRepository.GetTotpMfaByUuid(userUuid);
            if (mfa == null)
                throw new CustomException("El usuario no tiene TOTP configurado.");

            await _mfaRepository.AdminResetMfa(mfa.UserId);
            resp.Data = true;
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }

    public async Task<Response<bool>> AdminSetRequired(Guid userUuid, bool required)
    {
        var resp = new Response<bool>();
        try
        {
            // Try to get userId from MFA row first; fall back to users table if no MFA row yet
            var mfa = await _mfaRepository.GetTotpMfaByUuid(userUuid);
            int? userId = mfa?.UserId ?? await _mfaRepository.GetUserIdByUuid(userUuid);

            if (userId == null)
                throw new CustomException("Usuario no encontrado o inactivo.");

            await _mfaRepository.AdminSetRequired(userId.Value, required);
            resp.Data = true;
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }
}
