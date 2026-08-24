namespace Inventory.Domain;

public class PaymentMethod
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string IconCss { get; set; } = "";
    public bool RequiresChanges { get; set; }

    /// <summary>Si el cobro entra físicamente al cajón. Lo que el arqueo suma como efectivo.</summary>
    public bool AffectsCash { get; set; }
}
