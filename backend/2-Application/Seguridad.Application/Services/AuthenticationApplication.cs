using System.Security.Cryptography;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Microsoft.Extensions.Options;
using Seguridad.Domain;
using Seguridad.Infrastructure;

namespace Seguridad.Application;

public class AuthenticationApplication(
    IAuthenticationRepository _authenticationRepository,
    IRefreshTokenRepository _refreshTokenRepository,
    IOptions<LoginSettings> _options) : IAuthenticationApplication
{
    public async Task<Response<LoginResponse>> Login(LoginRequest login)
    {
        var resp = new Response<LoginResponse>() { Data = new LoginResponse() };
        try
        {
            var settings = _options.Value;
            int failed = await _authenticationRepository.RecentFailedAttempts(
                login.Email, settings.LockoutMinutes);

            if (failed >= settings.MaxFailedAttempts)
                throw new CustomException(
                    $"Demasiados intentos fallidos. Vuelve a intentarlo en {settings.LockoutMinutes} minutos.");

            resp.Data = await _authenticationRepository.Login(login);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }

    public async Task<string> IssueRefreshToken(int userId, string device, string loginFrom, int days)
    {
        string raw = GenerateToken();
        await _refreshTokenRepository.Create(
            userId, HashToken(raw), device, loginFrom, DateTime.UtcNow.AddDays(days));
        return raw;
    }

    public async Task<Response<LoginResponse>> Refresh(RefreshTokenRequest request, int days)
    {
        var resp = new Response<LoginResponse>() { Data = new LoginResponse() };
        try
        {
            var stored = await _refreshTokenRepository.GetByHash(HashToken(request.RefreshToken));

            if (stored == null)
                throw new CustomException("Sesión inválida. Inicia sesión nuevamente.");

            // Un token ya rotado que vuelve a aparecer significa que alguien
            // conserva una copia: se cortan todas las sesiones del usuario.
            if (stored.IsRevoked)
            {
                await _refreshTokenRepository.RevokeAllForUser(stored.UserId);
                throw new CustomException("Sesión inválida. Inicia sesión nuevamente.");
            }

            if (stored.IsExpired)
                throw new CustomException("La sesión expiró. Inicia sesión nuevamente.");

            string loginFrom = Enum.GetName(typeof(Seguridad.Domain.Enums.InicioSesionDesde), request.LoginFrom) ?? "";
            var data = await _refreshTokenRepository.GetLoginDataForRefresh(
                stored.UserId, request.Device, loginFrom);

            // Usuario desactivado: el refresh deja de servir de inmediato.
            if (data == null)
            {
                await _refreshTokenRepository.RevokeAllForUser(stored.UserId);
                throw new CustomException("La cuenta no está disponible. Contacta al administrador.");
            }

            // Rotación: el token usado queda revocado y apuntando al nuevo.
            string raw = GenerateToken();
            long newId = await _refreshTokenRepository.Create(
                stored.UserId, HashToken(raw), request.Device, loginFrom, DateTime.UtcNow.AddDays(days));
            await _refreshTokenRepository.Revoke(stored.Id, newId);

            data.RefreshToken = raw;
            resp.Data = data;
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }

    public async Task<Response<bool>> RevokeRefreshToken(string refreshToken)
    {
        var resp = new Response<bool>();
        try
        {
            var stored = await _refreshTokenRepository.GetByHash(HashToken(refreshToken));
            if (stored != null && !stored.IsRevoked)
                await _refreshTokenRepository.Revoke(stored.Id, null);

            // Se responde igual exista o no: no se filtra si el token era válido.
            resp.Data = true;
            resp.ok = true;
        }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }

    /// <summary>256 bits de entropía en hexadecimal.</summary>
    private static string GenerateToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// SHA-512 directo: el token ya es aleatorio de 256 bits, así que no hace
    /// falta un hash lento con salt como en las contraseñas. Además debe ser
    /// determinista para poder buscarlo por hash.
    /// </summary>
    private static string HashToken(string token) =>
        Common.Utilities.Cryptography.Hash.SHA512Hash(token);
}
