namespace Inventory.Domain;

/// <summary>Precio, mínimo y stock de un producto en una sucursal.</summary>
public class ProductBranchSettingResponse
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = "";

    /// <summary>Excepción de precio de la sucursal, o null si usa el base.</summary>
    public decimal? SalePrice { get; set; }

    /// <summary>Excepción de mínimo de reposición, o null si usa el base.</summary>
    public int? MinReorderQuantity { get; set; }

    public decimal BaseSalePrice { get; set; }
    public int BaseMinReorderQuantity { get; set; }

    /// <summary>Stock actual del producto en la sucursal.</summary>
    public decimal Stock { get; set; }
}
