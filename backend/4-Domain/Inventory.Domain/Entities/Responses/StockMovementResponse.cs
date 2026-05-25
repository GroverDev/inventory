namespace Inventory.Domain.Entities.Responses;

public class StockMovementResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public string MovementType { get; set; } = "";
    public int Quantity { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    public string? Reason { get; set; }
    public string? Observation { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime Created { get; set; }
    public int CreatedBy { get; set; }
}
