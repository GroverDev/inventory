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
            string sqlQuery = @"
                    INSERT INTO sales_detail
                               (id,   sale_id, product_id, quantity, unit_price, line_subtotal, line_total_discounts, line_total, state, created_by, created, modified_by, modified)
                    VALUES
                            (@Id, @SaleId, @ProductId,  @Quantity, @UnitPrice, @LineSubtotal,@LineTotalDiscounts, @LineTotal,  @State, @CreatedBy,  @Created, @ModifiedBy, @Modified);
                ";
            var result = await db.ExecuteAsync(sqlQuery, detail, transaction: transaction);

            int stockBefore = await db.ExecuteScalarAsync<int>(
                "SELECT current_stock FROM products WHERE id = @Id",
                new { Id = detail.ProductId }, transaction);

            sqlQuery = @"
                        UPDATE products
                           SET current_stock = (current_stock - @Quantity)
                         WHERE id = @ProductId;
                    ";
            await db.ExecuteAsync(sqlQuery, new { detail.Quantity, detail.ProductId }, transaction);

            int stockAfter = stockBefore - detail.Quantity;

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = detail.ProductId,
                MovementType = "VENTA",
                Quantity = -detail.Quantity,
                StockBefore = stockBefore,
                StockAfter = stockAfter,
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
