using Common.Utilities.Exceptions;
using Inventory.Domain;

namespace Inventory.Application;

/// <summary>
/// Pagos de una venta. El vuelto lo calcula el servidor: el arqueo de caja resta
/// el vuelto entregado, así que un vuelto inventado por el cliente descuadraría
/// la caja.
/// </summary>
public static class SalePaymentRules
{
    /// <summary>
    /// Valida los pagos contra el total y fija el vuelto de cada uno.
    /// </summary>
    /// <param name="daVuelto">Si cada método entrega vuelto (efectivo sí; tarjeta y QR no).</param>
    /// <exception cref="CustomException">Pagos que no cubren el total, o que cobran de más con un método sin vuelto.</exception>
    public static void Apply(List<SalePaymentRequest> payments, decimal total, IReadOnlyDictionary<Guid, bool> daVuelto)
    {
        if (payments.Count == 0)
            throw new CustomException("Debe registrar al menos un método de pago.");

        foreach (var p in payments)
        {
            if (!Guid.TryParse(p.PaymentMethodId, out var id) || !daVuelto.ContainsKey(id))
                throw new CustomException("Hay un pago con un método que no existe o está inactivo.");
            if (p.AmountGiven <= 0)
                throw new CustomException("Los montos de pago deben ser mayores a cero.");
        }

        bool DaVuelto(SalePaymentRequest p) => daVuelto[Guid.Parse(p.PaymentMethodId)];

        // Con tarjeta o QR se cobra exacto: si esos pagos solos superan el total,
        // se estaría cobrando de más sin vuelto posible.
        decimal sinVuelto = payments.Where(p => !DaVuelto(p)).Sum(p => p.AmountGiven);
        if (sinVuelto > total)
            throw new CustomException(
                $"Los pagos sin vuelto (tarjeta, QR...) suman Bs. {sinVuelto:0.00} y superan el total de Bs. {total:0.00}.");

        decimal pagado = payments.Sum(p => p.AmountGiven);
        if (pagado < total)
            throw new CustomException("El monto total pagado no puede ser menor al total de la venta.");

        // El vuelto sale de los pagos que lo admiten, empezando por el último
        // (normalmente el efectivo con el que se completó el cobro).
        decimal vuelto = pagado - total;
        foreach (var p in payments) p.AmountReturned = 0;
        foreach (var p in Enumerable.Reverse(payments).Where(DaVuelto))
        {
            if (vuelto <= 0) break;
            p.AmountReturned = Math.Min(vuelto, p.AmountGiven);
            vuelto -= p.AmountReturned;
        }
    }
}
