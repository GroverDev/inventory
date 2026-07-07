using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Seguridad.Domain.Entities.requests;

namespace Seguridad.Infrastructure;

public class AdminRepository(SeguridadDbContext _context) : IAdminRepository
{
    /// <summary>Nombre del rol autorizado para reiniciar la empresa.</summary>
    public const string SuperAdminRole = "SuperAdmin";

    /// <summary>
    /// Tablas que NO se truncan: estructura de menús/permisos, lookups del sistema,
    /// registro de auditoría y tablas generadoras de contadores.
    /// </summary>
    private static readonly HashSet<string> PreservedTables =
    [
        "public.sequences_key",     // contadores (se reinician con UPDATE)
        "public.zlogs_app",         // logs de auditoría (se conservan)
        "public.purchases_status",  // lookup de estados de compra (referenciado por enum)
        "sec.forms",                // estructura de formularios/menús
        "sec.modules",              // módulos
        "sec.roles",                // definición de roles/permisos
        "sec.roles_forms",          // asignación de formularios a roles
        "siat.llaves_primarias",    // generador de PKs SIAT (se reinicia con UPDATE)
        "siat.secuencia_facturas"   // secuencia de facturas SIAT (se reinicia con UPDATE)
    ];

    private static string Q(string ident) => "\"" + ident.Replace("\"", "\"\"") + "\"";

    public async Task<bool> UserHasActiveRole(int userId, string roleName)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            const string query = @"
                SELECT EXISTS (
                    SELECT 1
                      FROM sec.users_roles ur
                           INNER JOIN sec.roles r ON r.id = ur.rol_id
                           INNER JOIN sec.users u ON u.id = ur.user_id
                     WHERE u.id = @UserId AND u.is_active AND ur.state AND r.state
                       AND LOWER(r.name_rol) = LOWER(@RoleName)
                )";
            return await db.ExecuteScalarAsync<bool>(query, new { UserId = userId, RoleName = roleName });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task<string> ResetCompany(ResetCompanyRequest request, int executedByUserId)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                // 1) Re-validar identidad y autorización DENTRO de la transacción (autoridad final).
                //    Se hace primero porque el usuario ejecutor será eliminado al truncar.
                var me = await db.QueryFirstOrDefaultAsync(
                    @"SELECT u.password AS password,
                             EXISTS (
                                 SELECT 1 FROM sec.users_roles ur
                                        INNER JOIN sec.roles r ON r.id = ur.rol_id
                                  WHERE ur.user_id = u.id AND ur.state AND r.state
                                    AND LOWER(r.name_rol) = LOWER(@Role)
                             ) AS is_super
                        FROM sec.users u
                       WHERE u.id = @Uid AND u.is_active",
                    new { Uid = executedByUserId, Role = SuperAdminRole }, transaction);

                if (me == null)
                    throw new CustomException("Usuario no encontrado o inactivo.");
                if (!(bool)me.is_super)
                    throw new CustomException("Solo un usuario con rol SuperAdmin puede reiniciar la empresa.");
                if (!Common.Utilities.Cryptography.Hash.VerifyPassword((string)me.password, request.CurrentPassword))
                    throw new CustomException("La contraseña actual es incorrecta.");

                // 2) Determinar dinámicamente las tablas a truncar (todo public/sec/siat menos las preservadas).
                var rows = await db.QueryAsync(
                    "SELECT schemaname, tablename FROM pg_tables WHERE schemaname IN ('public','sec','siat')",
                    transaction: transaction);

                var toProcess = new List<(string Schema, string Table)>();
                foreach (var r in rows)
                {
                    string schema = (string)r.schemaname;
                    string table = (string)r.tablename;
                    if (!PreservedTables.Contains($"{schema}.{table}"))
                        toProcess.Add((schema, table));
                }

                // 3) Respaldo previo: copia de los datos a un esquema backup_<timestamp> (misma BD).
                string backupSchema = "";
                if (!request.SkipBackup)
                {
                    backupSchema = "backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    await db.ExecuteAsync($"CREATE SCHEMA {Q(backupSchema)}", transaction: transaction);
                    foreach (var (schema, table) in toProcess)
                    {
                        string dest = $"{Q(backupSchema)}.{Q($"{schema}__{table}")}";
                        string src = $"{Q(schema)}.{Q(table)}";
                        await db.ExecuteAsync($"CREATE TABLE {dest} AS TABLE {src}", transaction: transaction);
                    }
                }

