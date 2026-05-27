using Common.Utilities.Exceptions;
using Dapper;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class CashSessionRepository(InventoryDbContext _DbContext, ICashMovementRepository _movementRepository) : ICashSessionRepository
{
    public async Task<Guid> OpenSession(CashSession session)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            session.Id = Guid.NewGuid();
            const string sql = @"
                INSERT INTO cash_sessions
                       (id, user_id, opened_at, opening_amount, state, created_by, created, modified_by, modified)
                VALUES (@Id, @UserId, @OpenedAt, @OpeningAmount, TRUE, @CreatedBy, @Created, @ModifiedBy, @Modified);";
            await db.ExecuteAsync(sql, session);
            return session.Id;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<int> CloseSession(Guid sessionId, decimal declaredAmount, decimal expectedAmount, decimal difference, string notes, int modifiedBy)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                UPDATE cash_sessions
                   SET closed_at       = NOW(),
                       declared_amount = @DeclaredAmount,
                       expected_amount = @ExpectedAmount,
                       difference      = @Difference,
                       notes           = @Notes,
                       modified_by     = @ModifiedBy,
                       modified        = NOW()
                 WHERE id = @SessionId AND closed_at IS NULL AND state = TRUE;";
            return await db.ExecuteAsync(sql, new { SessionId = sessionId, DeclaredAmount = declaredAmount, ExpectedAmount = expectedAmount, Difference = difference, Notes = notes, ModifiedBy = modifiedBy });
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<CashSessionResponse?> GetActiveSessionByUser(int userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                SELECT cs.id, cs.user_id, u.full_name AS UserFullName,
                       cs.opened_at, cs.closed_at,
                       cs.opening_amount, cs.declared_amount, cs.expected_amount, cs.difference, cs.notes,
                       COALESCE((SELECT SUM(s.total) FROM sales s WHERE s.cash_session_id = cs.id AND s.state), 0) AS TotalSales,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'expense' AND m.state), 0) AS TotalExpenses,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'withdrawal' AND m.state), 0) AS TotalWithdrawals,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'income' AND m.state), 0) AS TotalIncome
                  FROM cash_sessions cs
                  JOIN sec.users u ON u.id = cs.user_id
                 WHERE cs.user_id = @UserId AND cs.closed_at IS NULL AND cs.state = TRUE;";
            var session = await db.QueryFirstOrDefaultAsync<CashSessionResponse>(sql, new { UserId = userId });
            if (session != null)
                session.Movements = await _movementRepository.GetMovementsBySession(session.Id);
            return session;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<CashSessionResponse?> GetSessionById(Guid sessionId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            const string sql = @"
                SELECT cs.id, cs.user_id, u.full_name AS UserFullName,
                       cs.opened_at, cs.closed_at,
                       cs.opening_amount, cs.declared_amount, cs.expected_amount, cs.difference, cs.notes,
                       COALESCE((SELECT SUM(s.total) FROM sales s WHERE s.cash_session_id = cs.id AND s.state), 0) AS TotalSales,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'expense' AND m.state), 0) AS TotalExpenses,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'withdrawal' AND m.state), 0) AS TotalWithdrawals,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'income' AND m.state), 0) AS TotalIncome
                  FROM cash_sessions cs
                  JOIN sec.users u ON u.id = cs.user_id
                 WHERE cs.id = @SessionId AND cs.state = TRUE;";
            var session = await db.QueryFirstOrDefaultAsync<CashSessionResponse>(sql, new { SessionId = sessionId });
            if (session != null)
                session.Movements = await _movementRepository.GetMovementsBySession(session.Id);
            return session;
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }

    public async Task<List<CashSessionResponse>> GetSessions(DateTime dateFrom, DateTime dateTo, int? userId)
    {
        using var db = _DbContext.CreateConnection;
        try
        {
            db.Open();
            string userFilter = userId.HasValue ? "AND cs.user_id = @UserId" : "";
            string sql = $@"
                SELECT cs.id, cs.user_id, u.full_name AS UserFullName,
                       cs.opened_at, cs.closed_at,
                       cs.opening_amount, cs.declared_amount, cs.expected_amount, cs.difference, cs.notes,
                       COALESCE((SELECT SUM(s.total) FROM sales s WHERE s.cash_session_id = cs.id AND s.state), 0) AS TotalSales,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'expense' AND m.state), 0) AS TotalExpenses,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'withdrawal' AND m.state), 0) AS TotalWithdrawals,
                       COALESCE((SELECT SUM(m.amount) FROM cash_movements m WHERE m.cash_session_id = cs.id AND m.movement_type = 'income' AND m.state), 0) AS TotalIncome
                  FROM cash_sessions cs
                  JOIN sec.users u ON u.id = cs.user_id
                 WHERE cs.state = TRUE
                   AND cs.opened_at >= @DateFrom
                   AND cs.opened_at <= @DateTo
                   {userFilter}
                 ORDER BY cs.opened_at DESC;";
            var result = await db.QueryAsync<CashSessionResponse>(sql, new { DateFrom = dateFrom, DateTo = dateTo, UserId = userId });
            return result.ToList();
        }
        catch (CustomException ex) { throw new CustomException(ex.Message, ex); }
        catch (Exception ex) { throw new Exception(ex.Message, ex); }
        finally { db.Close(); }
    }
}
