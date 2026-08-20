using System.Data;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Seguridad.Domain;

namespace Seguridad.Infrastructure;

/// <remarks>
/// Todo este repositorio usa <c>CreateAuthConnection</c>. Sus operaciones son
/// parte del ciclo de autenticación —emitir el refresh al iniciar sesión,
/// canjearlo, revocarlo— y ocurren en endpoints anónimos, donde todavía no hay
/// claim de tenant. La tabla <c>sec.refresh_tokens</c> queda fuera de RLS por eso
/// mismo, y se accede siempre por <c>user_id</c> o por hash del token, dos valores
/// que ya vienen validados. Las lecturas administrativas (GetActiveForUser,
/// GetActiveForTenant) corren desde endpoints autenticados, así que ahí sí
/// filtran <c>tenant_id</c> explícito: la tabla no tiene RLS, nadie lo hace por
/// ellas.
/// </remarks>
public class RefreshTokenRepository(SeguridadDbContext _context) : IRefreshTokenRepository
{
    public async Task<long> Create(
        int userId, int tenantId, int sessionId, string tokenHash, string device, string loginFrom, DateTime expiresAt)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                INSERT INTO sec.refresh_tokens
                    (user_id, tenant_id, session_id, token_hash, device, login_from, expires_at)
                VALUES
                    (@user_id, @tenant_id, @session_id, @token_hash, @device, @login_from, @expires_at)
                RETURNING id";

