using Common.Utilities;

namespace Inventory.Domain;

public class Customer : Audit
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public string Cellphone { get; set; } = "";
    public bool IsActive { get; set; }
}
