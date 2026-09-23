namespace Inventory.Domain;

public class PosSettingsResponse
{
    public decimal MaxCashierDiscountPct    { get; set; }
    public decimal MaxCashierDiscountAmount { get; set; }

    /// <summary>Tope máximo para cualquier rol en la sucursal activa; null = sin tope.</summary>
    public decimal? MaxDiscountPct { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
}
