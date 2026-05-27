using Common.Utilities;

namespace Inventory.Domain;

public class CashSession : Audit
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningAmount { get; set; }
    public decimal? DeclaredAmount { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal? Difference { get; set; }
    public string Notes { get; set; } = "";
}
