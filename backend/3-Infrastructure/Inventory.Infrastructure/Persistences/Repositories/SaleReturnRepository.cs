using System.Data;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class SaleReturnRepository(InventoryDbContext _DbContext) : ISaleReturnRepository
{
    public async Task<string> CreateReturn(SaleReturn saleReturn)
    {
        using var db = _DbContext.CreateConnection;
        string uuid = string.Empty;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                // Insertar cabecera de devolución
                saleReturn.Id = Guid.NewGuid();
                string sqlReturn = @"
                    INSERT INTO sale_returns
                           (id, sale_id, return_date, reason, total_returned, is_full_return,
                            state, created_by, created, modified_by, modified)
                    VALUES (@Id, @SaleId, @ReturnDate, @Reason, @TotalReturned, @IsFullReturn,
                            @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
                ";
                await db.ExecuteAsync(sqlReturn, saleReturn, transaction);

                // Por cada línea: insertar detalle + restaurar stock + registrar movimiento
                foreach (var det in saleReturn.Detail)
                {
                    det.Id = Guid.NewGuid();
                    det.ReturnId = saleReturn.Id;

                    string sqlDetail = @"
                        INSERT INTO sale_return_detail
                               (id, return_id, sale_detail_id, product_id,
                                quantity_returned, unit_price, line_total,
                                state, created_by, created, modified_by, modified)
                        VALUES (@Id, @ReturnId, @SaleDetailId, @ProductId,
                                @QuantityReturned, @UnitPrice, @LineTotal,
                                @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
                    ";
                    await db.ExecuteAsync(sqlDetail, det, transaction);

                    // Restaurar stock en la MISMA existencia de la que salió: con
                    // lotes, la del lote vendido. Hay que pasarla explícitamente
                    // porque fn_mover_stock, sin existencia indicada, usa la que no
                    // tiene lote; el producto devuelto perdía su lote y su
                    // vencimiento, que es justo lo que los lotes vienen a resolver.
                    // El dato siempre está: sales_detail.stock_item_id es NOT NULL.
                    var stockItemId = await db.QueryFirstOrDefaultAsync<Guid?>(
                        "SELECT stock_item_id FROM sales_detail WHERE id = @SaleDetailId AND state",
                        new { det.SaleDetailId }, transaction);

                    if (stockItemId is null || stockItemId == Guid.Empty)
                        throw new CustomException(
                            "No se pudo identificar de qué existencia salió la línea que se devuelve.");

                    var stock = await db.QueryFirstAsync<(Guid StockItemId, decimal StockBefore, decimal StockAfter)>(
                        "SELECT stock_item_id, stock_before, stock_after FROM fn_mover_stock(@ProductId, @Delta, @UserId, @Item)",
                        new
                        {
                            det.ProductId,
                            Delta = (decimal)det.QuantityReturned,
                            UserId = saleReturn.CreatedBy,
                            Item = stockItemId
                        },
                        transaction);

                    // Registrar movimiento de stock
                    var movement = new StockMovement
                    {
                        Id = Guid.NewGuid(),
                        ProductId = det.ProductId,
                        StockItemId = stock.StockItemId,
                        MovementType = "DEVOLUCION",
                        Quantity = det.QuantityReturned,
                        StockBefore = (int)stock.StockBefore,
                        StockAfter = (int)stock.StockAfter,
                        ReferenceId = saleReturn.Id,
                        ReferenceType = "RETURN",
                        Reason = saleReturn.Reason,
                        State = true,
                        CreatedBy = saleReturn.CreatedBy,
                        ModifiedBy = saleReturn.CreatedBy,
                        Created = DateTime.Now,
                        Modified = DateTime.Now,
                    };

                    string movSql = @"
                        INSERT INTO stock_movements
                               (id, product_id, stock_item_id, movement_type, quantity, stock_before, stock_after,
                                reason, observation, reference_id, reference_type,
                                state, created_by, created, modified_by, modified)
                        VALUES (@Id, @ProductId, @StockItemId, @MovementType, @Quantity, @StockBefore, @StockAfter,
                                @Reason, @Observation, @ReferenceId, @ReferenceType,
                                @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
                    ";
                    await db.ExecuteAsync(movSql, movement, transaction);
                }

                // Si es devolución total, marcar la venta como inactiva
                if (saleReturn.IsFullReturn)
                {
                    await db.ExecuteAsync(
                        "UPDATE sales SET is_active = FALSE, modified_by = @UserId, modified = NOW() WHERE id = @SaleId",
                        new { UserId = saleReturn.CreatedBy, SaleId = saleReturn.SaleId }, transaction);
                }

                transaction.Commit();
                uuid = saleReturn.Id.ToString();
            }
            catch (CustomException ex) { transaction.Rollback(); throw new CustomException(ex.Message, ex); }
            catch (Exception ex) { transaction.Rollback(); throw new Exception(ex.Message, ex); }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<string>(ex); }
        finally { db.Close(); }
        return uuid;
    }

    public async Task<List<SaleReturnResponse>> GetReturnsBySale(Guid saleId, IDbConnection db)
    {
        List<SaleReturnResponse> returns = [];
        try
        {
            string sqlReturns = @"
                SELECT id, sale_id, return_date, reason, total_returned, is_full_return
                  FROM sale_returns
                 WHERE sale_id = @SaleId AND state
                 ORDER BY return_date DESC;
            ";
            var rows = await db.QueryAsync<SaleReturnResponse>(sqlReturns, new { SaleId = saleId });
            returns = [.. rows];

            foreach (var ret in returns)
            {
                string sqlDetail = @"
                    SELECT srd.id, srd.sale_detail_id, srd.product_id,
                           p.product_name AS ProductName,
                           srd.quantity_returned, srd.unit_price, srd.line_total
                      FROM sale_return_detail srd
                           INNER JOIN products p ON p.id = srd.product_id
                     WHERE srd.return_id = @ReturnId AND srd.state;
                ";
                var detail = await db.QueryAsync<SaleReturnDetailResponse>(sqlDetail, new { ReturnId = ret.Id });
                ret.Detail = [.. detail];
            }
        }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        return returns;
    }
}
