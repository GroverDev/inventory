namespace Inventory.Domain;

public class DiscountRequest
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";  // "Percentage" | "FixedAmount"
    public decimal Value { get; set; }
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
