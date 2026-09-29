using System.Security.Cryptography;
using Common.Utilities;
using Common.Utilities.Comun.Bases;
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

    public async Task<string> IssueRefreshToken(int userId, int tenantId, Guid branchId, int sessionId, string device, string loginFrom, double days, long? trustedDeviceId = null)
    {
        string raw = GenerateToken();
        await _refreshTokenRepository.Create(
            userId, tenantId, branchId, sessionId, HashToken(raw), device, loginFrom, DateTime.UtcNow.AddDays(days), trustedDeviceId);
        return raw;
    }

    public async Task<Response<LoginResponse>> Refresh(RefreshTokenRequest request, int days, double webSessionHours)
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
                stored.UserId, request.Device, loginFrom, stored.BranchId);

            // Usuario desactivado: el refresh deja de servir de inmediato.
            if (data == null)
            {
                var revokedIds = await _refreshTokenRepository.RevokeAllForUser(stored.UserId);
                _sessionRegistry.RevokeMany(revokedIds);
                throw new CustomException("La cuenta no está disponible. Contacta al administrador.");
            }

            // En web la vida del token depende de si el usuario pidió mantener
            // la sesión. No hay columna para esa elección: se deduce de la
            // vida con que se emitió el token que se canjea (12 h frente a 30
            // días), y la rotación la conserva. El móvil no pasa por acá: su
            // token siempre dura lo mismo.
            double lifetimeDays = days;
            if (request.LoginFrom is Seguridad.Domain.Enums.InicioSesionDesde.Web
                or Seguridad.Domain.Enums.InicioSesionDesde.ReconexionWeb)
            {
                double sessionDays = webSessionHours / 24.0;
                double issuedDays = (stored.ExpiresAt - stored.CreatedAt).TotalDays;
                data.RememberSession = issuedDays > (sessionDays + days) / 2.0;
                lifetimeDays = data.RememberSession ? days : sessionDays;
            }

            // Rotación: el token usado queda revocado y apuntando al nuevo.
            string raw = GenerateToken();
            long newId = await _refreshTokenRepository.Create(
                stored.UserId, data.TenantId, data.BranchId, data.SesionId, HashToken(raw), request.Device, loginFrom, DateTime.UtcNow.AddDays(lifetimeDays), stored.TrustedDeviceId);
            await _refreshTokenRepository.Revoke(stored.Id, newId);

            data.RefreshToken = raw;
            resp.Data = data;
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return resp;
    }

    public async Task<(Response<LoginResponse> Resp, bool Renewable)> SwitchBranch(DataToken session, Guid branchId)
    {
        var resp = new Response<LoginResponse>() { Data = new LoginResponse() };
        bool renewable = false;
        try
        {
            // El JWT nuevo repite los datos del actual; solo cambia la sucursal.
            var data = new LoginResponse
            {
                UserId = session.UserId,
                TenantId = session.TenantId,
                Uuid = session.Uuid,
                Email = session.Email,
                SesionId = session.SessionId,
                RolName = session.Rol,
                Roles = session.Roles
            };

            // Misma regla que el login: solo sucursales activas y habilitadas.
            // Si la pedida no lo está, AssignBranch cae a la default, y eso acá
            // es un rechazo, no un cambio silencioso a otra sucursal.
            await _authenticationRepository.AssignBranch(data, branchId);
            if (data.BranchId != branchId)
                throw new CustomException("No está habilitado en esa sucursal, o la sucursal está inactiva.");

            renewable = await _refreshTokenRepository.SetBranchForSession(
                session.UserId, session.SessionId, branchId) > 0;

            resp.Data = data;
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }

        return (resp, renewable);
    }

    public async Task<Response<bool>> RevokeRefreshToken(string refreshToken, string deviceTrustToken = "", bool keepDevice = false)
    {
        var resp = new Response<bool>();
        try
        {
            var stored = string.IsNullOrEmpty(refreshToken)
                ? null
                : await _refreshTokenRepository.GetByHash(HashToken(refreshToken));
            if (stored != null && !stored.IsRevoked)
            {
                await _refreshTokenRepository.Revoke(stored.Id, null);
                _sessionRegistry.Revoke(stored.SessionId);
            }

            // Cerrar sesión en web también olvida el equipo. Se revoca el
            // dispositivo enlazado a la sesión y el de la cookie: pueden ser el
            // mismo, o solo existir uno (sesión anterior al enlace, o sesión ya
            // vencida con la cookie aún en el navegador). Revoke es idempotente.
            // El enlace de la sesión solo se usa si también se pidió olvidar el
            // equipo: KeepDevice llega aquí como deviceTrustToken vacío y se
            // distingue con keepDevice.
            if (!keepDevice && stored?.TrustedDeviceId is long linkedDeviceId)
                await _trustedDeviceRepository.Revoke(linkedDeviceId);

            if (!string.IsNullOrEmpty(deviceTrustToken))
            {
                var device = await _trustedDeviceRepository.GetByHash(HashToken(deviceTrustToken));
                if (device != null && !device.IsRevoked)
                    await _trustedDeviceRepository.Revoke(device.Id);
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

    public async Task<Response<bool>> AssignBranch(LoginResponse data)
    {
        var resp = new Response<bool>();
        try
        {
            await _authenticationRepository.AssignBranch(data, null);
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Soporte Técnico.", ex); }
        return resp;
    }

    public async Task<(string Raw, long Id)> IssueTrustedDevice(int userId, int tenantId, string device, int days)
    {
        string raw = GenerateToken();
        long id = await _trustedDeviceRepository.Create(
            userId, tenantId, HashToken(raw), device, DateTime.UtcNow.AddDays(days));
        return (raw, id);
    }

    public async Task<long?> FindTrustedDevice(int userId, string rawToken)
    {
        if (string.IsNullOrEmpty(rawToken)) return null;

        var stored = await _trustedDeviceRepository.GetByHash(HashToken(rawToken));
        return stored != null && stored.UserId == userId && stored.IsActive ? stored.Id : null;
    }

    public async Task RevokeAllTrustedDevicesForUser(int userId)
    {
        await _trustedDeviceRepository.RevokeAllForUser(userId);

        // Sin esto la sesión ya abierta seguiría renovándose sola con su cookie.
        var sessionIds = await _refreshTokenRepository.RevokeAllTrustedDeviceSessions(userId);
        _sessionRegistry.RevokeMany(sessionIds);
    }

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

            // También las sesiones web abiertas con ese dispositivo: olvidarlo
            // no debe dejar al equipo entrando sin contraseña ni TOTP por la
            // cookie de refresh que ya tenía. Solo si el dispositivo es del
            // usuario (device != null): el id solo no basta.
            if (device != null)
            {
                var sessionIds = await _refreshTokenRepository.RevokeByTrustedDevice(id, userId);
                _sessionRegistry.RevokeMany(sessionIds);
            }

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