                // 4) Truncar todas las tablas de datos (reinicia identity/serial y respeta FKs con CASCADE).
                if (toProcess.Count > 0)
                {
                    string list = string.Join(", ", toProcess.Select(t => $"{Q(t.Schema)}.{Q(t.Table)}"));
                    await db.ExecuteAsync($"TRUNCATE TABLE {list} RESTART IDENTITY CASCADE", transaction: transaction);
                }

                // 5) Reiniciar contadores manuales (no cubiertos por RESTART IDENTITY).
                await db.ExecuteAsync(
                    @"UPDATE public.sequences_key SET sequence_id = 0
                       WHERE table_name IN ('sec.users','sec.users_login','sec.users_resetpass')",
                    transaction: transaction);
                await db.ExecuteAsync("UPDATE siat.llaves_primarias SET secuencia = 0", transaction: transaction);
                await db.ExecuteAsync("UPDATE siat.secuencia_facturas SET secuencia = 0", transaction: transaction);

                // 6) Garantizar el rol SuperAdmin y que tenga acceso a todos los formularios/menús.
                int? rolId = await db.ExecuteScalarAsync<int?>(
                    "SELECT id FROM sec.roles WHERE LOWER(name_rol) = LOWER(@N) AND state LIMIT 1",
                    new { N = SuperAdminRole }, transaction);

                if (rolId is null or 0)
                {
                    int newRolId = await db.ExecuteScalarAsync<int>(
                        "SELECT set_sequences_key(@T)", new { T = "sec.roles" }, transaction);
                    await db.ExecuteAsync(
                        @"INSERT INTO sec.roles (id, name_rol, description, state, created_by, created, modified_by, modified)
                          VALUES (@Id, @N, 'Super administrador del sistema', true, 1, now(), 1, now())",
                        new { Id = newRolId, N = SuperAdminRole }, transaction);
                    rolId = newRolId;
                }

                await db.ExecuteAsync(
                    @"INSERT INTO sec.roles_forms
                          (rol_id, form_id, can_create, can_read, can_update, can_delete, state, created_by, created, modified_by, modified)
                      SELECT @RolId, f.id, true, true, true, true, true, 1, now(), 1, now()
                        FROM sec.forms f
                       WHERE f.state
                         AND NOT EXISTS (SELECT 1 FROM sec.roles_forms rf WHERE rf.rol_id = @RolId AND rf.form_id = f.id)",
                    new { RolId = rolId }, transaction);

                await db.ExecuteAsync(
                    @"UPDATE sec.roles_forms
                         SET state = true, can_create = true, can_read = true, can_update = true, can_delete = true, modified = now()
                       WHERE rol_id = @RolId",
                    new { RolId = rolId }, transaction);

                // 7) Crear el nuevo administrador de la empresa nueva y asignarle el rol SuperAdmin.
                int newUserId = await db.ExecuteScalarAsync<int>(
                    "SELECT set_sequences_key(@T)", new { T = "sec.users" }, transaction);
                string hash = Common.Utilities.Cryptography.Hash.HashPassword(request.NewAdminPassword);

                await db.ExecuteAsync(
                    @"INSERT INTO sec.users
                          (id, user_name, password, email, full_name, last_access, change_password, is_active, created_by, created, modified_by, modified, uuid)
                      VALUES
                          (@Id, @UserName, @Password, @Email, @FullName, now(), false, true, @Cb, now(), @Cb, now(), @Uuid)",
                    new
                    {
                        Id = newUserId,
                        UserName = request.NewAdminEmail,
                        Password = hash,
                        Email = request.NewAdminEmail,
                        FullName = request.NewAdminFullName,
                        Cb = newUserId,
                        Uuid = Guid.NewGuid()
                    }, transaction);

                await db.ExecuteAsync(
                    @"INSERT INTO sec.users_roles (user_id, rol_id, state, created_by, created, modified_by, modified)
                      VALUES (@UserId, @RolId, true, @UserId, now(), @UserId, now())",
                    new { UserId = newUserId, RolId = rolId }, transaction);

                transaction.Commit();
                return backupSchema;
            }
            catch (CustomException ex) { transaction.Rollback(); throw new CustomException(ex.Message, ex); }
            catch (Exception ex) { transaction.Rollback(); throw new Exception(ex.Message, ex); }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<string>(ex); }
        finally { db.Close(); }
    }
}
