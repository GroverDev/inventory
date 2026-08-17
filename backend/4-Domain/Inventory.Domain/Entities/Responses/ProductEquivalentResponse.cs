namespace Inventory.Domain;

/// <summary>
/// Un producto que puede ofrecerse en lugar de otro.
/// </summary>
/// <remarks>
/// Dos orígenes distintos con presentación distinta a propósito:
/// <list type="bullet">
/// <item><b>Equivalente</b> (<c>IsManual = false</c>): misma composición y
/// concentración. Es intercambiable de verdad y lo deduce el sistema.</item>
/// <item><b>Sugerencia</b> (<c>IsManual = true</c>): la definió la farmacia por
/// razones comerciales, y puede tener otro principio activo. Quien vende debe
/// verlo distinto, porque la decisión es suya.</item>
/// </list>
/// </remarks>
public class ProductEquivalentResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal SalePrice { get; set; }
    public decimal CurrentStock { get; set; }

    public string ProductType { get; set; } = "";
    public string Presentation { get; set; } = "";

    public bool IsManual { get; set; }
    public string Reason { get; set; } = "";
}
