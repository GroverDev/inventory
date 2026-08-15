using System.Data;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class PurchaseDetailRepository : IPurchaseDetailRepository
{
    public async Task<bool> CreatePurchaseDetail(PurchaseDetail detail, IDbConnection db, IDbTransaction transaction)
    {
        bool ok;
        try
        {
            detail.Id = Guid.NewGuid();
            string sqlQuery = @"
                    INSERT INTO purchases_detail
                        (id,   purchase_id, product_id, order_unit_price, ordered_quantity, order_final_price, state, created_by, created, modified_by, modified, delivery_unit_price, delivered_quantity, delivery_final_price, purchase_status_id)
                    VALUES
                        (@Id, @PurchaseId, @ProductId, @OrderUnitPrice, @OrderedQuantity, @OrderFinalPrice, @State, @CreatedBy,  @Created, @ModifiedBy, @Modified, @DeliveryUnitPrice, @DeliveredQuantity, @DeliveryFinalPrice, @PurchaseStatusId);
                ";

            var result = await db.ExecuteAsync(sqlQuery, detail, transaction: transaction);
            ok = true;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }

        return ok;
    }

    public async Task<bool> UpdatePurchaseDetail(PurchaseDetail detail, IDbConnection db, IDbTransaction transaction)
    {
        bool ok;
        try
        {
            string sqlQuery = @"
                    UPDATE purchases_detail
                       SET modified_by = @ModifiedBy,
                           modified = @Modified,
                           order_unit_price = @OrderUnitPrice,
                           ordered_quantity = @OrderedQuantity,
                           order_final_price = @OrderFinalPrice,
                           purchase_status_id = @PurchaseStatusId
                    WHERE id = @Id and product_id = @ProductId and purchase_id = @PurchaseId;
                ";

            var result = await db.ExecuteAsync(sqlQuery, detail, transaction);
            ok = true;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }

        return ok;
    }

    /// <summary>
    /// Registra una línea de recepción: graba el hecho, mueve el stock, deja el
    /// movimiento auditable y actualiza el acumulado de la línea del pedido.
    /// Todo dentro de la transacción que abre <see cref="PurchaseRepository.ReceiveOrders"/>.
    /// </summary>
    public async Task<bool> ReceiveOrdersDetail(Guid purchaseId, PurchaseDeliveryDetail detail, IDbConnection db, IDbTransaction transaction)
    {
        bool ok;
        try
        {
            detail.Id = Guid.NewGuid();
            string sqlQuery = @"
                    INSERT INTO purchases_delivery_detail
                            (id, purchase_delivery_id, product_id, ordered_quantity, delivery_quantity, unit_price, state, created_by, created, modified_by, modified)
                    VALUES(@Id, @PurchaseDeliveryId, @ProductId, @OrderedQuantity, @DeliveryQuantity, @UnitPrice, @State, @CreatedBy, @Created, @ModifiedBy , @Modified);
                ";

            await db.ExecuteAsync(sqlQuery, detail, transaction);

            // Mueve el stock y obtiene ambos saldos en una sola sentencia. Con un
            // SELECT y un UPDATE separados, otra transacción puede intercalarse
            // entre los dos y dejar registrado un stock_after que nunca existió.
            //
            // Cuando se active el modo 'lot', es acá donde se capturarán el lote y
            // el vencimiento: la recepción es el momento en que entran a la farmacia.
            var stock = await db.QueryFirstAsync<(Guid StockItemId, decimal StockBefore, decimal StockAfter)>(
                "SELECT stock_item_id, stock_before, stock_after FROM fn_mover_stock(@ProductId, @Delta, @UserId)",
                new { detail.ProductId, Delta = (decimal)detail.DeliveryQuantity, UserId = detail.CreatedBy },
                transaction);

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = detail.ProductId,
                StockItemId = stock.StockItemId,
                MovementType = "COMPRA",
                Quantity = detail.DeliveryQuantity,
                StockBefore = (int)stock.StockBefore,
                StockAfter = (int)stock.StockAfter,
                ReferenceId = detail.PurchaseDeliveryId,
                ReferenceType = "PURCHASE",
                State = true,
                CreatedBy = detail.CreatedBy,
                ModifiedBy = detail.CreatedBy,
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

            // Caché denormalizado sobre la línea del pedido. La verdad sigue siendo
            // el log de recepciones; esto solo evita recalcularlo en cada consulta.
            string cacheSql = @"
                UPDATE purchases_detail
                   SET delivered_quantity   = delivered_quantity + @DeliveryQuantity,
                       delivery_unit_price  = @UnitPrice,
                       delivery_final_price = delivery_final_price + (@DeliveryQuantity * @UnitPrice),
                       modified_by          = @ModifiedBy,
                       modified             = @Modified
                 WHERE purchase_id = @PurchaseId
                   AND product_id  = @ProductId
                   AND state;
            ";
            await db.ExecuteAsync(cacheSql, new
            {
                detail.DeliveryQuantity,
                detail.UnitPrice,
                detail.ModifiedBy,
                detail.Modified,
                PurchaseId = purchaseId,
                detail.ProductId
            }, transaction);

            ok = true;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }

        return ok;
    }
}
