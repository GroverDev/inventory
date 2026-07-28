namespace Inventory.Domain;

/// <summary>
/// Saldo de una línea del pedido: lo ordenado contra lo efectivamente recibido.
/// El recibido se calcula sumando el log de recepciones, no leyendo un contador.
/// </summary>
public class PurchaseLineBalance
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int OrderedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }

    public int PendingQuantity => Math.Max(0, OrderedQuantity - ReceivedQuantity);
    public bool IsComplete => ReceivedQuantity >= OrderedQuantity;
}
