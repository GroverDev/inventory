using Common.Utilities;
using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class DiscountRepository(InventoryDbContext _DbContext) : IDiscountRepository
{
    public async Task<List<DiscountResponse>> GetDiscounts()
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                SELECT id, name, type, value, description, is_active
                  FROM discounts
                 WHERE state = true
                 ORDER BY name;
            ";
            var result = await db.QueryAsync<DiscountResponse>(sql);
            return result.ToList();
        }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<DiscountResponse?> GetDiscount(Guid id)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                SELECT id, name, type, value, description, is_active
                  FROM discounts
                 WHERE state = true AND id = @Id;
            ";
            return await db.QueryFirstOrDefaultAsync<DiscountResponse>(sql, new { Id = id });
        }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<string> CreateDiscount(Discount discount)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            discount.Id = Guid.NewGuid();
            const string sql = @"
                INSERT INTO discounts (id, name, type, value, description, is_active, state, created_by, created, modified_by, modified)
                VALUES (@Id, @Name, @Type, @Value, @Description, @IsActive, @State, @CreatedBy, @Created, @ModifiedBy, @Modified);
            ";
            await db.ExecuteAsync(sql, discount);
            return discount.Id.ToString();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<string>(ex); }
        finally { db.Close(); }
    }

    public async Task<int> UpdateDiscount(Discount discount)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                UPDATE discounts
                   SET name        = @Name,
                       type        = @Type,
                       value       = @Value,
                       description = @Description,
                       is_active   = @IsActive,
                       modified_by = @ModifiedBy,
                       modified    = @Modified
                 WHERE id = @Id AND state = true;
            ";
            return await db.ExecuteAsync(sql, discount);
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }
    }

    public async Task<int> DeleteDiscount(Guid id, int modifiedBy)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                UPDATE discounts
                   SET state = false, modified_by = @ModifiedBy, modified = @Modified
                 WHERE id = @Id;
            ";
            return await db.ExecuteAsync(sql, new { Id = id, ModifiedBy = modifiedBy, Modified = DateTime.UtcNow });
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw ExceptionHandler.HandleException<int>(ex); }
        finally { db.Close(); }
    }
}
