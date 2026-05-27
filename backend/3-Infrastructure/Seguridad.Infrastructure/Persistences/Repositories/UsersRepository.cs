using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Seguridad.Domain;
using Seguridad.Domain.Requests;

namespace Seguridad.Infrastructure;

public class UsersRepository(SeguridadDbContext _context) : IUsersRepository
{
   public async Task<List<UsersResponse>> GetUsers(UserSearchRequest userSearchRequest)
{
    var objResp = new List<UsersResponse>();
    using var db = _context.CreateConnection;
    try
    {

        string subquery = userSearchRequest.Email.Trim().Length > 0
                          ? " u.email ILIKE @Email "
                          : " full_name ILIKE @full_name ";

        userSearchRequest.Email = "%" + userSearchRequest.Email + "%";
        userSearchRequest.FullName = "%" + userSearchRequest.FullName + "%";
        db.Open();
        string query = @"SELECT u.uuid::Text as uuid, u.user_name, u.email, u.full_name, u.last_access,
                                        u.change_password, u.is_active,
                                        COALESCE(m.is_enabled,  false) AS mfa_enabled,
                                        COALESCE(m.is_required, false) AS mfa_required
                                 FROM sec.users u
                                 LEFT JOIN sec.user_mfa m ON m.user_id = u.id AND m.mfa_type = 'totp'
                                WHERE u.is_active
                                  AND " + subquery;
        var listResp = await db.QueryAsync<UsersResponse>(query, new { userSearchRequest.Email, full_name = userSearchRequest.FullName });
        objResp = listResp.ToList();
    }
    catch (Exception ex) { throw ExceptionHandler.HandleException<List<Forms>>(ex); }
    finally { db.Close(); }
    return objResp;
}

public async Task<bool> CreateUser(Users user, int userId)
{
    var ok = false;
    using var db = _context.CreateConnection;
    try
    {
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            user.Password = Common.Utilities.Cryptography.Hash.HashPassword(user.Password);
            string sqlQuery = @"SELECT EXISTS (
                                    SELECT 1 FROM sec.users WHERE LOWER(user_name) = LOWER(@UserName) OR LOWER(email) = LOWER(@Email)
                                 )
                              ";
            var existeUsuario = db.ExecuteScalar<bool>(sqlQuery, new { user.UserName, user.Email });
            if (existeUsuario) throw new CustomException("Ya existe un usuario con ese nombre de usuario o ese correo electrónico");

            user.Id = await db.ExecuteScalarAsync<int>("select set_sequences_key(@TableName)", new { TableName = "sec.users" }, transaction);
            sqlQuery = @"INSERT INTO sec.users
                                    (id,  user_name, password,   email, full_name ,  last_access,  change_password, is_active,  created_by, created, modified_by, modified, uuid)
                             VALUES (@Id, @UserName,@Password, @Email, @FullName, @LastAccess, @ChangePassword, @IsActive, @CreatedBy, now(),  @ModifiedBy, now() ,@Uuid);";
            await db.ExecuteAsync(sqlQuery, user, transaction);
            transaction.Commit();
            ok = true;
        }
        catch (CustomException ex)
        {
            transaction.Rollback();
            throw new CustomException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception(ex.Message, ex);
        }
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
    catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
    finally { db.Close(); }
    return ok;
}
public async Task<bool> CreateUserOutPassword(Users user, int userId)
{
    var ok = false;
    using var db = _context.CreateConnection;
    try
    {
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            user.Password = Common.Utilities.Cryptography.Hash.HashPassword(user.Password);
            string sqlQuery = @"SELECT EXISTS (
                                    SELECT 1 FROM sec.users WHERE LOWER(user_name) = LOWER(@UserName) OR LOWER(email) = LOWER(@Email)
                                 )
                              ";
            var existeUsuario = db.ExecuteScalar<bool>(sqlQuery, new { user.UserName, user.Email });
            if (existeUsuario) throw new CustomException("Ya existe un usuario con ese nombre de usuario o ese correo electrónico");

            user.Id = await db.ExecuteScalarAsync<int>("select set_sequences_key(@TableName)", new { TableName = "sec.users" }, transaction);
            user.Password = "";
            sqlQuery = @"INSERT INTO sec.users
                                    (id,  user_name, password,   email, full_name ,  last_access,  change_password, is_active,  created_by, created, modified_by, modified, uuid)
                             VALUES (@Id, @UserName,@Password, @Email, @FullName, @LastAccess, @ChangePassword, @IsActive, @CreatedBy, now(),  @ModifiedBy, now() ,@Uuid);";
            await db.ExecuteAsync(sqlQuery, user, transaction);
            transaction.Commit();
            ok = true;
        }
        catch (CustomException ex)
        {
            transaction.Rollback();
            throw new CustomException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception(ex.Message, ex);
        }
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
    catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
    finally { db.Close(); }
    return ok;
}


