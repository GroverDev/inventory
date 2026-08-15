using System.Data;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;

namespace Inventory.Infrastructure;

public class StockMovementRepository(InventoryDbContext _DbContext) : IStockMovementRepository
{
    public async Task<List<StockMovementResponse>> GetMovementsByProduct(Guid productId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sql = @"
                SELECT sm.id,
                       sm.product_id,
                       p.product_name,
                       p.product_code,
                       sm.movement_type,
                       sm.quantity,
                       sm.stock_before,
                       sm.stock_after,
                       sm.reason,
                       sm.observation,
                       sm.reference_id,
                       sm.reference_type,
                       sm.created,
                       sm.created_by
                  FROM stock_movements sm
                       INNER JOIN products p ON p.id = sm.product_id
                 WHERE sm.product_id = @ProductId
                   AND sm.state
                 ORDER BY sm.created DESC;
            ";
            var result = await db.QueryAsync<StockMovementResponse>(sql, new { ProductId = productId });
            return [.. result];
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<StockMovementResponse>>(ex); }
        finally { db.Close(); }
    }

    public async Task CreateAdjustment(StockMovement movement, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                // El ajuste sí valida que no quede negativo: es una corrección
                // manual, y dejar el inventario en negativo a propósito no tiene
                // sentido. Los otros caminos (venta, compra, devolución) responden
                // a hechos ya ocurridos y no se bloquean.
                var saldo = await db.QueryFirstAsync<(Guid StockItemId, decimal StockBefore, decimal StockAfter)>(
                    "SELECT stock_item_id, stock_before, stock_after FROM fn_mover_stock(@ProductId, @Delta, @UserId)",
                    new { movement.ProductId, Delta = (decimal)movement.Quantity, UserId = userId },
                    transaction);

                if (saldo.StockAfter < 0)
                    throw new CustomException("El stock resultante no puede ser negativo.");

                movement.Id = Guid.NewGuid();
                movement.StockItemId = saldo.StockItemId;
                movement.StockBefore = (int)saldo.StockBefore;
                movement.StockAfter = (int)saldo.StockAfter;
                movement.MovementType = "AJUSTE";
                movement.State = true;
                movement.CreatedBy = movement.ModifiedBy = userId;
                movement.Created = movement.Modified = DateTime.Now;

                await InsertMovement(movement, db, transaction);

                transaction.Commit();
            }
            catch (CustomException ex) { transaction.Rollback(); throw new CustomException(ex.Message, ex); }
            catch (Exception ex) { transaction.Rollback(); throw new Exception(ex.Message, ex); }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }

    public async Task InsertMovement(StockMovement movement, IDbConnection db, IDbTransaction transaction)
    {
        string sql = @"
            INSERT INTO stock_movements
                   (id, product_id, stock_item_id, movement_type, quantity, stock_before, stock_after,
                    reason, observation, reference_id, reference_type,
                    state, created_by, created, modified_by, modified)
            VALUES (@Id, @ProductId, @StockItemId, @MovementType, @Quantity, @StockBefore, @StockAfter,
                    @Reason, @Observation, @ReferenceId, @ReferenceType,
                    @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
        ";
        await db.ExecuteAsync(sql, movement, transaction);
    }
}
