namespace Inventory.Domain.Enums;

public enum PurchaseStatusEnum
{
    /// <summary>
    /// Solo para filtrar: significa "sin filtrar por estado". Ninguna orden lo
    /// tiene, así que no se confunde con un estado real. Está declarado porque
    /// el binder de ASP.NET rechaza un valor que el enum no define, y sin esto
    /// la consulta de todos los estados no llegaba ni al controlador.
    /// </summary>
    TODOS = 0,
    REQUESTED = 1,
    PARTIALLY_RECEIVED = 2,
    TOTALLY_RECEIVED = 3,
    /// <summary>Anulada antes de recibir nada.</summary>
    CANCELLED = 4,
    /// <summary>Cerrada con faltante: el proveedor no enviará el saldo pendiente.</summary>
    CLOSED = 5
}
