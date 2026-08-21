using System.Data;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;

namespace Inventory.Infrastructure;

public class StockMovementRepository(InventoryDbContext _DbContext) : IStockMovementRepository
{
    public async Task<List<StockMovementResponse>> GetMovementsByProduct(Guid productId, Guid? stockItemId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            // LEFT JOIN, no INNER: un movimiento nunca debería quedar sin su
            // existencia, pero si algo estuviera inconsistente preferimos mostrar
            // la fila igual (lote vacío) a que desaparezca del historial.
            string sql = @"
                SELECT sm.id,
                       sm.product_id,
                       p.product_name,
                       p.product_code,
                       sm.stock_item_id,
                       si.lot_code,
                       si.expiry_date,
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
                       LEFT JOIN stock_items si ON si.id = sm.stock_item_id
                 WHERE sm.product_id = @ProductId
                   AND sm.state
                   AND (@StockItemId::uuid IS NULL OR sm.stock_item_id = @StockItemId)
                 ORDER BY sm.created DESC;
            ";
            var result = await db.QueryAsync<StockMovementResponse>(sql, new { ProductId = productId, StockItemId = stockItemId });
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
                movement.Created = movement.Modified = DateTime.UtcNow;

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

    public async Task CreateWriteOff(StockMovement movement, Guid stockItemId, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                // A diferencia del ajuste genérico, acá el lote es explícito: dar
                // de baja "el producto en general" no tiene sentido cuando lo que
                // venció es una existencia puntual. Se valida que pertenezca al
                // producto para no dar de baja el lote de otro por un id mal armado.
                bool perteneceAlProducto = await db.ExecuteScalarAsync<bool>(
                    "SELECT EXISTS(SELECT 1 FROM stock_items WHERE id = @StockItemId AND product_id = @ProductId)",
                    new { StockItemId = stockItemId, movement.ProductId }, transaction);

                if (!perteneceAlProducto)
                    throw new CustomException("La existencia indicada no pertenece a este producto.");

                var saldo = await db.QueryFirstAsync<(Guid StockItemId, decimal StockBefore, decimal StockAfter)>(
                    "SELECT stock_item_id, stock_before, stock_after FROM fn_mover_stock(@ProductId, @Delta, @UserId, @StockItemId)",
                    new { movement.ProductId, Delta = -(decimal)movement.Quantity, UserId = userId, StockItemId = stockItemId },
                    transaction);

                if (saldo.StockAfter < 0)
                    throw new CustomException("No se puede dar de baja más cantidad de la que hay en esta existencia.");

                movement.Id = Guid.NewGuid();
                movement.StockItemId = saldo.StockItemId;
                movement.Quantity = -movement.Quantity;
                movement.StockBefore = (int)saldo.StockBefore;
                movement.StockAfter = (int)saldo.StockAfter;
                movement.MovementType = "MERMA";
                movement.ReferenceType = "EXPIRY";
                movement.State = true;
                movement.CreatedBy = movement.ModifiedBy = userId;
                movement.Created = movement.Modified = DateTime.UtcNow;

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

    public async Task<WriteOffReportResponse> GetWriteOffs(DateTime desde, DateTime hasta, Guid? productId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            // Rango calculado acá, no en SQL con DATE(created): el usuario elige
            // días del calendario boliviano y `created` guarda instantes UTC, así
            // que hay que convertir. Sin esto el día arrancaba a las 20:00 de la
            // víspera y una merma registrada de noche caía en el reporte del día
            // siguiente. El tope ya era exclusivo, que es lo que espera
            // EndOfDayUtcExclusive.
            var inicio = BusinessTime.StartOfDayUtc(desde);
            var fin = BusinessTime.EndOfDayUtcExclusive(hasta);

            string sqlDetalle = @"
                SELECT product_id, product_code, product_name, lot_code, expiry_date,
                       cantidad, valor_perdido, reason, observation, created, created_by
                  FROM v_mermas
                 WHERE created >= @Inicio AND created < @Fin
                   AND (@ProductId::uuid IS NULL OR product_id = @ProductId)
                 ORDER BY created DESC;
            ";
            var detalle = (await db.QueryAsync<WriteOffDetailResponse>(
                sqlDetalle, new { Inicio = inicio, Fin = fin, ProductId = productId })).ToList();

            string sqlPorProducto = @"
                SELECT product_id, product_code, product_name,
                       sum(cantidad)      AS Unidades,
                       sum(valor_perdido) AS ValorPerdido,
                       count(*)           AS Eventos
                  FROM v_mermas
                 WHERE created >= @Inicio AND created < @Fin
                   AND (@ProductId::uuid IS NULL OR product_id = @ProductId)
                 GROUP BY product_id, product_code, product_name
                 ORDER BY ValorPerdido DESC;
            ";
            var porProducto = (await db.QueryAsync<WriteOffByProductResponse>(
                sqlPorProducto, new { Inicio = inicio, Fin = fin, ProductId = productId })).ToList();

            return new WriteOffReportResponse
            {
                TotalUnidades = detalle.Sum(d => d.Cantidad),
                TotalValorPerdido = detalle.Sum(d => d.ValorPerdido),
                TotalEventos = detalle.Count,
                PorProducto = porProducto,
                Detalle = detalle,
            };
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<WriteOffReportResponse>(ex); }
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
