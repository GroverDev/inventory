namespace Inventory.Domain;

public class PaymentMethod
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string IconCss { get; set; } = "";
    public bool RequiresChanges { get; set; }
}
