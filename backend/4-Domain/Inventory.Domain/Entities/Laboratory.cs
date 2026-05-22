using Common.Utilities;

namespace Inventory.Domain;

public class Laboratory: Audit
{
    public Guid Id { get; set; }
    public string LaboratoryName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Direction { get; set; } = "";
    public string Celular { get; set; } = "";
    public bool IsActive { get; set; } = false;
	
}

