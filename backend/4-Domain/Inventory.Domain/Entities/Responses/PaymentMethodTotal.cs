namespace Inventory.Domain;

/// <summary>Lo que entró por un medio de pago en un período o un turno.</summary>
public class PaymentMethodTotal
{
    /// <summary>Null para devoluciones viejas que no registraron con qué medio se reintegraron.</summary>
    public Guid? PaymentMethodId { get; set; }
    public string Name { get; set; } = "";
    public string IconCss { get; set; } = "";

    /// <summary>Cobrado: lo entregado menos el vuelto.</summary>
    public decimal Collected { get; set; }

    /// <summary>Reintegrado por devoluciones con este medio.</summary>
    public decimal Refunded { get; set; }

    public decimal Net { get; set; }
}
