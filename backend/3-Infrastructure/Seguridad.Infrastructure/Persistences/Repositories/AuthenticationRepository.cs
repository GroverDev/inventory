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
        // Única consulta del sistema que corre sin tenant: todavía no sabemos a
        // qué farmacia pertenece quien está intentando entrar.
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            var dtDatos = new DataTable();
            // No consulta sec.users directamente: esa tabla está bajo RLS y acá
            // todavía no se sabe el tenant. La función es la única excepción
            // declarada del sistema, y además deja app.tenant_id fijado en esta
            // conexión para todo lo que siga.
            const string query = "SELECT * FROM sec.fn_auth_lookup(@email, NULL)";

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
            usuario.TenantId = fila["tenant_id"].ToString() != "" ? Convert.ToInt32(fila["tenant_id"].ToString()) : 0;
            usuario.RolId = fila["rol_id"].ToString() != "" ? Convert.ToInt32(fila["rol_id"].ToString()) : 0;
            usuario.RolName = fila["rol_name"].ToString() ?? "";
            usuario.Roles = fila["roles"].ToString() ?? "";

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

    public async Task<int> RecentFailedAttempts(string email, int withinMinutes)
    {
        // Sin tenant: corre antes de autenticar, para decidir si exigir captcha.
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            // Solo cuentan los fallos posteriores al último acceso correcto:
            // así un login exitoso reinicia el contador y un atacante no puede
            // dejar bloqueada la cuenta de alguien que sí conoce su clave.
            const string query = @"
                SELECT COUNT(*)::int
                FROM sec.users_login
                WHERE login_value = @email
                  AND login_success = false
                  AND date > now() - (@minutes * interval '1 minute')
                  AND date > COALESCE((
                        SELECT MAX(date) FROM sec.users_login
                        WHERE login_value = @email AND login_success = true
                      ), to_timestamp(0))";

            return await db.ExecuteScalarAsync<int>(query,
                new { email, minutes = withinMinutes });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<LoginResponse>(ex); }
        finally { db.Close(); }
    }

    public async Task<LoginResponse> CompleteLoginWithTotp(int userId, TotpVerifyRequest request)
    {
        var usuario = new LoginResponse();
        // Sin tenant: la verificación del TOTP ocurre con el token intermedio de
        // 2FA, que todavía no lleva el claim de tenant.
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            var dt = new DataTable();
            // Igual que en Login: sec.users está bajo RLS y el token intermedio
            // de 2FA todavía no trae tenant. La función lo resuelve y lo fija.
            const string query = "SELECT * FROM sec.fn_auth_lookup(NULL, @user_id)";

            var reader = await db.ExecuteReaderAsync(query, new { user_id = userId });
            dt.Load(reader);

            if (dt.Rows.Count == 0)
                throw new CustomException("Usuario no encontrado o TOTP no habilitado.");

            // La función no filtra por TOTP habilitado: se valida acá.
            if (!Convert.ToBoolean(dt.Rows[0]["mfa_enabled"]))
                throw new CustomException("Usuario no encontrado o TOTP no habilitado.");

            var fila = dt.Rows[0];
            usuario.UserId = userId;
            usuario.TenantId = fila["tenant_id"].ToString() != "" ? Convert.ToInt32(fila["tenant_id"].ToString()) : 0;
            usuario.Email = fila["email"].ToString() ?? "";
            usuario.UserName = fila["user_name"].ToString() ?? "";
            usuario.Uuid = fila["uuid"].ToString() ?? Guid.Empty.ToString();
            usuario.ChangePassword = Convert.ToBoolean(fila["change_password"].ToString());
            usuario.FullName = fila["full_name"].ToString() ?? "";
            usuario.RolId = fila["rol_id"].ToString() != "" ? Convert.ToInt32(fila["rol_id"].ToString()) : 0;
            usuario.RolName = fila["rol_name"].ToString() ?? "";
            usuario.Roles = fila["roles"].ToString() ?? "";

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

    public async Task<int> RecordSuccessfulLogin(LoginRequest login, int userId)
    {
        // Sin tenant: corre en el mismo punto del flujo que Login/CompleteLoginWithTotp
        // cuando terminan con éxito, antes de que el JWT (que trae el tenant) exista.
        using var db = _context.CreateAuthConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                await db.ExecuteAsync("UPDATE sec.users SET last_access = now() WHERE id = @user_id",
                    new { user_id = userId });
                int sessionId = await GetSessionId(login, userId, true, db, transaction);
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
        // Sin tenant: registra el intento fallido, incluido el de correos que no
        // existen en ninguna farmacia.
        using var db = _context.CreateAuthConnection;
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