            return await db.ExecuteScalarAsync<long>(query, new
            {
                user_id = userId,
                tenant_id = tenantId,
                session_id = sessionId,
                token_hash = tokenHash,
                device = device ?? "",
                login_from = loginFrom,
                expires_at = expiresAt
            });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<RefreshToken>(ex); }
        finally { db.Close(); }
    }

    public async Task<RefreshToken?> GetByHash(string tokenHash)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                SELECT id, user_id, tenant_id, session_id, token_hash, device, login_from,
                       created_at, expires_at, revoked_at, replaced_by
                FROM sec.refresh_tokens
                WHERE token_hash = @token_hash";

            return await db.QueryFirstOrDefaultAsync<RefreshToken>(query, new { token_hash = tokenHash });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<RefreshToken>(ex); }
        finally { db.Close(); }
    }

    /// <summary>
    /// Fila puntual, solo si pertenece al tenant indicado. Lo usa el cierre de
    /// una sesión desde el panel de administración: sin este chequeo, un admin
    /// podría cerrar por id la sesión de un usuario de otro tenant.
    /// </summary>
    public async Task<RefreshToken?> GetByIdForTenant(long id, int tenantId)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                SELECT id, user_id, tenant_id, session_id, token_hash, device, login_from,
                       created_at, expires_at, revoked_at, replaced_by
                FROM sec.refresh_tokens
                WHERE id = @id AND tenant_id = @tenant_id";

            return await db.QueryFirstOrDefaultAsync<RefreshToken>(query, new { id, tenant_id = tenantId });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<RefreshToken>(ex); }
        finally { db.Close(); }
    }

    public async Task<List<SessionResponse>> GetActiveForUser(int userId, int tenantId)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                SELECT id, device, login_from, created_at, expires_at
                FROM sec.refresh_tokens
                WHERE user_id = @user_id AND tenant_id = @tenant_id
                  AND revoked_at IS NULL AND expires_at > now()
                ORDER BY created_at DESC";

            var rows = await db.QueryAsync<SessionResponse>(query, new { user_id = userId, tenant_id = tenantId });
            return rows.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<SessionResponse>>(ex); }
        finally { db.Close(); }
    }

    public async Task<List<ConnectedUserResponse>> GetActiveForTenant(int tenantId)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                SELECT rt.id, rt.device, rt.login_from, rt.created_at, rt.expires_at,
                       u.uuid::text AS uuid, u.full_name, u.email
                FROM sec.refresh_tokens rt
                JOIN sec.users u ON u.id = rt.user_id
                WHERE rt.tenant_id = @tenant_id
                  AND rt.revoked_at IS NULL AND rt.expires_at > now()
                ORDER BY rt.created_at DESC";

            var rows = await db.QueryAsync<ConnectedUserResponse>(query, new { tenant_id = tenantId });
            return rows.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<ConnectedUserResponse>>(ex); }
        finally { db.Close(); }
    }

    public async Task Revoke(long id, long? replacedBy)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                UPDATE sec.refresh_tokens
                   SET revoked_at = now(), replaced_by = @replaced_by
                 WHERE id = @id AND revoked_at IS NULL";

            await db.ExecuteAsync(query, new { id, replaced_by = replacedBy });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<RefreshToken>(ex); }
        finally { db.Close(); }
    }

    /// <summary>Revoca todos los tokens activos del usuario y devuelve los SessionId que quedaron sin vigencia.</summary>
    public async Task<List<int>> RevokeAllForUser(int userId)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                UPDATE sec.refresh_tokens
                   SET revoked_at = now()
                 WHERE user_id = @user_id AND revoked_at IS NULL
                RETURNING session_id";

            var ids = await db.QueryAsync<int?>(query, new { user_id = userId });
            return ids.Where(id => id.HasValue).Select(id => id!.Value).ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<RefreshToken>(ex); }
        finally { db.Close(); }
    }

    /// <summary>
    /// Igual que <see cref="RevokeAllForUser"/>, pero acotado al tenant del
    /// admin que la pide: cerrar "todas las sesiones" de un usuario desde el
    /// panel no debe poder alcanzar sesiones de otro tenant.
    /// </summary>
    public async Task<List<int>> RevokeAllForUserInTenant(int userId, int tenantId)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                UPDATE sec.refresh_tokens
                   SET revoked_at = now()
                 WHERE user_id = @user_id AND tenant_id = @tenant_id AND revoked_at IS NULL
                RETURNING session_id";

            var ids = await db.QueryAsync<int?>(query, new { user_id = userId, tenant_id = tenantId });
            return ids.Where(id => id.HasValue).Select(id => id!.Value).ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<RefreshToken>(ex); }
        finally { db.Close(); }
    }

    public async Task<LoginResponse?> GetLoginDataForRefresh(int userId, string device, string loginFrom)
    {
        // Sin tenant: el refresh llega con el access token vencido o ausente, así
        // que no hay claim del cual sacarlo. El tenant sale de esta misma consulta
        // y viaja en el JWT nuevo.
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            var dt = new DataTable();
            // sec.users está bajo RLS y el refresh llega sin claim de tenant.
            const string query = "SELECT * FROM sec.fn_auth_lookup(NULL, @user_id)";

            var reader = await db.ExecuteReaderAsync(query, new { user_id = userId });
            dt.Load(reader);

            // Usuario dado de baja o eliminado: el refresh deja de servir.
            if (dt.Rows.Count == 0) return null;

            var fila = dt.Rows[0];
            var usuario = new LoginResponse
            {
                UserId = userId,
                TenantId = fila["tenant_id"].ToString() != "" ? Convert.ToInt32(fila["tenant_id"].ToString()) : 0,
                Email = fila["email"].ToString() ?? "",
                UserName = fila["user_name"].ToString() ?? "",
                Uuid = fila["uuid"].ToString() ?? Guid.Empty.ToString(),
                ChangePassword = Convert.ToBoolean(fila["change_password"].ToString()),
                FullName = fila["full_name"].ToString() ?? "",
                RolId = fila["rol_id"].ToString() != "" ? Convert.ToInt32(fila["rol_id"].ToString()) : 0,
                RolName = fila["rol_name"].ToString() ?? "",
                Roles = fila["roles"].ToString() ?? ""
            };

            // Una reconexión abre sesión nueva: el JWT emitido lleva su SessionId
            // y queda registrada en la auditoría igual que un login normal.
            using var transaction = db.BeginTransaction();
            try
            {
                const string queryInsert = @"
                    INSERT INTO sec.users_login
                        (id, login_with, login_value, login_from, login_success, date, device, user_id)
                    VALUES
                        (@session_id, 'Email', @login_value, @login_from, true, now(), @device, @user_id)";

                int sessionId = Convert.ToInt32(db.ExecuteScalar(
                    "select set_sequences_key(@NombreTabla)",
                    new { NombreTabla = "sec.users_login" }, transaction: transaction)!.ToString());

                await db.ExecuteAsync(queryInsert, new
                {
                    session_id = sessionId,
                    login_value = usuario.Email,
                    login_from = loginFrom,
                    device = device ?? "",
                    user_id = userId
                }, transaction: transaction);

                await db.ExecuteAsync("UPDATE sec.users SET last_access = now() WHERE id = @user_id",
                    new { user_id = userId }, transaction: transaction);

                transaction.Commit();
                usuario.SesionId = sessionId;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }

            return usuario;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<LoginResponse>(ex); }
        finally { db.Close(); }
    }
}
