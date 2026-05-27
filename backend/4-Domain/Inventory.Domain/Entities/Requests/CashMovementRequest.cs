namespace Inventory.Domain;

public class CashMovementRequest
{
    public string CashSessionId { get; set; } = "";
    public string MovementType { get; set; } = "";
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
}
