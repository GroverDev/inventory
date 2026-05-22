namespace Inventory.Domain;

public class ProviderRequest
{
    public string Id { get; set; } = Guid.Empty.ToString();
    public string ProviderName { get; set; } ="";
    public string Description { get; set; } ="";
    public string Direction { get; set; } ="";
    public string Celular { get; set; } ="";

}
