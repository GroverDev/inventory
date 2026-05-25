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
                           delivery_unit_price = @DeliveryUnitPrice, 
                           delivered_quantity = @DeliveredQuantity,
                           delivery_final_price = @DeliveryFinalPrice,
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

    public async Task<bool> ReceiveOrdersDetail(PurchaseDeliveryDetail detail, IDbConnection db, IDbTransaction transaction)
    {
        bool ok;
        try
        {
            string sqlQuery = @"
                    INSERT INTO purchases_delivery_detail
                            (id, purchase_delivery_id, product_id, ordered_quantity, delivery_quantity, state, created_by, created, modified_by, modified)
                    VALUES(@Id, @PurchaseDeliveryId, @ProductId, @OrderedQuantity, @DeliveryQuantity, @State, @CreatedBy, @Created, @ModifiedBy , @Modified);
                ";

            await db.ExecuteAsync(sqlQuery, detail, transaction);

            int stockBefore = await db.ExecuteScalarAsync<int>(
                "SELECT current_stock FROM products WHERE id = @Id",
                new { Id = detail.ProductId }, transaction);

            await db.ExecuteAsync(
                "UPDATE products SET current_stock = current_stock + @DeliveryQuantity WHERE id = @ProductId;",
                new { detail.DeliveryQuantity, detail.ProductId }, transaction);

            int stockAfter = stockBefore + detail.DeliveryQuantity;

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = detail.ProductId,
                MovementType = "COMPRA",
                Quantity = detail.DeliveryQuantity,
                StockBefore = stockBefore,
                StockAfter = stockAfter,
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
                       (id, product_id, movement_type, quantity, stock_before, stock_after,
                        reason, observation, reference_id, reference_type,
                        state, created_by, created, modified_by, modified)
                VALUES (@Id, @ProductId, @MovementType, @Quantity, @StockBefore, @StockAfter,
                        @Reason, @Observation, @ReferenceId, @ReferenceType,
                        @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
            ";
            await db.ExecuteAsync(movSql, movement, transaction);

            ok = true;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }

        return ok;
    }
}
