using Common.Utilities;

namespace Inventory.Domain;

public class Discount : Audit
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";  // "Percentage" | "FixedAmount"
    public decimal Value { get; set; }
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
