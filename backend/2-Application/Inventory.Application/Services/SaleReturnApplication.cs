using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain;
using Inventory.Infrastructure;

namespace Inventory.Application;

public class SaleReturnApplication(
    ISaleReturnRepository _saleReturnRepository,
    ISalesRepository _salesRepository,
    IPaymentMethodRepository _paymentMethodRepository,
    ICashSessionRepository _cashSessionRepository) : ISaleReturnApplication
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

            if (request.Detail.GroupBy(d => d.SaleDetailId).Any(g => g.Count() > 1))
                throw new CustomException("Hay líneas repetidas en la devolución.");

            // Cargar la venta con su detalle y devoluciones previas
            var sale = await _salesRepository.GetSale(saleId);
            if (sale.Id == Guid.Empty)
                throw new CustomException("La venta no existe.");

            // Neto de cada línea = lo que se cobró por ella antes del descuento global.
            // Se calcula desde line_subtotal en vez de leer line_total porque hay ventas
            // viejas (feb/2026) con line_total guardado truncado a entero: 10.50 quedó
            // en 10. line_subtotal = cantidad * precio sí cuadra en todas.
            decimal Neto(SaleProductDetailResponse d) => d.LineSubtotal - d.LineTotalDiscounts;

            // Prorrateo del descuento global entre las líneas, proporcional a lo que
            // pesa cada una ya con sus propios descuentos aplicados. El resto por
            // redondeo se le asigna a la línea de mayor importe, para que la suma de
            // las partes sea exactamente el descuento aplicado y una devolución total
            // reembolse exactamente sale.Total, ni un centavo más.
            var headerShare = sale.Detail.ToDictionary(d => d.Id, _ => 0m);
            decimal sumaLineas = sale.Detail.Sum(Neto);
            if (sale.HeaderDiscountAmount > 0 && sumaLineas > 0)
            {
                foreach (var d in sale.Detail)
                    headerShare[d.Id] = Math.Round(sale.HeaderDiscountAmount * Neto(d) / sumaLineas, 2);

                decimal resto = sale.HeaderDiscountAmount - headerShare.Values.Sum();
                if (resto != 0)
                    headerShare[sale.Detail.OrderByDescending(Neto).First().Id] += resto;
            }

            var saleReturn = new SaleReturn
            {
                SaleId = saleId,
                ReturnDate = DateTime.UtcNow,
                Reason = request.Reason,
                State = true,
            };
            AuditHelper.SetCreated(saleReturn, createdBy);

            foreach (var line in request.Detail)
            {
                if (!Guid.TryParse(line.SaleDetailId, out Guid saleDetailId))
                    throw new CustomException("ID de detalle de venta no válido.");

                var soldLine = sale.Detail.FirstOrDefault(d => d.Id == saleDetailId)
                    ?? throw new CustomException("La línea de venta no pertenece a esta venta.");

                int alreadyReturned = sale.Returns
                    .SelectMany(r => r.Detail)
                    .Where(d => d.SaleDetailId == saleDetailId)
                    .Sum(d => d.QuantityReturned);

                int available = soldLine.Quantity - alreadyReturned;
                if (line.QuantityReturned > available)
                    throw new CustomException(
                        $"'{soldLine.ProductName}': se intenta devolver {line.QuantityReturned} " +
                        $"pero solo quedan {available} disponibles para devolución.");

                // Los importes salen de la venta, no del request: el cliente solo dice
                // qué línea y cuántas unidades. El UnitPrice que manda es el precio de
                // lista y se ignora a propósito — reembolsar sobre él devolvería los
                // descuentos que el cliente nunca llegó a pagar, y además dejaría el
                // monto del reembolso en manos de quien arma el payload.
                decimal descuentosLinea = soldLine.LineTotalDiscounts + headerShare[soldLine.Id];
                decimal descuentoDevuelto = line.QuantityReturned == soldLine.Quantity
                    ? descuentosLinea
                    : Math.Round(descuentosLinea * line.QuantityReturned / soldLine.Quantity, 2);

                decimal montoLinea = Math.Round(line.QuantityReturned * soldLine.UnitPrice, 2) - descuentoDevuelto;

                var det = new SaleReturnDetail
                {
                    SaleDetailId = saleDetailId,
                    ProductId = soldLine.ProductId,
                    QuantityReturned = line.QuantityReturned,
                    UnitPrice = soldLine.UnitPrice,
                    DiscountShare = descuentoDevuelto,
                    LineTotal = montoLinea,
                    State = true,
                };
                AuditHelper.SetCreated(det, createdBy);
                saleReturn.Detail.Add(det);
            }

            saleReturn.TotalReturned = saleReturn.Detail.Sum(d => d.LineTotal);

            // Invariante: entre todas las devoluciones nunca se puede reembolsar más de
            // lo que la venta cobró.
            decimal yaReembolsado = sale.Returns.Sum(r => r.TotalReturned);
            if (saleReturn.TotalReturned + yaReembolsado > sale.Total)
                throw new CustomException(
                    $"El reembolso ({saleReturn.TotalReturned:0.00}) supera el saldo devolvible " +
                    $"de la venta ({sale.Total - yaReembolsado:0.00}).");

            // Devolución total: ninguna línea queda con unidades pendientes de devolver
            saleReturn.IsFullReturn = sale.Detail.All(soldLine =>
            {
                int alreadyReturned = sale.Returns
                    .SelectMany(r => r.Detail)
                    .Where(d => d.SaleDetailId == soldLine.Id)
                    .Sum(d => d.QuantityReturned);

                int newReturn = request.Detail
                    .Where(d => Guid.TryParse(d.SaleDetailId, out var id) && id == soldLine.Id)
                    .Sum(d => d.QuantityReturned);

                return (alreadyReturned + newReturn) >= soldLine.Quantity;
            });

            // Medio de reintegro: lo elige el POS, precargado con el de la venta. Si no
            // viene (una venta vieja sin pagos registrados, por ejemplo) se usa el de mayor
            // importe de la venta, y si tampoco hay, la devolución queda sin medio y sin
            // impacto en caja.
            var metodos = await _paymentMethodRepository.GetPaymentMethods();

            Guid? metodoId = Guid.TryParse(request.PaymentMethodId, out var pmId) && pmId != Guid.Empty
                ? pmId
                : sale.Payments.OrderByDescending(p => p.AmountGiven).FirstOrDefault()?.PaymentMethodId;

            var metodo = metodos.FirstOrDefault(m => m.Id == metodoId);
            if (metodoId.HasValue && metodoId != Guid.Empty && metodo is null)
                throw new CustomException("El medio de reintegro no es válido.");

            saleReturn.PaymentMethodId = metodo?.Id;

            // Si la plata sale del cajón tiene que haber cajón abierto: es la única forma
            // de que ninguna salida de efectivo quede afuera de un arqueo. Los reintegros
            // por QR o tarjeta no tocan la caja y no necesitan sesión.
            if (metodo?.AffectsCash == true)
            {
                var sesion = await _cashSessionRepository.GetActiveSessionByUser(createdBy)
                    ?? throw new CustomException(
                        "Para devolver en efectivo debe tener una caja abierta. " +
                        "Abra la caja e intente nuevamente.");

                saleReturn.CashSessionId = sesion.Id;
            }

            respuesta.Data = await _saleReturnRepository.CreateReturn(saleReturn);
            respuesta.ok = true;
        }
        catch (CustomException ex) { respuesta.SetMessage(MessageTypes.Warning, ex.Message); }
        catch (Exception ex) { respuesta.SetLogMessage(MessageTypes.Error, "Ocurrió un error, por favor comuníquese con Sistemas.", ex); }
        return respuesta;
    }
}
