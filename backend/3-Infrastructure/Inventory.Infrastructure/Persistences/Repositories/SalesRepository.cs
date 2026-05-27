using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class SalesRepository(InventoryDbContext _DbContext, ISalesDetailRepository _salesDetailRepository, ISalePaymentRepository _salePaymentRepository, ISaleReturnRepository _saleReturnRepository): ISalesRepository
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
                              (id, customer_id,  sale_date,  subtotal, total_discounts, total,  is_active, cash_session_id, state, created_by, created, modified_by, modified)
                       VALUES(@Id, @CustomerId, @SaleDate, @Subtotal, @TotalDiscounts, @Total, @IsActive, @CashSessionId,  @State, @CreatedBy,  @Created, @ModifiedBy, @Modified);
                         ";
                await db.ExecuteAsync(sqlQuery, sale, transaction);

                sale.Detail.ForEach(x => x.SaleId = sale.Id);
                foreach (var detail in sale.Detail)
                    await _salesDetailRepository.CreateSaleDetail(detail, db, transaction);

                sale.Payments.ForEach(p => p.SaleId = sale.Id);
                await _salePaymentRepository.CreateSalePayments(sale.Payments, db, transaction);

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

     public async Task<List<SaleProductResponse>> GetSales(DateTime SaleDateInitial, DateTime SaleDateEnd, int? userId = null)
    {
        List<SaleProductResponse> listSales = [];
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string userFilter = userId.HasValue ? "AND s.created_by = @UserId" : "";
            string sqlQuery = $@"
                       SELECT s.id, s.customer_id, c.full_name AS CustomerName,
                              s.sale_date, s.subtotal, s.total_discounts, s.total, s.is_active,
                              COALESCE(u.full_name, '') AS SellerName
                         FROM sales s
                        INNER JOIN customers c ON c.id = s.customer_id
                        LEFT  JOIN sec.users u ON u.id = s.created_by
                        WHERE s.state
                          AND s.sale_date >= @SaleDateInitial
                          AND s.sale_date <= @SaleDateEnd
                          {userFilter}
                        ORDER BY s.sale_date DESC;
                ";

            var result = await db.QueryAsync<SaleProductResponse>(sqlQuery, new { SaleDateInitial, SaleDateEnd, UserId = userId });
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
                var payments = await _salePaymentRepository.GetSalePayments(sale.Id, db);
                sale.Payments = [.. payments];
                var returns = await _saleReturnRepository.GetReturnsBySale(sale.Id, db);
                sale.Returns = [.. returns];
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


