using System;

namespace Inventory.Domain.Entities.Responses;

public class ProductResponse
{
    public Guid Id { get; set; } = Guid.Empty;
    public string ProductCode { get; set; } ="";
    public string ProductName { get; set; }="";
    public string Description { get; set; }="";
    public decimal SalePrice { get; set; }

    public string BarCode { get; set; } = "";
    public int CurrentStock { get; set; }
    public int MinReorderQuantity { get; set; }
    public bool AvailableInPos { get; set; }
    public bool RequiresAuthorization { get; set; }
    public Guid UomId { get; set; }= Guid.Empty;
    public string UnitName { get; set; }="";
    public Guid? LaboratoryId { get; set; }
    public string LaboratoryName { get; set; }="";
    public Guid? CategoryId { get; set; }
    public string CategoryName { get; set; }="";
 
    public bool IsActive { get; set; }

    /// <summary>
    /// Seguimiento de existencias: 'none', 'lot' o 'serial'. Decide si la recepción
    /// exige lote y si la venta reparte por FEFO.
    /// </summary>
    public string TrackingMode { get; set; } = "none";

}
