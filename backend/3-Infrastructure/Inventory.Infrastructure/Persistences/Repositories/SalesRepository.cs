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
                // La caja tiene que ser de la sucursal donde se vende. La base lo
                // exige igual (FK compuesta sales_caja_misma_sucursal); esto es
                // para que el mensaje lo diga. Pasa si el punto de venta quedó
                // abierto con la caja de la sucursal anterior.
                if (sale.CashSessionId.HasValue)
                {
                    bool mismaSucursal = await db.ExecuteScalarAsync<bool>(
                        "SELECT EXISTS (SELECT 1 FROM cash_sessions WHERE id = @Id AND branch_id = public.current_branch())",
                        new { Id = sale.CashSessionId }, transaction);

                    if (!mismaSucursal)
                        throw new CustomException(
                            "La caja con la que se intenta vender es de otra sucursal. Recargue el punto de venta y abra o elija la caja de esta sucursal.");
                }

                sale.Id = Guid.NewGuid();
                string sqlQuery = @"
                       INSERT INTO sales
                              (id, customer_id, sale_date, subtotal, total_discounts, total, is_active, cash_session_id,
                               header_discount_id, header_discount_amount,
                               state, created_by, created, modified_by, modified)
                       VALUES(@Id, @CustomerId, @SaleDate, @Subtotal, @TotalDiscounts, @Total, @IsActive, @CashSessionId,
                              @HeaderDiscountId, @HeaderDiscountAmount,
                              @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
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
            // Se relanza el error original, sin envolverlo: envuelto en un
            // Exception genérico, ExceptionHandler ya no reconoce el RAISE de
            // Postgres ("Stock insuficiente en esta sucursal...") y el cajero
            // recibe "Ocurrió un error" en vez del motivo.
            catch
            {
                transaction.Rollback();
                throw;
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

    public async Task<SalesPagedResponse> GetSales(DateTime saleDateInitial, DateTime saleDateEnd, int? userId = null, int page = 1, int pageSize = 50, string? sellerName = null, Guid[]? branches = null)
    {
        SalesPagedResponse result = new();
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string userFilter   = userId.HasValue                   ? "AND s.created_by = @UserId"    : "";
            string sellerFilter = !string.IsNullOrEmpty(sellerName) ? "AND u.full_name = @SellerName" : "";
            int offset = (page - 1) * pageSize;

            string sqlQuery = $@"
                SELECT s.id, s.customer_id, c.full_name AS CustomerName,
                       s.sale_date, s.subtotal, s.total_discounts,
                       COALESCE(s.header_discount_amount, 0) AS HeaderDiscountAmount,
                       s.total, s.is_active,
                       s.total_returned, s.net_total, s.sale_status,
                       COALESCE(u.full_name, '') AS SellerName,
                       b.name                    AS BranchName,
                       (SELECT string_agg(DISTINCT pm.name, ' + ' ORDER BY pm.name)
                          FROM sale_payments sp
                          JOIN payment_methods pm ON pm.id = sp.payment_method_id
                         WHERE sp.sale_id = s.id)  AS PaymentMethodsLabel,
                       COUNT(*)                OVER() AS TotalCount,
                       SUM(s.subtotal)         OVER() AS PeriodSubtotal,
                       SUM(s.total_discounts)  OVER() AS PeriodDiscounts,
                       SUM(s.total)            OVER() AS PeriodTotal,
                       SUM(s.total_returned)   OVER() AS PeriodReturned,
                       SUM(s.net_total)        OVER() AS PeriodNet
                  -- v_sales_net y no sales: trae lo devuelto por venta, para que
                  -- el listado y sus KPIs no muestren importes brutos.
                  FROM v_sales_net s
                 INNER JOIN customers c ON c.id = s.customer_id
                 INNER JOIN branches b  ON b.id = s.branch_id
                 LEFT  JOIN sec.users u ON u.id = s.created_by
                 WHERE s.state
                   -- Sin alcance, la sucursal activa; los reportes pueden pedir
                   -- otra o varias (ya validadas contra las del usuario).
                   AND s.branch_id = ANY(COALESCE(@Branches::uuid[], ARRAY[public.current_branch()]))
                   AND s.sale_date >= @SaleDateInitial
                   AND s.sale_date <  @SaleDateEnd
                   {userFilter}
                   {sellerFilter}
                 ORDER BY s.sale_date DESC
                 LIMIT @PageSize OFFSET @Offset;
            ";

            var rows = (await db.QueryAsync<SalePageRow>(sqlQuery, new
            {
                SaleDateInitial = saleDateInitial,
                SaleDateEnd     = saleDateEnd,
                UserId          = userId,
                SellerName      = sellerName,
                Branches        = branches,
                PageSize        = pageSize,
                Offset          = offset,
            })).ToList();

            if (rows.Count > 0)
            {
                result.TotalCount      = rows[0].TotalCount;
                result.PeriodSubtotal  = rows[0].PeriodSubtotal;
                result.PeriodDiscounts = rows[0].PeriodDiscounts;
                result.PeriodTotal     = rows[0].PeriodTotal;
                result.PeriodReturned  = rows[0].PeriodReturned;
                result.PeriodNet       = rows[0].PeriodNet;
            }

            // Por medio de pago, sobre el período completo con los mismos filtros
            // del listado, no sobre la página visible.
            string ventasDelPeriodo = $@"
                SELECT s.id, 1 AS grupo
                  FROM v_sales_net s
                  LEFT JOIN sec.users u ON u.id = s.created_by
                 WHERE s.state
                   AND s.branch_id = ANY(COALESCE(@Branches::uuid[], ARRAY[public.current_branch()]))
                   AND s.sale_date >= @SaleDateInitial
                   AND s.sale_date <  @SaleDateEnd
                   {userFilter}
                   {sellerFilter}";
            const string devoluciones = @"
                SELECT v.grupo, sr.payment_method_id, 0::numeric, sum(sr.total_returned)
                  FROM sale_returns sr
                  JOIN ventas v ON v.id = sr.sale_id
                 WHERE sr.state
                 GROUP BY v.grupo, sr.payment_method_id";
            result.PeriodByPaymentMethod = (await db.QueryAsync<PaymentMethodTotal>(
                PaymentBreakdownSql.Build(ventasDelPeriodo, devoluciones), new
                {
                    SaleDateInitial = saleDateInitial,
                    SaleDateEnd     = saleDateEnd,
                    UserId          = userId,
                    SellerName      = sellerName,
                    Branches        = branches,
                })).ToList();

            foreach (var row in rows)
            {
                var detail = await _salesDetailRepository.GetSalesProductDetail(row.Id, db);
                row.Detail = [.. detail];
                result.Items.Add(row);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }

        return result;
    }

    private class SalePageRow : SaleProductResponse
    {
        public int TotalCount { get; set; }
        public decimal PeriodSubtotal { get; set; }
        public decimal PeriodDiscounts { get; set; }
        public decimal PeriodTotal { get; set; }
        public decimal PeriodReturned { get; set; }
        public decimal PeriodNet { get; set; }
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
                           s.sale_date, s.subtotal, s.total_discounts,
                           COALESCE(s.header_discount_amount, 0) AS HeaderDiscountAmount,
                           s.total, s.is_active,
                           s.total_returned, s.net_total, s.sale_status
                      FROM v_sales_net s
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
            DateTime fechaActual = DateTime.UtcNow;
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


