using Common.Utilities;
using Inventory.Domain;

namespace Inventory.Application;

public interface ICashSessionApplication
{
    Task<Response<string>> OpenSession(OpenCashSessionRequest request, int userId);
    /// <param name="supervisorId">Supervisor que autoriza, ya validado; null si no vino.</param>
    /// <param name="closerIsSupervisor">Quien cierra no es solo cajero: se autoriza a sí mismo.</param>
    Task<Response<CashSessionResponse>> CloseSession(string sessionId, CloseCashSessionRequest request, int userId,
                                                     int? supervisorId = null, bool closerIsSupervisor = false);

    /// <summary>Qué medios se arquean al cierre y cuándo se exige observación.</summary>
    Task<Response<CashCloseSettings>> GetCloseSettings();
    Task<Response<bool>> SaveCloseSettings(CashCloseSettingsRequest request, int userId);
    Task<Response<CashSessionResponse>> GetActiveSession(int userId);
    Task<Response<CashSessionResponse>> GetSessionById(string sessionId);
    Task<Response<List<CashSessionResponse>>> GetSessions(string dateFrom, string dateTo, int userId, string rol);
    Task<Response<List<SaleProductResponse>>> GetSessionSales(string sessionId);
}
