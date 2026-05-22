namespace Inventory.Domain;

public class LaboratoryRequest
{

    public string Id { get; set; } = Guid.Empty.ToString();
    public string LaboratoryName { get; set; }= Guid.Empty.ToString();
    public string Description { get; set; }= Guid.Empty.ToString();
    public string Direction { get; set; }= Guid.Empty.ToString();
    public string Celular { get; set; }= Guid.Empty.ToString();
    public bool IsActive { get; set; }

}