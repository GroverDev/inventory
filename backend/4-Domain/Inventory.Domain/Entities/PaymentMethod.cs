namespace Inventory.Domain;

public class PaymentMethod
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string IconCss { get; set; } = "";
    public bool RequiresChanges { get; set; }

    /// <summary>Si el cobro entra físicamente al cajón. Lo que el arqueo suma como efectivo.</summary>
    public bool AffectsCash { get; set; }

    /// <summary>
    /// Se declara al cerrar la caja. El efectivo se arquea siempre; tarjeta y QR,
    /// si se activa (contra el cierre de lote del datáfono o lo recibido en la cuenta).
    /// </summary>
    public bool RequiresCount { get; set; }

    /// <summary>Si este medio entra en el arqueo del cierre.</summary>
    public bool IsCounted => AffectsCash || RequiresCount;
}
