namespace Inventory.Domain;

public class CashSessionResponse
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public string UserFullName { get; set; } = "";
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public bool IsOpen => ClosedAt == null;
    public decimal OpeningAmount { get; set; }
    public decimal? DeclaredAmount { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal? Difference { get; set; }
    public string Notes { get; set; } = "";
    public decimal TotalSales { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalIncome { get; set; }
    public List<CashMovementResponse> Movements { get; set; } = [];
}
