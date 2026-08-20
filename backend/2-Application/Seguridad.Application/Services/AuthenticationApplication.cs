using System.Security.Cryptography;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Common.Utilities.Security;
using Microsoft.Extensions.Options;
using Seguridad.Domain;
using Seguridad.Infrastructure;

namespace Seguridad.Application;

public class AuthenticationApplication(
    IAuthenticationRepository _authenticationRepository,
    IRefreshTokenRepository _refreshTokenRepository,
    ITrustedDeviceRepository _trustedDeviceRepository,
    SessionRevocationRegistry _sessionRegistry,
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

    public async Task<int> RecentFailedAttempts(string email)
    {
        try
        {
            return await _authenticationRepository.RecentFailedAttempts(
                email, _options.Value.LockoutMinutes);
        }
        catch
        {
            // Si la consulta falla, la base está caída y ningún login va a
            // prosperar de todos modos: se devuelve 0 para que el error salga
            // por el camino normal de Login(), con su mensaje y su log, en vez
            // de convertirse en un 500 sin contexto.
            return 0;
        }
    }

    public async Task<string> IssueRefreshToken(int userId, int tenantId, int sessionId, string device, string loginFrom, int days)
    {
        string raw = GenerateToken();
        await _refreshTokenRepository.Create(
            userId, tenantId, sessionId, HashToken(raw), device, loginFrom, DateTime.UtcNow.AddDays(days));
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
            // conserva una copia: se cortan todas las sesiones del usuario, y se
            // tumba también cualquier access token que ya tuvieran en la mano.
            if (stored.IsRevoked)
            {
                var revokedIds = await _refreshTokenRepository.RevokeAllForUser(stored.UserId);
                _sessionRegistry.RevokeMany(revokedIds);
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
                var revokedIds = await _refreshTokenRepository.RevokeAllForUser(stored.UserId);
                _sessionRegistry.RevokeMany(revokedIds);
                throw new CustomException("La cuenta no está disponible. Contacta al administrador.");
            }

            // Rotación: el token usado queda revocado y apuntando al nuevo.
            string raw = GenerateToken();
            long newId = await _refreshTokenRepository.Create(
                stored.UserId, data.TenantId, data.SesionId, HashToken(raw), request.Device, loginFrom, DateTime.UtcNow.AddDays(days));
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
            {
                await _refreshTokenRepository.Revoke(stored.Id, null);
                _sessionRegistry.Revoke(stored.SessionId);
            }

            // Se responde igual exista o no: no se filtra si el token era válido.
            resp.Data = true;
            resp.ok = true;
        }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }

    public async Task<int> RecordSuccessfulLogin(LoginRequest login, int userId) =>
        await _authenticationRepository.RecordSuccessfulLogin(login, userId);

    public async Task<string> IssueTrustedDevice(int userId, int tenantId, string device, int days)
    {
        string raw = GenerateToken();
        await _trustedDeviceRepository.Create(
            userId, tenantId, HashToken(raw), device, DateTime.UtcNow.AddDays(days));
        return raw;
    }

    public async Task<bool> IsTrustedDevice(int userId, string rawToken)
    {
        if (string.IsNullOrEmpty(rawToken)) return false;

        var stored = await _trustedDeviceRepository.GetByHash(HashToken(rawToken));
        return stored != null && stored.UserId == userId && stored.IsActive;
    }

    public async Task RevokeAllTrustedDevicesForUser(int userId) =>
        await _trustedDeviceRepository.RevokeAllForUser(userId);

    public async Task<Response<List<TrustedDeviceResponse>>> GetTrustedDevices(int userId)
    {
        var resp = new Response<List<TrustedDeviceResponse>>() { Data = [] };
        try
        {
            resp.Data = await _trustedDeviceRepository.GetActiveForUser(userId);
            resp.ok = true;
        }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }
        return resp;
    }

    public async Task<Response<bool>> RevokeTrustedDevice(long id, int userId)
    {
        var resp = new Response<bool>();
        try
        {
            var device = await _trustedDeviceRepository.GetByIdForUser(id, userId);

            // No existe, es de otro usuario, o ya estaba revocado: el objetivo
            // (que no quede recordado con ese id) igual se cumple.
            if (device != null && !device.IsRevoked)
                await _trustedDeviceRepository.Revoke(id);

            resp.Data = resp.ok = true;
        }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }
        return resp;
    }

    public async Task<Response<List<SessionResponse>>> GetActiveSessions(int userId, int tenantId)
    {
        var resp = new Response<List<SessionResponse>>() { Data = [] };
        try
        {
            resp.Data = await _refreshTokenRepository.GetActiveForUser(userId, tenantId);
            resp.ok = true;
        }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }
        return resp;
    }

    public async Task<Response<List<ConnectedUserResponse>>> GetConnectedUsers(int tenantId)
    {
        var resp = new Response<List<ConnectedUserResponse>>() { Data = [] };
        try
        {
            resp.Data = await _refreshTokenRepository.GetActiveForTenant(tenantId);
            resp.ok = true;
        }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }
        return resp;
    }

    public async Task<Response<bool>> CloseSession(long id, int tenantId)
    {
        var resp = new Response<bool>();
        try
        {
            var session = await _refreshTokenRepository.GetByIdForTenant(id, tenantId);

            // No existe, es de otro tenant, o ya estaba cerrada: el objetivo
            // (que no quede una sesión abierta con ese id) igual se cumple.
            if (session == null || session.IsRevoked)
            {
                resp.Data = resp.ok = true;
                return resp;
            }

            await _refreshTokenRepository.Revoke(id, null);
            _sessionRegistry.Revoke(session.SessionId);

            resp.Data = resp.ok = true;
        }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }
        return resp;
    }

    public async Task<Response<bool>> CloseAllSessions(int userId, int tenantId)
    {
        var resp = new Response<bool>();
        try
        {
            var revokedSessionIds = await _refreshTokenRepository.RevokeAllForUserInTenant(userId, tenantId);
            _sessionRegistry.RevokeMany(revokedSessionIds);

            resp.Data = resp.ok = true;
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
