using Common.Utilities;
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
                SELECT id, name, icon_css, requires_changes, affects_cash, requires_count
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

    public async Task SetRequiresCount(List<(Guid Id, bool RequiresCount)> methods, int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            // El efectivo se arquea siempre: no se deja apagar.
            await db.ExecuteAsync(@"
                UPDATE payment_methods
                   SET requires_count = @RequiresCount OR affects_cash,
                       modified_by = @UserId, modified = now()
                 WHERE id = @Id AND state",
                methods.Select(m => new { m.Id, m.RequiresCount, UserId = userId }));
        }
        catch (Exception ex) { throw ExceptionHandler.HandleException<bool>(ex); }
        finally { db.Close(); }
    }
}
