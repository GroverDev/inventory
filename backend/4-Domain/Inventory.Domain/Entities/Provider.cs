using Common.Utilities;

namespace Inventory.Domain;

public class Provider : Audit
{
    public Guid Id { get; set; }
    public string ProviderName { get; set; }="";
    public string Description { get; set; } ="";
    public string Direction { get; set; } ="";
    public string Celular { get; set; } ="";
}
