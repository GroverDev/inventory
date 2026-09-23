using Inventory.Domain;

namespace Inventory.Infrastructure;

public interface ICashSessionRepository
{
    Task<Guid> OpenSession(CashSession session);
    Task<int> CloseSession(Guid sessionId, decimal declaredAmount, decimal expectedAmount, decimal difference, string notes, int modifiedBy);

    /// <summary>
    /// Cierra la sesión y guarda el arqueo por medio en una sola transacción.
    /// Devuelve 0 si ya estaba cerrada.
    /// </summary>
    Task<int> CloseSessionWithCounts(Guid sessionId, decimal declaredAmount, decimal expectedAmount, decimal difference,
                                     string notes, int modifiedBy, List<CashCountResponse> counts,
                                     int? authorizedBy, List<DenominationCount> denominations);

    /// <summary>Suma un intento de cierre rechazado por diferencia sin observación.</summary>
    Task IncrementCloseAttempts(Guid sessionId);

    /// <summary>Configuración del cierre, sin los medios (los valores por defecto si no se configuró).</summary>
    Task<CashCloseSettings> GetCloseSettings();
    Task SaveCloseSettings(CashCloseSettingsRequest settings, int userId);
    Task<CashSessionResponse?> GetActiveSessionByUser(int userId);
    Task<CashSessionResponse?> GetSessionById(Guid sessionId);
    Task<List<CashSessionResponse>> GetSessions(DateTime dateFrom, DateTime dateTo, int? userId);
    Task<List<SaleProductResponse>> GetSessionSales(Guid sessionId);
}
