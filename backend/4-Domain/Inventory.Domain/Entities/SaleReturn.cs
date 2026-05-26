using Common.Utilities;

namespace Inventory.Domain;

public class SaleReturn : Audit
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public DateTime ReturnDate { get; set; }
    public string? Reason { get; set; }
    public decimal TotalReturned { get; set; }
    public bool IsFullReturn { get; set; }
    public List<SaleReturnDetail> Detail { get; set; } = [];
}
