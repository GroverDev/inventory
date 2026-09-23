using System.Data;
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

        // Filtro de estado: true = solo activos, false = solo inactivos, null = todos.
        string activeFilter = userSearchRequest.IsActive switch
        {
            true  => " u.is_active ",
            false => " NOT u.is_active ",
            _     => " TRUE "
        };

        userSearchRequest.Email = "%" + userSearchRequest.Email + "%";
        userSearchRequest.FullName = "%" + userSearchRequest.FullName + "%";
        db.Open();
        string query = @"SELECT u.uuid::Text as uuid, u.user_name, u.email, u.full_name, u.last_access,
                                        u.change_password, u.is_active,
                                        COALESCE(m.is_enabled,  false) AS mfa_enabled,
                                        COALESCE(m.is_required, false) AS mfa_required
                                 FROM sec.users u
                                 LEFT JOIN sec.user_mfa m ON m.user_id = u.id AND m.mfa_type = 'totp'
                                WHERE " + activeFilter + " AND " + subquery;
        var listResp = await db.QueryAsync<UsersResponse>(query, new { userSearchRequest.Email, full_name = userSearchRequest.FullName });
        objResp = listResp.ToList();
    }
    catch (Exception ex) { throw ExceptionHandler.HandleException<List<Forms>>(ex); }
    finally { db.Close(); }
    return objResp;
}

public async Task<string> CreateUser(Users user, int userId)
{
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
            await HabilitarEnSucursalActiva(db, transaction, user);
            transaction.Commit();
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
    catch (Exception ex) { throw ExceptionHandler.HandleException<string>(ex); }
    finally { db.Close(); }
    return user.Uuid.ToString();
}
/// <summary>
/// Habilita al usuario nuevo en la sucursal activa de quien lo crea, como su
/// default. Sin ninguna sucursal no podría iniciar sesión.
/// </summary>
/// <remarks>
/// tenant_id y branch_id salen de la sesión (DEFAULT y current_branch()), igual
/// que en el resto de los INSERT: sin sucursal en la sesión, falla en vez de
/// elegir una a ciegas.
/// </remarks>
private static Task HabilitarEnSucursalActiva(IDbConnection db, IDbTransaction transaction, Users user) =>
    db.ExecuteAsync(@"INSERT INTO sec.users_branches (user_id, branch_id, is_default, created_by, modified_by)
                      VALUES (@Id, public.current_branch(), true, @CreatedBy, @CreatedBy)",
                    user, transaction);

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
            await HabilitarEnSucursalActiva(db, transaction, user);
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

public async Task<List<UserBranchResponse>> GetBranchesByUserUuid(Guid uuid)
{
    using var db = _context.CreateConnection;
    try
    {
        db.Open();
        // Todas las sucursales activas, habilitadas o no: la pantalla muestra la
        // lista completa para marcar y desmarcar.
        const string query = @"
            SELECT b.id AS branch_id, b.name,
                   ub.user_id IS NOT NULL         AS enabled,
                   coalesce(ub.is_default, false) AS is_default
              FROM public.branches b
              LEFT JOIN sec.users_branches ub
                     ON ub.branch_id = b.id
                    AND ub.user_id = (SELECT id FROM sec.users WHERE uuid = @Uuid)
             WHERE b.state AND b.is_active
             ORDER BY b.name";
        var result = await db.QueryAsync<UserBranchResponse>(query, new { Uuid = uuid });
        return [.. result];
    }
    catch (Exception ex) { throw ExceptionHandler.HandleException<List<UserBranchResponse>>(ex); }
    finally { db.Close(); }
}

public async Task AssignBranchesToUser(Guid uuid, List<Guid> branchIds, Guid defaultBranchId, int modifiedBy)
{
    // Sin sucursales, o con una default que no está entre ellas, el usuario no
    // podría iniciar sesión.
    branchIds = branchIds.Distinct().ToList();
    if (branchIds.Count == 0)
        throw new CustomException("El usuario debe quedar habilitado en al menos una sucursal.");
    if (!branchIds.Contains(defaultBranchId))
        throw new CustomException("La sucursal por defecto debe estar entre las habilitadas.");

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

            // Solo sucursales activas de esta farmacia: las de otra no se ven
            // (RLS) y la FK compuesta las rechazaría de todos modos.
            int validas = await db.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM public.branches WHERE id = ANY(@Ids) AND state AND is_active",
                new { Ids = branchIds.ToArray() }, transaction);

            if (validas != branchIds.Count)
                throw new CustomException("Alguna de las sucursales elegidas no existe o está inactiva.");

            // Reemplazo completo. Borra también las filas de sucursales dadas de
            // baja, que la pantalla no muestra y ya no sirven.
            await db.ExecuteAsync("DELETE FROM sec.users_branches WHERE user_id = @UserId",
                new { UserId = userId }, transaction);

            await db.ExecuteAsync(@"
                INSERT INTO sec.users_branches (user_id, branch_id, is_default, created_by, modified_by)
                SELECT @UserId, b, b = @DefaultId, @ModifiedBy, @ModifiedBy
                  FROM unnest(@Ids) AS b",
                new { UserId = userId, Ids = branchIds.ToArray(), DefaultId = defaultBranchId, ModifiedBy = modifiedBy },
                transaction);

            transaction.Commit();
        }
        catch (CustomException) { transaction.Rollback(); throw; }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw new Exception(ex.Message, ex);
        }
    }
    catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
    catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
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
