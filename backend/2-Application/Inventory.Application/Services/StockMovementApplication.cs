using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Domain.Entities.Responses;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class StockMovementApplication(IStockMovementRepository _stockMovementRepository) : IStockMovementApplication
{
    public async Task<Response<List<StockMovementResponse>>> GetMovementsByProduct(string productId, string? stockItemId)
    {
        var resp = new Response<List<StockMovementResponse>>() { Data = [] };
        try
        {
            Guid id = Guid.Parse(productId);
            Guid? stockId = string.IsNullOrWhiteSpace(stockItemId) ? null : Guid.Parse(stockItemId);
            resp.Data = await _stockMovementRepository.GetMovementsByProduct(id, stockId);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<List<LotTraceabilityResponse>>> GetTraceability(string lotCode)
    {
        var resp = new Response<List<LotTraceabilityResponse>>() { Data = [] };
        try
        {
            if (string.IsNullOrWhiteSpace(lotCode))
                throw new CustomException("Indique el lote que quiere rastrear.", MessageTypes.Warning);

            resp.Data = await _stockMovementRepository.GetTraceability(lotCode);
            resp.ok = true;

            // Que no haya ventas es un resultado legítimo —el lote sigue en el
            // estante—, pero se dice, porque desde una pantalla vacía no se
            // distingue de un lote mal tecleado.
            if (resp.Data.Count == 0)
                resp.SetMessage(MessageTypes.Info,
                    $"No hay ventas registradas del lote «{lotCode.Trim()}».");
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<List<StockSerialResponse>>> GetAvailableSerials(string productId)
    {
        var resp = new Response<List<StockSerialResponse>>() { Data = [] };
        try
        {
            if (!Guid.TryParse(productId, out var id) || id == Guid.Empty)
                throw new CustomException("El identificador del producto no es válido.", MessageTypes.Warning);

            resp.Data = await _stockMovementRepository.GetAvailableSerials(id);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<List<StockExpiryResponse>>> GetExpiring(int dias)
    {
        var resp = new Response<List<StockExpiryResponse>>() { Data = [] };
        try
        {
            resp.Data = await _stockMovementRepository.GetExpiring(dias);
            resp.ok = true;

            int vencidos = resp.Data.Count(x => x.Estado == "VENCIDO");
            int criticos = resp.Data.Count(x => x.Estado == "CRITICO");

            // Un mensaje solo cuando hay algo que hacer. Un listado vacío no
            // necesita explicación.
            if (vencidos > 0 || criticos > 0)
                resp.SetMessage(MessageTypes.Warning,
                    $"Hay {vencidos} existencia(s) vencida(s) y {criticos} que vencen en menos de 30 días.");
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<bool>> CreateAdjustment(StockAdjustmentRequest request, int userId)
    {
        var resp = new Response<bool>();
        try
        {
            var movement = new StockMovement
            {
                ProductId = Guid.Parse(request.ProductId),
                Quantity = request.Quantity,
                Reason = request.Reason,
                Observation = string.IsNullOrWhiteSpace(request.Observation) ? null : request.Observation,
            };

            await _stockMovementRepository.CreateAdjustment(movement, userId);
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<bool>> CreateWriteOff(StockWriteOffRequest request, int userId)
    {
        var resp = new Response<bool>();
        try
        {
            var movement = new StockMovement
            {
                ProductId = Guid.Parse(request.ProductId),
                Quantity = (int)request.Quantity,
                Reason = request.Reason,
                Observation = string.IsNullOrWhiteSpace(request.Observation) ? null : request.Observation,
            };

            await _stockMovementRepository.CreateWriteOff(movement, Guid.Parse(request.StockItemId), userId);
            resp.Data = resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }

    public async Task<Response<WriteOffReportResponse>> GetWriteOffs(DateTime desde, DateTime hasta, string? productId)
    {
        var resp = new Response<WriteOffReportResponse>() { Data = new WriteOffReportResponse() };
        try
        {
            Guid? id = string.IsNullOrWhiteSpace(productId) ? null : Guid.Parse(productId);
            resp.Data = await _stockMovementRepository.GetWriteOffs(desde, hasta, id);
            resp.ok = true;
        }
        catch (CustomException ex) { resp.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { resp.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return resp;
    }
}
