using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class CashMovementRepository(InventoryDbContext _DbContext) : ICashMovementRepository
{
    public async Task<Guid> CreateMovement(CashMovement movement)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            movement.Id = Guid.NewGuid();
            const string sql = @"
                INSERT INTO cash_movements
                       (id, cash_session_id, movement_type, amount, description, state, created_by, created, modified_by, modified)
                VALUES (@Id, @CashSessionId, @MovementType, @Amount, @Description, TRUE, @CreatedBy, @Created, @ModifiedBy, @Modified);";
            await db.ExecuteAsync(sql, movement);
            return movement.Id;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<List<CashMovementResponse>> GetMovementsBySession(Guid sessionId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                SELECT id, cash_session_id AS CashSessionId, movement_type AS MovementType,
                       amount, description, created
                  FROM cash_movements
                 WHERE cash_session_id = @SessionId AND state = TRUE
                 ORDER BY created ASC;";
            var result = await db.QueryAsync<CashMovementResponse>(sql, new { SessionId = sessionId });
            return result.ToList();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }
}
