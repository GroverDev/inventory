namespace Inventory.Domain;

public class SaleReturnRequest
{
    public string SaleId { get; set; } = "";
    public string? Reason { get; set; }

    /// <summary>
    /// Medio por el que se reintegra. Si viene vacío se usa el de la venta.
    /// El servidor decide con él si sale plata del cajón (payment_methods.affects_cash).
    /// </summary>
    public string? PaymentMethodId { get; set; }
    public List<SaleReturnDetailRequest> Detail { get; set; } = [];
}

public class SaleReturnDetailRequest
{
    public string SaleDetailId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public int QuantityReturned { get; set; }
    public decimal UnitPrice { get; set; }
}
