using System.Data;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class SalePaymentRepository : ISalePaymentRepository
{
    public async Task CreateSalePayments(List<SalePayment> payments, IDbConnection db, IDbTransaction transaction)
    {
        try
        {
            const string sqlQuery = @"
                INSERT INTO sale_payments
                       (id, sale_id, payment_method_id, amount_given, ""amount returned"")
                VALUES (@Id, @SaleId, @PaymentMethodId, @AmountGiven, @AmountReturned);
            ";
            foreach (var payment in payments)
            {
                payment.Id = Guid.NewGuid();
                await db.ExecuteAsync(sqlQuery, payment, transaction);
            }
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }

    public async Task<List<SalePaymentResponse>> GetSalePayments(Guid saleId, IDbConnection db)
    {
        try
        {
            const string sqlQuery = @"
                SELECT sp.id,
                       sp.payment_method_id,
                       pm.name           AS PaymentMethodName,
                       pm.icon_css       AS IconCss,
                       sp.amount_given   AS AmountGiven,
                       sp.""amount returned"" AS AmountReturned
                  FROM sale_payments sp
                  JOIN payment_methods pm ON pm.id = sp.payment_method_id
                 WHERE sp.sale_id = @SaleId;
            ";
            var result = await db.QueryAsync<SalePaymentResponse>(sqlQuery, new { SaleId = saleId });
            return result.ToList();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
    }
}
