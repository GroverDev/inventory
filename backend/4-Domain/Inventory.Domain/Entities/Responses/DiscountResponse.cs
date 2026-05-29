namespace Inventory.Domain;

public class DiscountResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal Value { get; set; }
    public string Description { get; set; } = "";
    public bool IsActive { get; set; }
}
