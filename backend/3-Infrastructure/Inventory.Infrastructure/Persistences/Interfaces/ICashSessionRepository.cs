using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface ICashSessionRepository
{
    Task<Guid> OpenSession(CashSession session);
    Task<int> CloseSession(Guid sessionId, decimal declaredAmount, decimal expectedAmount, decimal difference, string notes, int modifiedBy);
    Task<CashSessionResponse?> GetActiveSessionByUser(int userId);
    Task<CashSessionResponse?> GetSessionById(Guid sessionId);
    Task<List<CashSessionResponse>> GetSessions(DateTime dateFrom, DateTime dateTo, int? userId);
}
