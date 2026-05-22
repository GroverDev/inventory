namespace Inventory.Domain.Entities.Responses;

public class UnitOfMeasurementResponse
{
    public Guid Id { get; set; }
    public string UnitName { get; set; }="";
    public int Proportion { get; set; }
    public int PrecisionRounding { get; set; }
    public bool IsLargeThanDefault { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}
