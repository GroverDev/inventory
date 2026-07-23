using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Seguridad.Domain;

namespace Seguridad.Infrastructure;

public class RolesRepository(SeguridadDbContext _context) : IRolesRepository
{
    public async Task<List<Roles>> GetRolesXUserId(int userId)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string query = @"SELECT r.id, r.name_rol, r.description, r.state
                               FROM sec.users u
                                    INNER JOIN sec.users_roles ur ON ur.user_id = u.id
                                    INNER JOIN sec.roles r        ON r.id = ur.rol_id
                                    INNER JOIN sec.roles_forms rf ON rf.rol_id = r.id
                                    INNER JOIN sec.forms f        ON f.id = rf.form_id
                              WHERE u.id = @IdUsuario
                                AND u.is_active AND ur.state AND r.state AND rf.state AND f.state";
            var result = await db.QueryAsync<Roles>(query, new { IdUsuario = userId });
            return result.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<Roles>>(ex); }
        finally { db.Close(); }
    }

    public async Task<List<Roles>> GetRoles(RolSearchRequest rolSearch)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string subquery = rolSearch.NameRol.Trim().Length > 0
                ? "r.name_rol ILIKE @NameRol"
                : "r.description ILIKE @Description";
            rolSearch.NameRol = "%" + rolSearch.NameRol + "%";
            rolSearch.Description = "%" + rolSearch.Description + "%";

            string query = $@"SELECT r.id, r.name_rol, r.description, r.state
                                FROM sec.roles r
                               WHERE r.state AND {subquery}
                               ORDER BY r.name_rol";
            var result = await db.QueryAsync<Roles>(query, new { rolSearch.NameRol, rolSearch.Description });
            return result.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<Roles>>(ex); }
        finally { db.Close(); }
    }

    public async Task<Roles> GetRoleById(int id)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string query = @"SELECT id, name_rol, description, state
                               FROM sec.roles
                              WHERE state AND id = @Id";
            var result = await db.QueryFirstOrDefaultAsync<Roles>(query, new { Id = id });
            if (result == null) throw new CustomException("Rol no encontrado", MessageTypes.Info);
            return result;
        }
        catch (CustomException) { throw; }
        catch (Exception ex) { throw ExceptionHandler.HandleException<Roles>(ex); }
        finally { db.Close(); }
    }

    public async Task<int> CreateRole(Roles role)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                role.Id = await db.ExecuteScalarAsync<int>(
                    "select set_sequences_key(@TableName)", new { TableName = "sec.roles" }, transaction);

                string sql = @"INSERT INTO sec.roles
                                     (id, name_rol, description, state, created_by, created, modified_by, modified)
                               VALUES(@Id, @NameRol, @Description, @State, @CreatedBy, @Created, @ModifiedBy, @Modified)";
                await db.ExecuteAsync(sql, role, transaction);
                transaction.Commit();
                return role.Id;
            }
            catch (Exception ex) { transaction.Rollback(); throw new Exception(ex.Message, ex); }
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }
    }

    public async Task<int> UpdateRole(Roles role)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string sql = @"UPDATE sec.roles
                              SET name_rol = @NameRol,
                                  description = @Description,
                                  modified_by = @ModifiedBy,
                                  modified = @Modified
                            WHERE id = @Id";
            return await db.ExecuteAsync(sql, role);
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }
    }

    public async Task<int> DeleteRole(int id, int modifiedBy)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            string sql = @"UPDATE sec.roles
                              SET state = false, modified_by = @ModifiedBy, modified = NOW()
                            WHERE id = @Id";
            return await db.ExecuteAsync(sql, new { Id = id, ModifiedBy = modifiedBy });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }
    }

    public async Task AssignFormsToRole(int rolId, List<int> formIds, int userId)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                // Desactivar todas las asignaciones actuales del rol
                await db.ExecuteAsync(
                    @"UPDATE sec.roles_forms
                         SET state = false, modified_by = @UserId, modified = NOW()
                       WHERE rol_id = @RolId",
                    new { RolId = rolId, UserId = userId }, transaction);

                // Activar o insertar cada formulario seleccionado
                foreach (var formId in formIds)
                {
                    int rows = await db.ExecuteAsync(
                        @"UPDATE sec.roles_forms
                             SET state = true, modified_by = @UserId, modified = NOW()
                           WHERE rol_id = @RolId AND form_id = @FormId",
                        new { RolId = rolId, FormId = formId, UserId = userId }, transaction);

                    if (rows == 0)
                    {
                        
                        await db.ExecuteAsync(
                            @"INSERT INTO sec.roles_forms
                                    ( rol_id, form_id, state, created_by, created, modified_by, modified)
                              VALUES( @RolId, @FormId, true, @UserId, NOW(), @UserId, NOW())",
                            new { RolId = rolId, FormId = formId, UserId = userId }, transaction);
                    }
                }
                transaction.Commit();
            }
            catch (Exception ex) { transaction.Rollback(); throw new Exception(ex.Message, ex); }
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task<bool> HasFormPermission(int userId, string formRoute, string action)
    {
        // El nombre de columna proviene de un switch cerrado (no de la entrada del usuario) → sin riesgo de inyección.
        string col = action switch
        {
            "create" => "can_create",
            "update" => "can_update",
            "delete" => "can_delete",
            _        => "can_read"
        };

        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            // Unión de permisos entre los roles del usuario. NULL = acceso total (COALESCE true).
            // Si el usuario no tiene el formulario asignado, bool_or sobre vacío es NULL → false (sin acceso).
            string query = $@"SELECT COALESCE(bool_or(COALESCE(rf.{col}, true)), false)
                                FROM sec.users_roles ur
                                     INNER JOIN sec.roles_forms rf ON rf.rol_id = ur.rol_id
                                     INNER JOIN sec.forms f        ON f.id      = rf.form_id
                               WHERE ur.user_id = @UserId
                                 AND ur.state AND rf.state AND f.state
                                 AND f.route = @FormRoute";
            return await db.ExecuteScalarAsync<bool>(query, new { UserId = userId, FormRoute = formRoute });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }
}
