namespace Inventory.Domain;

/// <summary>
/// Una venta de un lote o de una unidad con número de serie, con el cliente que
/// se la llevó. Es lo que permite responder a un retiro de mercado o a un
/// reclamo de garantía: sin esto, la farmacia sabe que lo vendió pero no a quién
/// avisarle.
/// </summary>
public class LotTraceabilityResponse
{
    /// <summary>Lote, o vacío si la unidad se identifica por número de serie.</summary>
    public string LotCode { get; set; } = "";

    /// <summary>Número de serie, o vacío si la existencia es un lote.</summary>
    public string SerialNumber { get; set; } = "";
    public DateTime? ExpiryDate { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";

    public Guid SaleId { get; set; }
    public DateTime SaleDate { get; set; }
    public int Quantity { get; set; }

    /// <summary>Datos de contacto: son el motivo de la consulta.</summary>
    public string Cliente { get; set; } = "";
    public string? DocumentNumber { get; set; }
    public string? Cellphone { get; set; }
}
