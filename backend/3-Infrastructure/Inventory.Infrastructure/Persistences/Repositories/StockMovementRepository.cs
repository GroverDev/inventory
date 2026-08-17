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

    public async Task<List<StockExpiryResponse>> GetExpiring(int dias)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            // La vista ya clasifica y calcula el valor en riesgo. El orden es por
            // fecha: lo primero de la lista es lo que hay que rotar hoy.
            string sql = @"
                SELECT stock_item_id, product_id, product_code, product_name,
                       lot_code, expiry_date, quantity,
                       dias_restantes AS DiasRestantes,
                       estado         AS Estado,
                       valor_en_riesgo AS ValorEnRiesgo
                  FROM v_stock_por_vencer
                 WHERE @Dias <= 0 OR expiry_date <= CURRENT_DATE + @Dias
                 ORDER BY expiry_date;
            ";
            var result = await db.QueryAsync<StockExpiryResponse>(sql, new { Dias = dias });
            return [.. result];
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<StockExpiryResponse>>(ex); }
        finally { db.Close(); }
    }

    public async Task<List<LotTraceabilityResponse>> GetTraceability(string lotCode)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            // El lote se compara sin distinguir mayúsculas ni espacios: en un
            // retiro de mercado el código llega dictado por teléfono o copiado
            // de un correo, no elegido de una lista.
            // Un mismo buscador para lote y serie: quien investiga un retiro o una
            // garantía tiene un código en la mano y no tiene por qué saber con
            // qué modo se registró ese producto.
            string sql = @"
                SELECT lot_code, '' AS serial_number, expiry_date, product_code, product_name,
                       sale_id, sale_date, quantity,
                       cliente, document_number, cellphone
                  FROM v_trazabilidad_lote
                 WHERE upper(trim(lot_code)) = upper(trim(@LotCode))
                UNION ALL
                SELECT '' AS lot_code, serial_number, expiry_date, product_code, product_name,
                       sale_id, sale_date, 1 AS quantity,
                       cliente, document_number, cellphone
                  FROM v_trazabilidad_serie
                 WHERE upper(trim(serial_number)) = upper(trim(@LotCode))
                 ORDER BY sale_date DESC;
            ";
            var result = await db.QueryAsync<LotTraceabilityResponse>(sql, new { LotCode = lotCode });
            return [.. result];
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<LotTraceabilityResponse>>(ex); }
        finally { db.Close(); }
    }

    public async Task<List<StockSerialResponse>> GetAvailableSerials(Guid productId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            // Orden por vencimiento: aunque el mostrador elige, la sugerencia
            // sensata sigue siendo entregar primero lo que vence antes. Las que
            // no vencen van al final.
            string sql = @"
                SELECT id AS stock_item_id, serial_number, expiry_date
                  FROM stock_items
                 WHERE product_id = @ProductId
                   AND serial_number IS NOT NULL
                   AND quantity > 0
                   AND state
                 ORDER BY expiry_date NULLS LAST, serial_number;
            ";
            var result = await db.QueryAsync<StockSerialResponse>(sql, new { ProductId = productId });
            return [.. result];
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<List<StockSerialResponse>>(ex); }
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
