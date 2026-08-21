using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class SaleReturnApplication(
    ISaleReturnRepository _saleReturnRepository,
    ISalesRepository _salesRepository) : ISaleReturnApplication
{
    public async Task<Response<string>> CreateReturn(SaleReturnRequest request, int createdBy)
    {
        Response<string> respuesta = new();
        try
        {
            if (!Guid.TryParse(request.SaleId, out Guid saleId))
                throw new CustomException("ID de venta no válido.");

            if (request.Detail.Count == 0)
                throw new CustomException("Debe seleccionar al menos un producto para devolver.");

            if (request.Detail.Any(d => d.QuantityReturned <= 0))
                throw new CustomException("La cantidad a devolver debe ser mayor a cero.");

            // Cargar la venta con su detalle y devoluciones previas
            var sale = await _salesRepository.GetSale(saleId);
            if (sale.Id == Guid.Empty)
                throw new CustomException("La venta no existe.");

            // Validar que no se devuelva más de lo vendido (considerando devoluciones previas)
            foreach (var line in request.Detail)
            {
                if (!Guid.TryParse(line.SaleDetailId, out Guid saleDetailId))
                    throw new CustomException("ID de detalle de venta no válido.");

                var soldLine = sale.Detail.FirstOrDefault(d => d.Id == saleDetailId)
                    ?? throw new CustomException($"La línea de venta no pertenece a esta venta.");

                int alreadyReturned = sale.Returns
                    .SelectMany(r => r.Detail)
                    .Where(d => d.SaleDetailId == saleDetailId)
                    .Sum(d => d.QuantityReturned);

                int available = soldLine.Quantity - alreadyReturned;
                if (line.QuantityReturned > available)
                    throw new CustomException(
                        $"'{soldLine.ProductName}': se intenta devolver {line.QuantityReturned} " +
                        $"pero solo quedan {available} disponibles para devolución.");
            }

            // Calcular total devuelto
            decimal totalReturned = request.Detail.Sum(d => d.QuantityReturned * d.UnitPrice);

            // Determinar si es devolución total: todas las líneas quedan en 0
            bool isFullReturn = sale.Detail.All(soldLine =>
            {
                int alreadyReturned = sale.Returns
                    .SelectMany(r => r.Detail)
                    .Where(d => d.SaleDetailId == soldLine.Id)
                    .Sum(d => d.QuantityReturned);

                var thisReturn = request.Detail.FirstOrDefault(d =>
                    Guid.Parse(d.SaleDetailId) == soldLine.Id);

                int newReturn = thisReturn?.QuantityReturned ?? 0;
                return (alreadyReturned + newReturn) >= soldLine.Quantity;
            });

            // Construir entidad
            var saleReturn = new SaleReturn
            {
                SaleId = saleId,
                ReturnDate = DateTime.UtcNow,
                Reason = request.Reason,
                TotalReturned = totalReturned,
                IsFullReturn = isFullReturn,
                State = true,
            };
            AuditHelper.SetCreated(saleReturn, createdBy);

            foreach (var line in request.Detail)
            {
                var det = new SaleReturnDetail
                {
                    SaleDetailId = Guid.Parse(line.SaleDetailId),
                    ProductId = Guid.Parse(line.ProductId),
                    QuantityReturned = line.QuantityReturned,
                    UnitPrice = line.UnitPrice,
                    LineTotal = line.QuantityReturned * line.UnitPrice,
                    State = true,
                };
                AuditHelper.SetCreated(det, createdBy);
                saleReturn.Detail.Add(det);
            }

            respuesta.Data = await _saleReturnRepository.CreateReturn(saleReturn);
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return respuesta;
    }
}
