using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class CashSessionApplication(
    ICashSessionRepository _cashSessionRepository,
    ICashMovementRepository _cashMovementRepository) : ICashSessionApplication
{

    public async Task<Response<string>> OpenSession(OpenCashSessionRequest request, int userId)
    {
        Response<string> resp = new();
        try
        {
            var existing = await _cashSessionRepository.GetActiveSessionByUser(userId);
            if (existing != null)
                throw new CustomException("Ya tienes una caja abierta. Ciérrala antes de abrir una nueva.");

            if (request.OpeningAmount < 0)
                throw new CustomException("El monto de apertura no puede ser negativo.");

            var session = new CashSession
            {
                UserId = userId,
                OpenedAt = DateTime.Now,
                OpeningAmount = request.OpeningAmount
            };
            AuditHelper.SetCreated(session, userId);

            var id = await _cashSessionRepository.OpenSession(session);
            resp.Data = id.ToString();
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Error al abrir la caja.", ex); }
        return resp;
    }

    public async Task<Response<CashSessionResponse>> CloseSession(string sessionId, CloseCashSessionRequest request, int userId)
    {
        Response<CashSessionResponse> resp = new();
        try
        {
            if (!Guid.TryParse(sessionId, out Guid id))
                throw new CustomException("ID de sesión inválido.");

            if (request.DeclaredAmount < 0)
                throw new CustomException("El monto declarado no puede ser negativo.");

            var session = await _cashSessionRepository.GetSessionById(id);
            if (session == null || session.ClosedAt != null)
                throw new CustomException("La sesión no existe o ya está cerrada.");

            if (session.UserId != userId)
                throw new CustomException("No tienes permiso para cerrar esta sesión.");

            // Calcular el monto esperado:
            // Fondo inicial + ventas en efectivo - gastos - retiros + ingresos extra
            decimal expectedAmount = session.OpeningAmount
                + session.TotalSales
                - session.TotalExpenses
                - session.TotalWithdrawals
                + session.TotalIncome;

            decimal difference = request.DeclaredAmount - expectedAmount;

            await _cashSessionRepository.CloseSession(id, request.DeclaredAmount, expectedAmount, difference, request.Notes, userId);

            resp.Data = await _cashSessionRepository.GetSessionById(id);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Error al cerrar la caja.", ex); }
        return resp;
    }

    public async Task<Response<CashSessionResponse>> GetActiveSession(int userId)
    {
        Response<CashSessionResponse> resp = new();
        try
        {
            resp.Data = await _cashSessionRepository.GetActiveSessionByUser(userId);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Error al consultar la sesión activa.", ex); }
        return resp;
    }

    public async Task<Response<CashSessionResponse>> GetSessionById(string sessionId)
    {
        Response<CashSessionResponse> resp = new();
        try
        {
            if (!Guid.TryParse(sessionId, out Guid id))
                throw new CustomException("ID de sesión inválido.");

            resp.Data = await _cashSessionRepository.GetSessionById(id);
            resp.ok = resp.Data != null;
            if (!resp.ok) resp.SetMessage(MessageTypes.Warning, "Sesión no encontrada.");
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Error al consultar la sesión.", ex); }
        return resp;
    }

    public async Task<Response<List<CashSessionResponse>>> GetSessions(string dateFrom, string dateTo, int userId, string rol)
    {
        Response<List<CashSessionResponse>> resp = new() { Data = [] };
        try
        {
            string dateFromFull = dateFrom + " 00:00:01";
            string dateToFull = dateTo + " 23:59:59";

            if (!DateTime.TryParse(dateFromFull, out DateTime from))
                throw new CustomException("Fecha desde inválida.", MessageTypes.Warning);
            if (!DateTime.TryParse(dateToFull, out DateTime to))
                throw new CustomException("Fecha hasta inválida.", MessageTypes.Warning);
            if (from > to)
                throw new CustomException("La fecha desde no puede ser mayor a la fecha hasta.", MessageTypes.Warning);

            // Cajero solo ve sus propias sesiones, y solo si Cajero es su único rol.
            int? filterUserId = Common.Utilities.Comun.Bases.RolePolicy.VeSoloLoPropio(rol) ? userId : null;

            resp.Data = await _cashSessionRepository.GetSessions(from, to, filterUserId);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Error al listar las sesiones.", ex); }
        return resp;
    }

    public async Task<Response<List<SaleProductResponse>>> GetSessionSales(string sessionId)
    {
        Response<List<SaleProductResponse>> resp = new() { Data = [] };
        try
        {
            if (!Guid.TryParse(sessionId, out Guid id))
                throw new CustomException("ID de sesión inválido.");

            resp.Data = await _cashSessionRepository.GetSessionSales(id);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Error al obtener las ventas de la sesión.", ex); }
        return resp;
    }
}
