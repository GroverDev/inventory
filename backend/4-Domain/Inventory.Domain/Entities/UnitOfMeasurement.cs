using System;
using Common.Utilities;

namespace Inventory.Domain.Entities;

public class UnitOfMeasurement: Audit
{
    public Guid Id { get; set; }
    public string UnitName { get; set; }="";
    public int Proportion { get; set; }
    public int PrecisionRounding { get; set; }
    public bool IsLargeThanDefault { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

