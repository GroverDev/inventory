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
/// que ya vienen validados.
/// </remarks>
public class RefreshTokenRepository(SeguridadDbContext _context) : IRefreshTokenRepository
{
    public async Task<long> Create(int userId, string tokenHash, string device, string loginFrom, DateTime expiresAt)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                INSERT INTO sec.refresh_tokens
                    (user_id, token_hash, device, login_from, expires_at)
                VALUES
                    (@user_id, @token_hash, @device, @login_from, @expires_at)
                RETURNING id";

            return await db.ExecuteScalarAsync<long>(query, new
            {
                user_id = userId,
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
                SELECT id, user_id, token_hash, device, login_from,
                       created_at, expires_at, revoked_at, replaced_by
                FROM sec.refresh_tokens
                WHERE token_hash = @token_hash";

            return await db.QueryFirstOrDefaultAsync<RefreshToken>(query, new { token_hash = tokenHash });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<RefreshToken>(ex); }
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

    public async Task RevokeAllForUser(int userId)
    {
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            const string query = @"
                UPDATE sec.refresh_tokens
                   SET revoked_at = now()
                 WHERE user_id = @user_id AND revoked_at IS NULL";

            await db.ExecuteAsync(query, new { user_id = userId });
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
