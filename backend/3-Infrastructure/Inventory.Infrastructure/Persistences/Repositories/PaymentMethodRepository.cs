using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class PaymentMethodRepository(InventoryDbContext _DbContext) : IPaymentMethodRepository
{
    public async Task<List<PaymentMethod>> GetPaymentMethods()
    {
        List<PaymentMethod> list = [];
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string sqlQuery = @"
                SELECT id, name, icon_css, requires_changes
                  FROM payment_methods
                 WHERE state
                 ORDER BY name;
            ";
            var result = await db.QueryAsync<PaymentMethod>(sqlQuery);
            list = result.ToList();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
        return list;
    }
}
