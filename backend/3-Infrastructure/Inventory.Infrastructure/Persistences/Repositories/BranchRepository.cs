using System.Data;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

/// <remarks>
/// Ninguna consulta filtra tenant_id: branches está bajo RLS y el DEFAULT de la
/// columna lo toma de la sesión, como en el resto de los maestros.
/// </remarks>
public class BranchRepository(InventoryDbContext _DbContext) : IBranchRepository
{
    public async Task<Guid> CreateBranch(Branch branch)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                await ValidarNombreUnico(db, transaction, branch.Name, Guid.Empty);

                branch.Id = Guid.NewGuid();
                await db.ExecuteAsync(@"
                    INSERT INTO branches
                           (id, name, code, address, phone, is_active, state, created_by, created, modified_by, modified)
                    VALUES (@Id, @Name, NULLIF(@Code, ''), @Address, @Phone, @IsActive, true, @CreatedBy, @Created, @ModifiedBy, @Modified)",
                    branch, transaction);

                // Quien crea la sucursal queda habilitado en ella (no como default):
                // si no, para entrar a la sucursal que acaba de crear tendría que
                // ir a editar su propio usuario.
                await db.ExecuteAsync(@"
                    INSERT INTO sec.users_branches (user_id, branch_id, is_default, created_by, modified_by)
                    VALUES (@CreatedBy, @Id, false, @CreatedBy, @CreatedBy)
                    ON CONFLICT DO NOTHING",
                    branch, transaction);

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
        catch (Exception ex) { throw ExceptionHandler.HandleException<Guid>(ex); }
        finally { db.Close(); }

        return branch.Id;
    }

    public async Task<int> UpdateBranch(Branch branch)
    {
        using var db = _DbContext.CreateConnection;
        int numberRows;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                await ValidarNombreUnico(db, transaction, branch.Name, branch.Id);

                if (!branch.IsActive)
                    await ValidarQueNadieQuedeSinSucursal(db, transaction, branch.Id);

                numberRows = await db.ExecuteAsync(@"
                    UPDATE branches
                       SET name        = @Name,
                           code        = NULLIF(@Code, ''),
                           address     = @Address,
                           phone       = @Phone,
                           is_active   = @IsActive,
                           modified_by = @ModifiedBy,
                           modified    = @Modified
                     WHERE id = @Id AND state",
                    branch, transaction);

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
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }

        return numberRows;
    }

    public async Task<List<Branch>> GetBranches(string name, bool includeInactive)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var result = await db.QueryAsync<Branch>(@"
                SELECT b.id, b.name, coalesce(b.code, '') AS code, b.address, b.phone, b.is_active,
                       (SELECT count(*) FROM sec.users_branches ub
                          JOIN sec.users u ON u.id = ub.user_id AND u.is_active
                         WHERE ub.branch_id = b.id)::int AS users_count
                  FROM branches b
                 WHERE b.state
                   AND (@includeInactive OR b.is_active)
                   AND b.name ILIKE @name
                 ORDER BY b.name",
                new { name = "%" + name + "%", includeInactive });
            return result.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<Branch>>(ex); }
        finally { db.Close(); }
    }

    public async Task<Branch> GetBranch(Guid id)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var branch = await db.QueryFirstOrDefaultAsync<Branch>(@"
                SELECT id, name, coalesce(code, '') AS code, address, phone, is_active
                  FROM branches
                 WHERE state AND id = @id",
                new { id });

            return branch ?? throw new CustomException("No se encontró la sucursal.", MessageTypes.Info);
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<Branch>(ex); }
        finally { db.Close(); }
    }

    public async Task<int> DeleteBranch(Guid id, int modifiedBy)
    {
        using var db = _DbContext.CreateConnection;
        int numberRows;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                await ValidarQueNadieQuedeSinSucursal(db, transaction, id);

                // Baja lógica: ventas, cajas y movimientos siguen apuntando a ella
                // y deben poder consultarse después.
                numberRows = await db.ExecuteAsync(@"
                    UPDATE branches
                       SET state = false, is_active = false,
                           modified_by = @modifiedBy, modified = now()
                     WHERE id = @id AND state",
                    new { id, modifiedBy }, transaction);

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
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }

        return numberRows;
    }

    public async Task<List<Guid>> GetUserBranchIds(int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var ids = await db.QueryAsync<Guid>(@"
                SELECT b.id
                  FROM sec.users_branches ub
                  JOIN branches b ON b.id = ub.branch_id AND b.state AND b.is_active
                 WHERE ub.user_id = @userId",
                new { userId });
            return ids.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<Guid>>(ex); }
        finally { db.Close(); }
    }

    private static async Task ValidarNombreUnico(IDbConnection db, IDbTransaction transaction, string name, Guid id)
    {
        bool existe = await db.ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (SELECT 1 FROM branches
                            WHERE state AND lower(name) = lower(@name) AND id <> @id)",
            new { name, id }, transaction);

        if (existe) throw new CustomException($"Ya existe una sucursal con el nombre «{name}».");
    }

    /// <summary>
    /// Desactivar o eliminar una sucursal no puede dejar a nadie sin sucursal
    /// activa: ese usuario ya no podría iniciar sesión. Cubre también el caso de
    /// la última sucursal de la farmacia.
    /// </summary>
    private static async Task ValidarQueNadieQuedeSinSucursal(IDbConnection db, IDbTransaction transaction, Guid id)
    {
        bool quedaOtra = await db.ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (SELECT 1 FROM branches WHERE state AND is_active AND id <> @id)",
            new { id }, transaction);

        if (!quedaOtra)
            throw new CustomException("Es la única sucursal activa de la farmacia: no se puede desactivar ni eliminar.");

        var huerfanos = (await db.QueryAsync<string>(@"
            SELECT u.full_name
              FROM sec.users u
             WHERE u.is_active
               AND EXISTS (SELECT 1 FROM sec.users_branches ub
                            WHERE ub.user_id = u.id AND ub.branch_id = @id)
               AND NOT EXISTS (SELECT 1 FROM sec.users_branches ub
                                 JOIN branches b ON b.id = ub.branch_id AND b.state AND b.is_active
                                WHERE ub.user_id = u.id AND b.id <> @id)
             ORDER BY u.full_name",
            new { id }, transaction)).ToList();

        if (huerfanos.Count > 0)
            throw new CustomException(
                "Estos usuarios quedarían sin ninguna sucursal activa y no podrían iniciar sesión: " +
                string.Join(", ", huerfanos) + ". Habilítelos antes en otra sucursal.");
    }
}
