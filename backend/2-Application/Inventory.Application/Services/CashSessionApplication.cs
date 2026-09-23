using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class CashSessionApplication(
    ICashSessionRepository _cashSessionRepository,
    ICashMovementRepository _cashMovementRepository,
    IPaymentMethodRepository _paymentMethodRepository) : ICashSessionApplication
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
                OpenedAt = DateTime.UtcNow,
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

    public async Task<Response<CashSessionResponse>> CloseSession(string sessionId, CloseCashSessionRequest request, int userId,
                                                               int? supervisorId = null, bool closerIsSupervisor = false)
    {
        Response<CashSessionResponse> resp = new();
        try
        {
            if (!Guid.TryParse(sessionId, out Guid id))
                throw new CustomException("ID de sesión inválido.");

            var session = await _cashSessionRepository.GetSessionById(id);
            if (session == null || session.ClosedAt != null)
                throw new CustomException("La sesión no existe o ya está cerrada.");

            if (session.UserId != userId)
                throw new CustomException("No tienes permiso para cerrar esta sesión.");

            // Calcular el monto esperado:
            // Fondo inicial + ventas en efectivo - gastos - retiros + ingresos extra.
            // "En efectivo" es TotalCashSales, no TotalSales: una venta cobrada por QR o
            // tarjeta no deja plata en el cajón, y sumarla dejaba al cajero con un
            // faltante que no era suyo.
            decimal expectedAmount = session.OpeningAmount
                + session.TotalCashSales
                - session.TotalExpenses
                - session.TotalWithdrawals
                + session.TotalIncome
                - session.TotalReturns;

            // Arqueo por medio de pago: lo declarado a ciegas contra lo esperado.
            var metodos = await _paymentMethodRepository.GetPaymentMethods();
            var arqueo = CashCountRules.Compute(metodos, session.ByPaymentMethod, expectedAmount,
                                                request.Counts, request.DeclaredAmount);

            var reglas = await _cashSessionRepository.GetCloseSettings();

            // Las columnas del turno siguen siendo las del efectivo (el cajón).
            var efectivo = arqueo.FirstOrDefault(a => metodos.Any(m => m.Id == a.PaymentMethodId && m.AffectsCash));
            decimal declarado = efectivo?.Declared ?? request.DeclaredAmount;
            decimal difference = declarado - expectedAmount;

            var billetes = CashCountRules.ValidateDenominations(request.Denominations, efectivo?.Declared, reglas.RequireDenominations);

            // Una diferencia grande exige explicarla. El intento rechazado queda
            // contado: con el conteo a ciegas, varios intentos pueden indicar que se
            // ajustaron las cifras hasta que el cierre pasó.
            var exige = CashCountRules.Requirement(arqueo, reglas, session.CloseAttempts);
            if (exige.NeedsNote && string.IsNullOrWhiteSpace(request.Notes))
            {
                await _cashSessionRepository.IncrementCloseAttempts(id);
                // Este rechazo puede ser el que agota los intentos.
                bool pideSupervisor = exige.NeedsSupervisor
                    || CashCountRules.Requirement(arqueo, reglas, session.CloseAttempts + 1).AttemptsExhausted;
                return Rechazo(resp, pideSupervisor ? "supervisor" : "note",
                    $"Hay diferencia en {string.Join(", ", exige.OverNote)}. Vuelva a contar y, si se mantiene, " +
                    "escriba una observación que la explique" +
                    (pideSupervisor ? " y pida la autorización de un supervisor." : " para poder cerrar."));
            }

            // Quien no es solo cajero se autoriza a sí mismo; un cajero necesita
            // la sesión de un supervisor. Sin volver a contar el intento: la
            // observación ya está, falta la firma.
            int? autorizadoPor = null;
            if (exige.NeedsSupervisor)
            {
                autorizadoPor = closerIsSupervisor ? userId : supervisorId;
                if (autorizadoPor is null)
                    return Rechazo(resp, "supervisor", exige.OverSupervisor.Count > 0
                        ? $"La diferencia en {string.Join(", ", exige.OverSupervisor)} necesita la autorización de un supervisor."
                        : $"Se superaron los {reglas.MaxAttempts} intentos de cierre: necesita la autorización de un supervisor.");
            }

            if (await _cashSessionRepository.CloseSessionWithCounts(
                    id, declarado, expectedAmount, difference, request.Notes, userId, arqueo, autorizadoPor, billetes) == 0)
                throw new CustomException("La sesión ya estaba cerrada.");

            resp.Data = await _cashSessionRepository.GetSessionById(id);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Error al cerrar la caja.", ex); }
        return resp;
    }

    /// <summary>
    /// Cierre rechazado: el mensaje, y en Data qué falta para que el punto de
    /// venta sepa si pedir la observación o abrir la autorización del supervisor.
    /// </summary>
    private static Response<CashSessionResponse> Rechazo(Response<CashSessionResponse> resp, string requires, string message)
    {
        resp.Data = new CashSessionResponse { CloseRequires = requires };
        resp.SetMessage(MessageTypes.Warning, message);
        return resp;
    }

    public async Task<Response<CashCloseSettings>> GetCloseSettings()
    {
        Response<CashCloseSettings> resp = new() { Data = new() };
        try
        {
            resp.Data = await _cashSessionRepository.GetCloseSettings();
            resp.Data.Methods = await _paymentMethodRepository.GetPaymentMethods();
            resp.ok = true;
        }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Error al consultar la configuración del cierre.", ex); }
        return resp;
    }

    public async Task<Response<bool>> SaveCloseSettings(CashCloseSettingsRequest request, int userId)
    {
        Response<bool> resp = new();
        try
        {
            if (request.NoteThreshold < 0)
                throw new CustomException("El monto no puede ser negativo.");
            if (request.SupervisorThreshold is decimal tope && tope < request.NoteThreshold)
                throw new CustomException("El monto que pide supervisor no puede ser menor al que pide observación.");
            if (request.MaxAttempts is < 1)
                throw new CustomException("El máximo de intentos tiene que ser al menos 1.");

            await _cashSessionRepository.SaveCloseSettings(request, userId);
            await _paymentMethodRepository.SetRequiresCount(
                request.Methods.Select(m => (m.Id, m.RequiresCount)).ToList(), userId);
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Error al guardar la configuración del cierre.", ex); }
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
            if (!DateTime.TryParse(dateFrom, out DateTime diaDesde))
                throw new CustomException("Fecha desde inválida.", MessageTypes.Warning);
            if (!DateTime.TryParse(dateTo, out DateTime diaHasta))
                throw new CustomException("Fecha hasta inválida.", MessageTypes.Warning);
            if (diaDesde.Date > diaHasta.Date)
                throw new CustomException("La fecha desde no puede ser mayor a la fecha hasta.", MessageTypes.Warning);

            // Días del calendario boliviano → ventana UTC; opened_at es un
            // instante. El tope es exclusivo (ver BusinessTime).
            var from = BusinessTime.StartOfDayUtc(diaDesde);
            var to = BusinessTime.EndOfDayUtcExclusive(diaHasta);

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
