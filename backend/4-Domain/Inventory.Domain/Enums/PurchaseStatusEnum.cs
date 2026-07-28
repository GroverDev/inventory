namespace Inventory.Domain.Enums;

public enum PurchaseStatusEnum
{
    REQUESTED = 1,
    PARTIALLY_RECEIVED = 2,
    TOTALLY_RECEIVED = 3,
    /// <summary>Anulada antes de recibir nada.</summary>
    CANCELLED = 4,
    /// <summary>Cerrada con faltante: el proveedor no enviará el saldo pendiente.</summary>
    CLOSED = 5
}