public async Task<UsersResponse> GetUser(Guid uuid)
{
    UsersResponse? objResp = new UsersResponse();
    using var db = _context.CreateConnection;
    try
    {
        db.Open();
        string query = @"SELECT uuid::Text as uuid, user_name, email, full_name, last_access, change_password, is_active, id 
                            FROM sec.users u 
                            WHERE is_active 
                              AND uuid = @Uuid";
        objResp = await db.QueryFirstOrDefaultAsync<UsersResponse>(query, new { Uuid = uuid });
    }
    catch (Exception ex) { throw ExceptionHandler.HandleException<UsersResponse>(ex); }
    finally { db.Close(); }
    return objResp??new UsersResponse();
}

public async Task<bool> UpdateUser(Users user, int modifiedBy)
{
    var ok = false;
    using var db = _context.CreateConnection;
    try
    {
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            string checkQuery = @"SELECT EXISTS (
                                       SELECT 1 FROM sec.users
                                       WHERE (LOWER(user_name) = LOWER(@UserName) OR LOWER(email) = LOWER(@Email))
                                         AND uuid != @Uuid
                                         AND is_active
                                    )";

            var existeUsuario = db.ExecuteScalar<bool>(checkQuery, new { user.UserName, user.Email, user.Uuid }, transaction);
            if (existeUsuario) throw new CustomException("Ya existe un usuario con ese nombre de usuario o correo electrónico");

            string sqlQuery = @"UPDATE sec.users
                                   SET user_name = @UserName,
                                       email = @Email,
                                       full_name = @FullName,
                                       modified_by = @ModifiedBy,
                                       modified = now()
                                   WHERE uuid = @Uuid";

            var rows = await db.ExecuteAsync(sqlQuery, new { user.UserName, user.Email, user.FullName, ModifiedBy = modifiedBy, user.Uuid }, transaction);

            if (rows == 0) throw new CustomException("No se encontró el usuario para actualizar");

            transaction.Commit();
            ok = true;
        }
        catch (CustomException ex)
        {
            transaction.Rollback();
            throw new CustomException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception(ex.Message, ex);
        }
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
    catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
    finally { db.Close(); }
    return ok;
}

public async Task<bool> DeleteUser(Guid uuid, int modifiedBy)
{
    var ok = false;
    using var db = _context.CreateConnection;
    try
    {
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            string sqlQuery = @"UPDATE sec.users
                                   SET is_active = false,
                                       modified_by = @ModifiedBy,
                                       modified = now()
                                   WHERE uuid = @Uuid";

            var rows = await db.ExecuteAsync(sqlQuery, new { Uuid = uuid, ModifiedBy = modifiedBy }, transaction);

            if (rows == 0) throw new CustomException("No se encontró el usuario para eliminar");

            transaction.Commit();
            ok = true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception(ex.Message, ex);
        }
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
    catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
    finally { db.Close(); }
    return ok;
}

