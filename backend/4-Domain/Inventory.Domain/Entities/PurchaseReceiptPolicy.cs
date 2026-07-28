using Common.Utilities;
using Common.Utilities.Exceptions;
using Inventory.Domain.Enums;

namespace Inventory.Domain;

/// <summary>
/// Reglas de negocio de la recepción de compras.
///
/// Es deliberadamente pura (sin I/O) para poder evaluarse dentro de la misma
/// transacción que escribe, sobre saldos recién leídos y bloqueados. El estado
/// de la orden es una función de los saldos, nunca un dato que elige el usuario.
/// </summary>
public static class PurchaseReceiptPolicy
{
    /// <summary>Valida que el estado actual de la orden admita recibir mercadería.</summary>
    public static void EnsureCanReceive(int purchaseStatusId)
    {
        switch ((PurchaseStatusEnum)purchaseStatusId)
        {
            case PurchaseStatusEnum.REQUESTED:
            case PurchaseStatusEnum.PARTIALLY_RECEIVED:
                return;
            case PurchaseStatusEnum.TOTALLY_RECEIVED:
                throw new CustomException("La orden ya fue recibida en su totalidad.", MessageTypes.Warning);
            case PurchaseStatusEnum.CANCELLED:
                throw new CustomException("La orden está cancelada y no admite recepciones.", MessageTypes.Warning);
            case PurchaseStatusEnum.CLOSED:
                throw new CustomException("La orden está cerrada y no admite recepciones.", MessageTypes.Warning);
            default:
                throw new CustomException("El estado de la orden no permite recepcionar.", MessageTypes.Warning);
        }
    }

    /// <summary>
    /// Valida las líneas entrantes contra los saldos pendientes. Tolerancia cero
    /// a la sobre-recepción: recibir de más rompe la conciliación con el proveedor.
    /// </summary>
    public static void EnsureLinesAreReceivable(
        IEnumerable<PurchaseLineBalance> balances,
        IReadOnlyCollection<PurchaseDeliveryDetail> incoming)
    {
        if (incoming.Count == 0)
            throw new CustomException("El detalle de la recepción no puede estar vacío.", MessageTypes.Warning);

        var byProduct = balances.ToDictionary(b => b.ProductId);
        var seen = new HashSet<Guid>();
        var totalReceived = 0;

        foreach (var line in incoming)
        {
            if (!byProduct.TryGetValue(line.ProductId, out var balance))
                throw new CustomException("Se intentó recibir un producto que no pertenece a la orden de compra.", MessageTypes.Warning);

            if (!seen.Add(line.ProductId))
                throw new CustomException($"El producto '{balance.ProductName}' está repetido en la recepción.", MessageTypes.Warning);

            if (line.DeliveryQuantity < 0)
                throw new CustomException($"La cantidad recibida de '{balance.ProductName}' no puede ser negativa.", MessageTypes.Warning);

            if (line.DeliveryQuantity > balance.PendingQuantity)
                throw new CustomException(
                    $"No se puede recibir {line.DeliveryQuantity} de '{balance.ProductName}': el pendiente es {balance.PendingQuantity}.",
                    MessageTypes.Warning);

            if (line.UnitPrice < 0)
                throw new CustomException($"El precio de '{balance.ProductName}' no puede ser negativo.", MessageTypes.Warning);

            totalReceived += line.DeliveryQuantity;
        }

        if (totalReceived == 0)
            throw new CustomException("Debe recibir al menos una unidad para registrar la recepción.", MessageTypes.Warning);
    }

    /// <summary>Deriva el estado de la orden a partir de los saldos ya actualizados.</summary>
    public static PurchaseStatusEnum DeriveStatus(IEnumerable<PurchaseLineBalance> balances)
    {
        var lines = balances.ToList();

        if (lines.Count == 0) return PurchaseStatusEnum.REQUESTED;
        if (lines.TrueForAll(b => b.IsComplete)) return PurchaseStatusEnum.TOTALLY_RECEIVED;
        if (lines.Exists(b => b.ReceivedQuantity > 0)) return PurchaseStatusEnum.PARTIALLY_RECEIVED;

        return PurchaseStatusEnum.REQUESTED;
    }

    /// <summary>
    /// Una orden solo se puede editar o eliminar mientras no haya recibido nada.
    /// Después, el pedido es la referencia contra la que se concilió el stock:
    /// cambiarlo dejaría al inventario contando una historia distinta.
    /// </summary>
    public static void EnsureCanModify(int purchaseStatusId)
    {
        var status = (PurchaseStatusEnum)purchaseStatusId;

        if (status == PurchaseStatusEnum.REQUESTED) return;

        throw status switch
        {
            PurchaseStatusEnum.PARTIALLY_RECEIVED => new CustomException("La orden ya tiene recepciones registradas y no se puede modificar.", MessageTypes.Warning),
            PurchaseStatusEnum.TOTALLY_RECEIVED => new CustomException("La orden ya fue recibida en su totalidad y no se puede modificar.", MessageTypes.Warning),
            PurchaseStatusEnum.CANCELLED => new CustomException("La orden está cancelada.", MessageTypes.Warning),
            PurchaseStatusEnum.CLOSED => new CustomException("La orden está cerrada.", MessageTypes.Warning),
            _ => new CustomException("El estado de la orden no permite modificarla.", MessageTypes.Warning)
        };
    }

    /// <summary>
    /// Cierre manual con faltante. Solo tiene sentido sobre una orden que ya
    /// recibió algo pero no todo: sin recepciones corresponde cancelar.
    /// </summary>
    public static void EnsureCanClose(int purchaseStatusId)
    {
        var status = (PurchaseStatusEnum)purchaseStatusId;

        if (status == PurchaseStatusEnum.PARTIALLY_RECEIVED) return;

        throw status switch
        {
            PurchaseStatusEnum.REQUESTED => new CustomException("La orden no tiene recepciones: corresponde cancelarla, no cerrarla.", MessageTypes.Warning),
            PurchaseStatusEnum.TOTALLY_RECEIVED => new CustomException("La orden ya fue recibida en su totalidad.", MessageTypes.Warning),
            _ => new CustomException("La orden ya no admite cambios de estado.", MessageTypes.Warning)
        };
    }

    /// <summary>
    /// Anulación. Se prohíbe si existe alguna recepción, porque el stock ya se
    /// movió: en ese caso corresponde cerrar con faltante o devolver al proveedor.
    /// </summary>
    public static void EnsureCanCancel(int purchaseStatusId, bool hasDeliveries)
    {
        if (hasDeliveries)
            throw new CustomException("La orden tiene recepciones registradas: no se puede cancelar, debe cerrarse con faltante.", MessageTypes.Warning);

        if ((PurchaseStatusEnum)purchaseStatusId != PurchaseStatusEnum.REQUESTED)
            throw new CustomException("Solo se pueden cancelar órdenes en estado Solicitado.", MessageTypes.Warning);
    }
}
