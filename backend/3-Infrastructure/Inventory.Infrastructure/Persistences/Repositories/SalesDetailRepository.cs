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

            Product product = new()
            {
                Id = detail.ProductId,
                CurrentStock = detail.Quantity,
            };
            sqlQuery = @"
                        UPDATE products
                           SET current_stock = (current_stock - @CurrentStock)
                         WHERE id = @Id ;
                    ";
            int numberRows = db.Execute(sqlQuery, product, transaction);
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
