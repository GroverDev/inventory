using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class CashMovementApplication(
    ICashMovementRepository _cashMovementRepository,
    ICashSessionRepository _cashSessionRepository) : ICashMovementApplication
{
    private static readonly string[] ValidTypes = ["expense", "withdrawal", "income"];

    public async Task<Response<string>> CreateMovement(CashMovementRequest request, int userId)
    {
        Response<string> resp = new();
        try
        {
            if (!Guid.TryParse(request.CashSessionId, out Guid sessionId))
                throw new CustomException("ID de sesión inválido.");

            if (!ValidTypes.Contains(request.MovementType))
                throw new CustomException("Tipo de movimiento inválido. Use: expense, withdrawal o income.");

            if (request.Amount <= 0)
                throw new CustomException("El monto debe ser mayor a cero.");

            if (string.IsNullOrWhiteSpace(request.Description))
                throw new CustomException("La descripción es obligatoria.");

            var session = await _cashSessionRepository.GetSessionById(sessionId);
            if (session == null || session.ClosedAt != null)
                throw new CustomException("La sesión de caja no existe o ya está cerrada.");

            var movement = new CashMovement
            {
                CashSessionId = sessionId,
                MovementType = request.MovementType,
                Amount = request.Amount,
                Description = request.Description.Trim()
            };
            AuditHelper.SetCreated(movement, userId);

            var id = await _cashMovementRepository.CreateMovement(movement);
            resp.Data = id.ToString();
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Error al registrar el movimiento.", ex); }
        return resp;
    }
}
