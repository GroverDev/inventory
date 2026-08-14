using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Seguridad.Domain.Entities.requests;
using Seguridad.Domain.Entities.responses;

namespace Seguridad.Infrastructure;

public class AdminRepository(SeguridadDbContext _context) : IAdminRepository
{
    /// <summary>Nombre del rol autorizado para reiniciar la empresa.</summary>
    public const string SuperAdminRole = "SuperAdmin";

    /// <summary>
    /// Datos de negocio de una farmacia, <b>ordenados de tablas hijas a padres</b>.
    /// </summary>
    /// <remarks>
    /// El orden es obligatorio y no es cosmético. La versión anterior usaba
    /// <c>TRUNCATE ... CASCADE</c>, que resolvía las dependencias por su cuenta pero
    /// es justamente la sentencia que Row-Level Security <b>no</b> filtra: en
    /// multi-tenant habría borrado los datos de todas las farmacias. Con
    /// <c>DELETE</c> el aislamiento lo aplica la política, pero las claves foráneas
    /// vuelven a importar.
    /// <para>
    /// Quedan deliberadamente fuera: <c>sec.forms</c>, <c>sec.modules</c> y
    /// <c>public.purchases_status</c> (catálogos globales), <c>public.sequences_key</c>
    /// (generador global de claves primarias), <c>public.zlogs_app</c> (auditoría),
    /// el schema <c>siat</c> (sin tenant_id y fuera de alcance) y todo <c>sec</c>
    /// relativo a usuarios, roles y permisos, que el reinicio conserva.
    /// </para>
    /// </remarks>
    private static readonly (string Schema, string Table)[] TenantDataTables =
    [
        ("public", "cash_movements"),
        ("public", "sale_detail_discounts"),
        ("public", "sale_payments"),
        ("public", "sale_return_detail"),
        ("public", "sale_returns"),
        ("public", "sales_detail"),
        ("public", "sales"),
        ("public", "cash_sessions"),
        ("public", "stock_movements"),
        ("public", "purchases_delivery_detail"),
        ("public", "purchases_delivery"),
        ("public", "purchases_detail"),
        ("public", "purchases"),
        ("public", "products_providers"),
        ("public", "products"),
        ("public", "discounts"),
        ("public", "customers"),
        ("public", "providers"),
        ("public", "categories"),
        ("public", "laboratories"),
        ("public", "unit_of_measurement"),
        ("public", "payment_methods")
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

    public async Task<bool> UserIsPlatformAdmin(int userId)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();
            return await db.ExecuteScalarAsync<bool>(
                "SELECT COALESCE(is_platform_admin, false) FROM sec.users WHERE id = @UserId AND is_active",
                new { UserId = userId });
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task<CreateTenantResponse> CreateTenant(CreateTenantRequest request)
    {
        using var db = _context.CreateConnection;
        try
        {
            db.Open();

            // El hash se calcula acá y no en SQL: replicar PBKDF2-SHA512 en la base
            // sería una fuente de divergencia silenciosa el día que cambien las
            // iteraciones o el formato.
            string hash = Common.Utilities.Cryptography.Hash.HashPassword(request.AdminPassword);

            // La función es SECURITY DEFINER: crear una farmacia cruza tenants por
            // definición, y con RLS activo cada INSERT para la farmacia nueva
            // rebotaría contra WITH CHECK si se hiciera desde la sesión del que llama.
            // Valida slug y correo duplicados por su cuenta, y es transaccional.
            int tenantId = await db.ExecuteScalarAsync<int>(
                "SELECT sec.fn_provision_tenant(@Name, @Slug, @Email, @FullName, @Password)",
                new
                {
                    Name = request.Name.Trim(),
                    Slug = request.Slug.Trim().ToLowerInvariant(),
                    Email = request.AdminEmail.Trim().ToLowerInvariant(),
                    FullName = request.AdminFullName.Trim(),
                    Password = hash
                });

            return new CreateTenantResponse
            {
                TenantId = tenantId,
                Name = request.Name.Trim(),
                Slug = request.Slug.Trim().ToLowerInvariant(),
                AdminEmail = request.AdminEmail.Trim().ToLowerInvariant()
            };
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "P0001")
        {
            // RAISE EXCEPTION de fn_provision_tenant: slug repetido, correo ya usado
            // en otra farmacia o nombre vacío. Son mensajes pensados para el usuario.
            throw new CustomException(ex.MessageText);
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<CreateTenantResponse>(ex); }
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

                // 2) Tenant sobre el que se opera. No hace falta filtrar por él en cada
                //    sentencia: con RLS activo, un DELETE sin WHERE ya alcanza solo las
                //    filas de esta farmacia. Se lee únicamente para nombrar el respaldo.
                int tenantId = await db.ExecuteScalarAsync<int>(
                    "SELECT public.current_tenant()", transaction: transaction);

                if (tenantId <= 0)
                    throw new CustomException("No se pudo determinar la farmacia de la sesión.");

                // 3) Respaldo previo en el schema 'backup'. El SELECT que lo alimenta
                //    también está filtrado por RLS, así que la copia contiene solo las
                //    filas de esta farmacia.
                string backupPrefix = "";
                if (!request.SkipBackup)
                {
                    backupPrefix = $"t{tenantId}_{DateTime.Now:yyyyMMdd_HHmmss}";
                    foreach (var (schema, table) in TenantDataTables)
                    {
                        string dest = $"backup.{Q($"{backupPrefix}__{schema}__{table}")}";
                        string src = $"{Q(schema)}.{Q(table)}";
                        await db.ExecuteAsync($"CREATE TABLE {dest} AS TABLE {src}", transaction: transaction);
                    }
                }

                // 4) Borrar los datos de negocio de ESTA farmacia.
                //
                //    Antes esto era un TRUNCATE ... CASCADE. En multi-tenant eso es
                //    catastrófico: TRUNCATE es la única sentencia de datos que RLS NO
                //    filtra, así que habría borrado las filas de todas las farmacias.
                //
                //    DELETE sí pasa por la política, de modo que cada sentencia alcanza
                //    solo lo propio. A cambio hay que respetar el orden de las claves
                //    foráneas, que CASCADE resolvía solo: por eso TenantDataTables está
                //    ordenada de hijas a padres.
                foreach (var (schema, table) in TenantDataTables)
                    await db.ExecuteAsync($"DELETE FROM {Q(schema)}.{Q(table)}", transaction: transaction);

                // 5) public.sequences_key NO se reinicia: es un generador GLOBAL de
                //    claves primarias compartido por todas las farmacias. Reiniciarlo
                //    haría que la siguiente farmacia en crear un usuario colisionara
                //    contra pk_user.
                //
                //    Las tablas de siat quedan fuera por partida doble: no tienen
                //    tenant_id y la facturación electrónica está fuera de alcance.

                // 6) Dejar la farmacia operativa: sin unidad de medida ni laboratorio no
                //    se puede registrar un producto, y sin método de pago no se puede
                //    vender. Es la misma siembra que usa el alta de una farmacia nueva.
                await db.ExecuteAsync("SELECT sec.fn_seed_tenant_master_data(@T)",
                    new { T = tenantId }, transaction);

                // 7) Los usuarios, roles y permisos de la farmacia NO se tocan.
                //
                //    En la versión de un solo cliente, reiniciar borraba también los
                //    usuarios y creaba un administrador nuevo, porque "reiniciar" era
                //    literalmente empezar de cero con otra empresa. En multi-tenant eso
                //    dejaría a la farmacia sin acceso a su propia cuenta: quien ejecuta
                //    el reinicio se borraría a sí mismo. Acá reiniciar significa vaciar
                //    los datos de negocio, no rehacer la organización.

                transaction.Commit();
                return backupPrefix;
            }
            catch (CustomException ex) { transaction.Rollback(); throw new CustomException(ex.Message, ex); }
            catch (Exception ex) { transaction.Rollback(); throw new Exception(ex.Message, ex); }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<string>(ex); }
        finally { db.Close(); }
    }
}
