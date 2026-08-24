using Common.Utilities;

namespace Inventory.Domain;

public class SaleReturnDetail : Audit
{
    public Guid Id { get; set; }
    public Guid ReturnId { get; set; }
    public Guid SaleDetailId { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityReturned { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Descuentos que corresponden a las unidades devueltas: el de la propia linea
    /// mas la porcion prorrateada del descuento global de la venta.
    /// </summary>
    public decimal DiscountShare { get; set; }
    public decimal LineTotal { get; set; }
}
