namespace Inventory.Domain;

public class CloseCashSessionRequest
{
    public decimal DeclaredAmount { get; set; }
    public string Notes { get; set; } = "";
}
