using Common.Utilities;

namespace Inventory.Domain;

public class Provider : Audit
{
    public Guid Id { get; set; }
    public string ProviderName { get; set; }="";
    public string Description { get; set; } ="";
    public string Direction { get; set; } ="";
    public string Celular { get; set; } ="";
    public bool IsCompany { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
