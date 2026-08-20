using Common.Utilities;
using Dapper;
using Seguridad.Domain;

namespace Seguridad.Infrastructure;

/// <remarks>
/// Igual que RefreshTokenRepository: usa CreateAuthConnection porque corre en
/// pleno ciclo de autenticación, antes de resolver tenant. sec.trusted_devices
/// queda fuera de RLS por eso mismo y se accede siempre por user_id o por hash.
/// </remarks>
public class TrustedDeviceRepository(SeguridadDbContext _context) : ITrustedDeviceRepository
{
    public async Task<long> Create(int userId, int tenantId, string tokenHash, string deviceLabel, DateTime expiresAt)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                INSERT INTO sec.trusted_devices
                    (user_id, tenant_id, token_hash, device_label, expires_at)
                VALUES
                    (@user_id, @tenant_id, @token_hash, @device_label, @expires_at)
                RETURNING id";

            return await db.ExecuteScalarAsync<long>(query, new
            {
                user_id = userId,
                tenant_id = tenantId,
                token_hash = tokenHash,
                device_label = deviceLabel ?? "",
                expires_at = expiresAt
            });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<TrustedDevice>(ex); }
        finally { db.Close(); }
    }

    public async Task<TrustedDevice?> GetByHash(string tokenHash)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                SELECT id, user_id, tenant_id, token_hash, device_label, created_at, expires_at, revoked_at
                FROM sec.trusted_devices
                WHERE token_hash = @token_hash";

            return await db.QueryFirstOrDefaultAsync<TrustedDevice>(query, new { token_hash = tokenHash });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<TrustedDevice>(ex); }
        finally { db.Close(); }
    }

    public async Task RevokeAllForUser(int userId)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                UPDATE sec.trusted_devices
                   SET revoked_at = now()
                 WHERE user_id = @user_id AND revoked_at IS NULL";

            await db.ExecuteAsync(query, new { user_id = userId });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<TrustedDevice>(ex); }
        finally { db.Close(); }
    }

    /// <summary>Dispositivos activos (no revocados, no vencidos) del propio usuario, para su autogestión.</summary>
    public async Task<List<TrustedDeviceResponse>> GetActiveForUser(int userId)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                SELECT id, device_label, created_at, expires_at
                FROM sec.trusted_devices
                WHERE user_id = @user_id AND revoked_at IS NULL AND expires_at > now()
                ORDER BY created_at DESC";

            var rows = await db.QueryAsync<TrustedDeviceResponse>(query, new { user_id = userId });
            return rows.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<TrustedDeviceResponse>>(ex); }
        finally { db.Close(); }
    }

    /// <summary>Fila puntual, solo si pertenece al usuario: evita que alguien olvide el dispositivo de otro adivinando el id.</summary>
    public async Task<TrustedDevice?> GetByIdForUser(long id, int userId)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                SELECT id, user_id, tenant_id, token_hash, device_label, created_at, expires_at, revoked_at
                FROM sec.trusted_devices
                WHERE id = @id AND user_id = @user_id";

            return await db.QueryFirstOrDefaultAsync<TrustedDevice>(query, new { id, user_id = userId });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<TrustedDevice>(ex); }
        finally { db.Close(); }
    }

    public async Task Revoke(long id)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                UPDATE sec.trusted_devices
                   SET revoked_at = now()
                 WHERE id = @id AND revoked_at IS NULL";

            await db.ExecuteAsync(query, new { id });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<TrustedDevice>(ex); }
        finally { db.Close(); }
    }
}
