using System;

namespace Inventory.Domain.Entities.Responses;

public class ProductResponse
{
    public Guid Id { get; set; } = Guid.Empty;
    public string ProductCode { get; set; } ="";
    public string ProductName { get; set; }="";
    public string Description { get; set; }="";
    /// <summary>Precio en la sucursal activa: su excepción si la tiene, si no el base.</summary>
    public decimal SalePrice { get; set; }

    /// <summary>
    /// Precio del catálogo, el mismo para toda la farmacia. Es el que se edita
    /// en la ficha y en la carga masiva; <see cref="SalePrice"/> no, porque
    /// copiaría la excepción de una sucursal a todas.
    /// </summary>
    public decimal BaseSalePrice { get; set; }

    public string BarCode { get; set; } = "";
    public int CurrentStock { get; set; }
    /// <summary>Mínimo de reposición en la sucursal activa.</summary>
    public int MinReorderQuantity { get; set; }

    /// <inheritdoc cref="BaseSalePrice"/>
    public int BaseMinReorderQuantity { get; set; }
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
