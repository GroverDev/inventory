using System.Data;
using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class SalesDetailRepository : ISalesDetailRepository
{
    public async Task<bool> CreateSaleDetail(SaleDetail detail, IDbConnection db, IDbTransaction transaction)
    {
        bool ok;
        try
        {
            detail.Id = Guid.NewGuid();

            // Mover el stock va primero: sales_detail.stock_item_id es NOT NULL, así
            // que la línea no se puede grabar sin saber de qué existencia sale.
            // Un solo lugar mueve el stock: actualiza la existencia y la caché de
            // products a la vez, y devuelve el saldo antes y después.
            var mov = await db.QueryFirstAsync<(Guid StockItemId, decimal StockBefore, decimal StockAfter)>(
                "SELECT stock_item_id, stock_before, stock_after FROM fn_mover_stock(@ProductId, @Delta, @UserId)",
                new { detail.ProductId, Delta = -(decimal)detail.Quantity, UserId = detail.CreatedBy },
                transaction);

            detail.StockItemId = mov.StockItemId;

            string sqlQuery = @"
                    INSERT INTO sales_detail
                               (id,   sale_id, product_id, stock_item_id, quantity, unit_price, line_subtotal, line_total_discounts, line_total, discount_id, state, created_by, created, modified_by, modified)
                    VALUES
                            (@Id, @SaleId, @ProductId, @StockItemId, @Quantity, @UnitPrice, @LineSubtotal, @LineTotalDiscounts, @LineTotal, @DiscountId, @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
                ";
            var result = await db.ExecuteAsync(sqlQuery, detail, transaction: transaction);

            if (detail.DiscountId.HasValue && detail.DiscountId != Guid.Empty)
            {
                string discountTrackSql = @"
                    INSERT INTO sale_detail_discounts
                                (id, sale_detail_id, discount_id, applied_amount, state, created_by, created, modified_by, modified)
                    VALUES      (@Id, @SaleDetailId, @DiscountId, @AppliedAmount, true, @CreatedBy, @Created, @CreatedBy, @Created);
                ";
                await db.ExecuteAsync(discountTrackSql, new
                {
                    Id           = Guid.NewGuid(),
                    SaleDetailId = detail.Id,
                    detail.DiscountId,
                    AppliedAmount = detail.LineTotalDiscounts,
                    detail.CreatedBy,
                    Created = DateTime.Now
                }, transaction);
            }

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = detail.ProductId,
                StockItemId = mov.StockItemId,
                MovementType = "VENTA",
                Quantity = -detail.Quantity,
                StockBefore = (int)mov.StockBefore,
                StockAfter = (int)mov.StockAfter,
                ReferenceId = detail.SaleId,
                ReferenceType = "SALE",
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
            ok = true;


        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        return ok;
    }

    public async Task<List<SaleProductDetailResponse>> GetSalesProductDetail(Guid idSale, IDbConnection db)
    {
        List<SaleProductDetailResponse> listDetails = [];
        try
        {

            string sqlQuery = @" SELECT sd.id,
                                        sd.sale_id,
                                        sd.product_id,
                                        p.product_name,
                                        sd.quantity,
                                        sd.unit_price,
                                        sd.line_subtotal,
                                        sd.line_total_discounts,
                                        sd.line_total
                                    FROM sales_detail sd
                                         INNER JOIN products p
                                         ON p.id = sd.product_id
                                   WHERE sd.sale_id = @sale_id";
            var result = await db.QueryAsync<SaleProductDetailResponse>(sqlQuery, new { sale_id = idSale });
            listDetails = result.ToList();


        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        return listDetails;
    }
}
