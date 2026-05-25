using Common.Utilities;

namespace Inventory.Domain;

public class Category : Audit
{
    public Guid Id { get; set; }
    public string CategoryName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = false;
}
