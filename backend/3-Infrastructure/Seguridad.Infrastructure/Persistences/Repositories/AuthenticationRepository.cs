using System.Data;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public class AuthenticationRepository(SeguridadDbContext _context) : IAuthenticationRepository
{
    public async Task<LoginResponse> Login(LoginRequest login)
    {
        var usuario = new LoginResponse();
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            var dtDatos = new DataTable();
            const string query = @"
                SELECT u.id    as user_id,
                       u.user_name,
                       u.change_password,
                       u.is_active,
                       u.email,
                       u.full_name,
                       u.uuid,
                       u.password,
                       COALESCE(m.is_enabled,  false) AS mfa_enabled,
                       COALESCE(m.is_required, false) AS mfa_required
                FROM sec.users u
                LEFT JOIN sec.user_mfa m ON m.user_id = u.id AND m.mfa_type = 'totp'
                WHERE u.email = @email AND u.is_active";

            var reader = await db.ExecuteReaderAsync(query, new { email = login.Email });
            dtDatos.Load(reader);

            if (dtDatos.Rows.Count == 0)
            {
                usuario.SesionId = await GetSessionId(login, 0, false);
                throw new CustomException("Correo electrónico o contraseña incorrectos.");
            }

            DataRow fila = dtDatos.Rows[0];

            if (!Common.Utilities.Cryptography.Hash.VerifyPassword(fila["password"].ToString() ?? "", login.Password))
            {
                usuario.SesionId = await GetSessionId(login, 0, false);
                throw new CustomException("Correo electrónico o contraseña incorrectos.");
            }

            usuario.Email = fila["email"].ToString() ?? "";
            usuario.UserName = fila["user_name"].ToString() ?? "";
            usuario.Uuid = fila["uuid"].ToString() ?? Guid.Empty.ToString();
            usuario.ChangePassword = Convert.ToBoolean(fila["change_password"].ToString());
            usuario.FullName = fila["full_name"].ToString() ?? "";
            int userId = fila["user_id"].ToString() != "" ? Convert.ToInt32(fila["user_id"].ToString()) : 0;
            usuario.UserId = userId;

            bool mfaEnabled = Convert.ToBoolean(fila["mfa_enabled"].ToString());
            bool mfaRequired = Convert.ToBoolean(fila["mfa_required"].ToString());

            if (mfaEnabled)
            {
                // TOTP configured and active: return session token only (full JWT issued after verification)
                usuario.RequireTotp = true;
                return usuario;
            }

            if (mfaRequired)
            {
                // Admin requires TOTP but user hasn't configured it yet: issue real JWT so setup can proceed
                usuario.TotpSetupRequired = true;
            }

            using var transaction = db.BeginTransaction();
            try
            {
                await db.ExecuteAsync("UPDATE sec.users SET last_access = now() WHERE id = @user_id",
                    new { user_id = userId });
                usuario.SesionId = await GetSessionId(login, userId, true, db, transaction);
                transaction.Commit();
            }
            catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<LoginResponse>(ex); }
        finally { db.Close(); }

        return usuario;
    }

    public async Task<LoginResponse> CompleteLoginWithTotp(int userId, TotpVerifyRequest request)
    {
        var usuario = new LoginResponse();
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            var dt = new DataTable();
            const string query = @"
                SELECT u.id as user_id, u.user_name, u.change_password, u.email, u.full_name, u.uuid
                FROM sec.users u
                JOIN sec.user_mfa m ON m.user_id = u.id AND m.mfa_type = 'totp'
                WHERE u.id = @user_id AND u.is_active AND m.is_enabled = true";

            var reader = await db.ExecuteReaderAsync(query, new { user_id = userId });
            dt.Load(reader);

            if (dt.Rows.Count == 0)
                throw new CustomException("Usuario no encontrado o TOTP no habilitado.");

            var fila = dt.Rows[0];
            usuario.UserId = userId;
            usuario.Email = fila["email"].ToString() ?? "";
            usuario.UserName = fila["user_name"].ToString() ?? "";
            usuario.Uuid = fila["uuid"].ToString() ?? Guid.Empty.ToString();
            usuario.ChangePassword = Convert.ToBoolean(fila["change_password"].ToString());
            usuario.FullName = fila["full_name"].ToString() ?? "";

            var loginRequest = new LoginRequest
            {
                Email = usuario.Email,
                Device = request.Device,
                LoginFrom = request.LoginFrom
            };

            using var transaction = db.BeginTransaction();
            try
            {
                await db.ExecuteAsync("UPDATE sec.users SET last_access = now() WHERE id = @user_id",
                    new { user_id = userId });
                usuario.SesionId = await GetSessionId(loginRequest, userId, true, db, transaction);
                transaction.Commit();
            }
            catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<LoginResponse>(ex); }
        finally { db.Close(); }

        return usuario;
    }

    protected static async Task<int> GetSessionId(LoginRequest login, int userId, bool loginSuccess, IDbConnection connection, IDbTransaction transactions)
    {
        int sessionId;
        try
        {
            sessionId = Convert.ToInt32(connection.ExecuteScalar("select set_sequences_key(@NombreTabla)",
                new { NombreTabla = "sec.users_login" }, transaction: transactions)!.ToString());

            const string queryInsert = @"
                INSERT INTO sec.users_login
                    (id, login_with, login_value, login_from, login_success, date, device, user_id)
                VALUES
                    (@session_id, @login_with, @login_value, @login_from, @login_success, now(), @device, @user_id)";

            await connection.ExecuteAsync(queryInsert, new
            {
                session_id = sessionId,
                login_with = "Email",
                login_value = login.Email,
                login_from = Enum.GetName(typeof(Domain.Enums.InicioSesionDesde), login.LoginFrom),
                device = login.Device ?? "",
                user_id = userId,
                login_success = loginSuccess
            }, transaction: transactions);
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<LoginResponse>(ex); }

        return sessionId;
    }

    protected async Task<int> GetSessionId(LoginRequest login, int userId, bool loginSuccess)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                var sessionId = await GetSessionId(login, userId, loginSuccess, db, transaction);
                transaction.Commit();
                return sessionId;
            }
            catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<LoginResponse>(ex); }
        finally { db.Close(); }
    }
}
