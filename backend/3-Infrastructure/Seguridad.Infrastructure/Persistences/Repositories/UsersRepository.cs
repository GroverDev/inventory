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
            // Check if email/username exists for OTHER users (exclude current user by UUID)
            string checkQuery = @"SELECT EXISTS (
                                       SELECT 1 FROM sec.users 
                                       WHERE (LOWER(email) = LOWER(@Email)) 
                                         AND uuid != @Uuid
                                         AND is_active
                                     )";
            // Note: Removed checking userName because often userName IS the email, or we just want to update profile fields.
            // If checking both: (LOWER(user_name) = LOWER(@UserName) OR LOWER(email) = LOWER(@Email))
            // Assuming logic based on CreateUser:
            checkQuery = @"SELECT EXISTS (
                                       SELECT 1 FROM sec.users 
                                       WHERE (LOWER(email) = LOWER(@Email)) 
                                         AND uuid != @Uuid
                                         AND is_active
                                    )";

            var existeUsuario = db.ExecuteScalar<bool>(checkQuery, new { user.Email, user.Uuid }, transaction);
            if (existeUsuario) throw new CustomException("Ya existe un usuario con ese correo electrónico");

            string sqlQuery = @"UPDATE sec.users
                                   SET email = @Email,
                                       full_name = @FullName,
                                       modified_by = @ModifiedBy,
                                       modified = now()
                                   WHERE uuid = @Uuid";

            // We pass 'modifiedBy' as 'ModifiedBy' prop or arg. The 'user' object might not have it set, so we pass it in anon object or set it.
            var rows = await db.ExecuteAsync(sqlQuery, new { user.Email, user.FullName, ModifiedBy = modifiedBy, user.Uuid }, transaction);

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
}
