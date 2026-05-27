using Common.Utilities;

namespace Inventory.Domain;

public class CashMovement : Audit
{
    public Guid Id { get; set; }
    public Guid CashSessionId { get; set; }
    public string MovementType { get; set; } = "";
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
}
