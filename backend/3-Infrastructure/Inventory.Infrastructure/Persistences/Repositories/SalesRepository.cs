using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class SalesRepository(InventoryDbContext _DbContext, ISalesDetailRepository _salesDetailRepository ): ISalesRepository
{
    public async Task<string> CreateSale(Sale sale)
    {
        using var db = _DbContext.CreateConnection;
        string uuid = string.Empty;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                sale.Id = Guid.NewGuid();
                string sqlQuery = @"
                       INSERT INTO sales
                              (id, customer_id,  sale_date,  subtotal, total_discounts, total,  is_active, state, created_by, created, modified_by, modified)
                       VALUES(@Id, @CustomerId, @SaleDate, @Subtotal, @TotalDiscounts, @Total, @IsActive, @State, @CreatedBy,  @Created, @ModifiedBy, @Modified);
                         ";
                var result = await db.ExecuteAsync(sqlQuery, sale, transaction);
                sale.Detail.ForEach(x => { x.SaleId = sale.Id;});

                foreach (var detail in sale.Detail) 
                {
                    var respOk = await _salesDetailRepository.CreateSaleDetail(detail,db,transaction);
                }
                transaction.Commit();
                uuid = sale.Id.ToString();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally
        {
            db.Close();
        }
        return uuid;
    }

    public async Task<int> UpdateSale(Sale sale)
    {
        using var db = _DbContext.CreateConnection;
        int numberRows = 0;
        try
        {
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                 string sqlQuery = @"
                        UPDATE sales
                           SET sale_date       = @SaleDate,
                               subtotal        = @Subtotal,
                               total_discounts = @TotalDiscounts,
                               total           = @Total,
                               is_active       = @IsActive,
                               modified_by     = @ModifiedBy,
                               modified        = @Modified
                         WHERE id = @Id;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, sale);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }

        return numberRows;
    }

     public async Task<List<SaleProductResponse>> GetSales(DateTime SaleDateInitial, DateTime SaleDateEnd)
    {
        List<SaleProductResponse> listSales = [];
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                       SELECT s.id, s.customer_id, c.full_name AS CustomerName,
                              s.sale_date, s.subtotal, s.total_discounts, s.total, s.is_active
                         FROM sales s
                        INNER JOIN customers c ON c.id = s.customer_id
                        WHERE s.state
                          AND s.sale_date >= @SaleDateInitial
                          AND s.sale_date <= @SaleDateEnd
                        ORDER BY s.sale_date DESC;
                ";

            var result = await db.QueryAsync<SaleProductResponse>(sqlQuery, new { SaleDateInitial = SaleDateInitial, SaleDateEnd=SaleDateEnd });
            listSales = result!.ToList();
            foreach (var saleProduct in listSales)
            {
                var resulDetail = await _salesDetailRepository.GetSalesProductDetail(saleProduct.Id, db);
                saleProduct.Detail = [.. resulDetail!];
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return listSales;
    }

     public async Task<SaleProductResponse> GetSale(Guid Id)
    {
        SaleProductResponse sale = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                    SELECT s.id, s.customer_id, c.full_name AS CustomerName,
                           s.sale_date, s.subtotal, s.total_discounts, s.total, s.is_active
                      FROM sales s
                     INNER JOIN customers c ON c.id = s.customer_id
                     WHERE s.state
                       AND s.id = @Id;
                ";
            sale = await db.QueryFirstOrDefaultAsync<SaleProductResponse>(sqlQuery, new { Id }) ?? new();
            if (sale.Id != Guid.Empty)
            {
                var detail = await _salesDetailRepository.GetSalesProductDetail(sale.Id, db);
                sale.Detail = [.. detail];
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex, ex.messageType); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
        return sale;
    }

    public async Task<int> DeleteSale(Guid id, int idUserModified)
    {
        using var db = _DbContext.CreateConnection;
        int numberRows = 0;
        try
        {
            DateTime fechaActual = DateTime.Now;
            db.Open();
            using var transaction = db.BeginTransaction();
            try
            {
                string sqlQuery = @"
                        UPDATE sales
                           SET state = false,
                               modified_by = @ModifiedBy, 
                               modified = @Modified
                         WHERE id = @Id ;
                    ";
                numberRows = await db.ExecuteAsync(sqlQuery, new { Id = id, ModifiedBy = idUserModified, @Modified = fechaActual });
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception(ex.Message, ex);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return numberRows;
    }

}


