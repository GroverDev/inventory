using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface ICashSessionApplication
{
    Task<Response<string>> OpenSession(OpenCashSessionRequest request, int userId);
    Task<Response<CashSessionResponse>> CloseSession(string sessionId, CloseCashSessionRequest request, int userId);
    Task<Response<CashSessionResponse>> GetActiveSession(int userId);
    Task<Response<CashSessionResponse>> GetSessionById(string sessionId);
    Task<Response<List<CashSessionResponse>>> GetSessions(string dateFrom, string dateTo, int userId, string rol);
}
