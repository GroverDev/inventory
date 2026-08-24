using Common.Utilities;

namespace Inventory.Domain;

public class SaleReturn : Audit
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public DateTime ReturnDate { get; set; }
    public string? Reason { get; set; }
    public decimal TotalReturned { get; set; }
    public bool IsFullReturn { get; set; }

    /// <summary>Medio por el que se reintegró al cliente.</summary>
    public Guid? PaymentMethodId { get; set; }

    /// <summary>
    /// Sesión de caja de la que sale el efectivo. Solo se completa cuando el
    /// reintegro afecta caja: es lo que dispara el movimiento de tipo return.
    /// </summary>
    public Guid? CashSessionId { get; set; }
    public List<SaleReturnDetail> Detail { get; set; } = [];
}
