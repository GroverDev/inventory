using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class DiscountLimitsRepository(InventoryDbContext _DbContext) : IDiscountLimitsRepository
{
    private const string Columnas =
        "cashier_max_pct, cashier_max_amount, general_max_pct, general_max_amount";

    public async Task<List<DiscountLimitLevel>> GetForCurrentBranch()
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            var rows = await db.QueryAsync<DiscountLimitLevel>($@"
                SELECT branch_id, {Columnas}
                  FROM discount_limits
                 WHERE branch_id IS NULL OR branch_id = public.current_branch()");
            return rows.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<DiscountLimitLevel>>(ex); }
        finally { db.Close(); }
    }

    public async Task<List<DiscountLimitLevel>> GetLevels()
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            // La empresa primero (branch_id nulo), después las sucursales por nombre.
            var rows = await db.QueryAsync<DiscountLimitLevel>($@"
                SELECT NULL::uuid AS branch_id, '' AS branch_name, d.{Columnas.Replace(", ", ", d.")}
                  FROM (SELECT 1) x
                  LEFT JOIN discount_limits d ON d.branch_id IS NULL
                UNION ALL
                SELECT b.id, b.name, d.{Columnas.Replace(", ", ", d.")}
                  FROM branches b
                  LEFT JOIN discount_limits d ON d.branch_id = b.id
                 WHERE b.state AND b.is_active
                 ORDER BY 1 NULLS FIRST, 2");
            return rows.ToList();
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<DiscountLimitLevel>>(ex); }
        finally { db.Close(); }
    }

    public async Task SaveLevels(List<DiscountLimitLevel> levels, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                foreach (var n in levels)
                {
                    string donde = n.BranchId is null ? "branch_id IS NULL" : "branch_id = @BranchId";

                    if (n.IsEmpty)
                    {
                        await db.ExecuteAsync($"DELETE FROM discount_limits WHERE {donde}", n, transaction);
                        continue;
                    }

                    // Dos índices únicos parciales (empresa y sucursal): cada uno
                    // necesita su propio ON CONFLICT. La FK compuesta rechaza una
                    // sucursal de otra farmacia.
                    string conflicto = n.BranchId is null
                        ? "(tenant_id) WHERE branch_id IS NULL"
                        : "(branch_id) WHERE branch_id IS NOT NULL";

                    await db.ExecuteAsync($@"
                        INSERT INTO discount_limits
                               (branch_id, {Columnas}, created_by, modified_by)
                        VALUES (@BranchId, @CashierMaxPct, @CashierMaxAmount, @GeneralMaxPct, @GeneralMaxAmount, @UserId, @UserId)
                        ON CONFLICT {conflicto} DO UPDATE
                           SET cashier_max_pct    = EXCLUDED.cashier_max_pct,
                               cashier_max_amount = EXCLUDED.cashier_max_amount,
                               general_max_pct    = EXCLUDED.general_max_pct,
                               general_max_amount = EXCLUDED.general_max_amount,
                               modified_by        = EXCLUDED.modified_by,
                               modified           = now()",
                        new { n.BranchId, n.CashierMaxPct, n.CashierMaxAmount, n.GeneralMaxPct, n.GeneralMaxAmount, UserId = userId },
                        transaction);
                }

                transaction.Commit();
            }
            catch { transaction.Rollback(); throw; }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }
}
