namespace Inventory.Domain;

/// <summary>Un evento de baja por vencimiento/pérdida, tal como lo devuelve v_mermas.</summary>
public class WriteOffDetailResponse
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string? LotCode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Cantidad { get; set; }
    public decimal ValorPerdido { get; set; }
    public string? Reason { get; set; }
    public string? Observation { get; set; }
    public DateTime Created { get; set; }
    public int CreatedBy { get; set; }
}

/// <summary>Mermas acumuladas de un producto en el período consultado.</summary>
public class WriteOffByProductResponse
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Unidades { get; set; }
    public decimal ValorPerdido { get; set; }
    public int Eventos { get; set; }
}

/// <summary>Reporte de mermas por vencimiento en un período: cuánta plata se perdió y en qué productos.</summary>
public class WriteOffReportResponse
{
    public decimal TotalUnidades { get; set; }
    public decimal TotalValorPerdido { get; set; }
    public int TotalEventos { get; set; }
    public List<WriteOffByProductResponse> PorProducto { get; set; } = [];
    public List<WriteOffDetailResponse> Detalle { get; set; } = [];
}
