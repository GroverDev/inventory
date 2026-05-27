namespace Inventory.Domain;

public class CashMovementResponse
{
    public Guid Id { get; set; }
    public Guid CashSessionId { get; set; }
    public string MovementType { get; set; } = "";
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public DateTime Created { get; set; }
}