public async Task<bool> ChangeUserPassword(Guid uuid, string hashedPassword, int modifiedBy)
{
    using var db = _context.CreateConnection;
    try
    {
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            string sqlQuery = @"UPDATE sec.users
                                   SET password = @HashedPassword,
                                       change_password = false,
                                       modified_by = @ModifiedBy,
                                       modified = now()
                                 WHERE uuid = @Uuid AND is_active";

            var rows = await db.ExecuteAsync(sqlQuery, new { HashedPassword = hashedPassword, ModifiedBy = modifiedBy, Uuid = uuid }, transaction);
            if (rows == 0) throw new CustomException("No se encontró el usuario para actualizar la contraseña.");
            transaction.Commit();
            return true;
        }
        catch (CustomException ex) { transaction.Rollback(); throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { transaction.Rollback(); throw new Exception(ex.Message, ex); }
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
    catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
    finally { db.Close(); }
}

public async Task<bool> ChangeOwnPassword(int userId, string currentPassword, string newHashedPassword)
{
    using var db = _context.CreateConnection;
    try
    {
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            var storedHash = await db.ExecuteScalarAsync<string>(
                "SELECT password FROM sec.users WHERE id = @UserId AND is_active",
                new { UserId = userId }, transaction);

            if (string.IsNullOrEmpty(storedHash))
                throw new CustomException("Usuario no encontrado.");

            if (!Common.Utilities.Cryptography.Hash.VerifyPassword(storedHash, currentPassword))
                throw new CustomException("La contraseña actual es incorrecta.");

            var rows = await db.ExecuteAsync(
                @"UPDATE sec.users SET password = @NewPassword, change_password = false, modified = now()
                   WHERE id = @UserId",
                new { NewPassword = newHashedPassword, UserId = userId }, transaction);

            transaction.Commit();
            return rows > 0;
        }
        catch (CustomException ex) { transaction.Rollback(); throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { transaction.Rollback(); throw new Exception(ex.Message, ex); }
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
    catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
    finally { db.Close(); }
}

public async Task<List<Roles>> GetRolesByUserUuid(Guid uuid)
{
    using var db = _context.CreateConnection;
    try
    {
        db.Open();
        string query = @"SELECT r.id, r.name_rol, r.description, r.state
                           FROM sec.roles r
                                INNER JOIN sec.users_roles ur ON ur.rol_id = r.id
                                INNER JOIN sec.users u        ON u.id = ur.user_id
                          WHERE u.uuid = @Uuid
                            AND ur.state AND r.state AND u.is_active";
        var result = await db.QueryAsync<Roles>(query, new { Uuid = uuid });
        return [.. result];
    }
    catch (Exception ex) { throw ExceptionHandler.HandleException<List<Roles>>(ex); }
    finally { db.Close(); }
}

public async Task AssignRolesToUser(Guid uuid, List<int> roleIds, int modifiedBy)
{
    using var db = _context.CreateConnection;
    try
    {
        db.Open();
        using var transaction = db.BeginTransaction();
        try
        {
            int userId = await db.ExecuteScalarAsync<int>(
                "SELECT id FROM sec.users WHERE uuid = @Uuid AND is_active",
                new { Uuid = uuid }, transaction);

            if (userId == 0) throw new CustomException("Usuario no encontrado o inactivo.");

            await db.ExecuteAsync(
                @"UPDATE sec.users_roles
                     SET state = false, modified_by = @ModifiedBy, modified = NOW()
                   WHERE user_id = @UserId",
                new { UserId = userId, ModifiedBy = modifiedBy }, transaction);

            foreach (var roleId in roleIds)
            {
                int rows = await db.ExecuteAsync(
                    @"UPDATE sec.users_roles
                         SET state = true, modified_by = @ModifiedBy, modified = NOW()
                       WHERE user_id = @UserId AND rol_id = @RolId",
                    new { UserId = userId, RolId = roleId, ModifiedBy = modifiedBy }, transaction);

                if (rows == 0)
                {
                    await db.ExecuteAsync(
                        @"INSERT INTO sec.users_roles
                                (user_id, rol_id, state, created_by, created, modified_by, modified)
                          VALUES(@UserId, @RolId, true, @ModifiedBy, NOW(), @ModifiedBy, NOW())",
                        new { UserId = userId, RolId = roleId, ModifiedBy = modifiedBy }, transaction);
                }
            }

            transaction.Commit();
        }
        catch (CustomException ex) { transaction.Rollback(); throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { transaction.Rollback(); throw new Exception(ex.Message, ex); }
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
    catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
    finally { db.Close(); }
}
}
