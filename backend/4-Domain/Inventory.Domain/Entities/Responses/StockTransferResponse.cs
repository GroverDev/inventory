namespace Inventory.Domain;

/// <summary>Traspaso visto desde la sucursal activa.</summary>
public class StockTransferResponse
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public Guid OriginBranchId { get; set; }
    public string OriginBranchName { get; set; } = "";
    public Guid DestBranchId { get; set; }
    public string DestBranchName { get; set; } = "";

    /// <summary>borrador, enviado, recibido o anulado.</summary>
    public string Status { get; set; } = "";
    public string Notes { get; set; } = "";

    /// <summary>"salida" si la sucursal activa es el origen, "entrada" si es el destino.</summary>
    public string Direction { get; set; } = "";

    public DateTime Created { get; set; }
    public string CreatedByName { get; set; } = "";
    public DateTime? SentAt { get; set; }
    public string SentByName { get; set; } = "";
    public DateTime? ReceivedAt { get; set; }
    public string ReceivedByName { get; set; } = "";

    public int LinesCount { get; set; }
    public decimal TotalQuantity { get; set; }

    public List<StockTransferDetailResponse> Detail { get; set; } = [];
}

public class StockTransferDetailResponse
{
    public Guid Id { get; set; }
    public Guid TransferId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string TrackingMode { get; set; } = "";
    public decimal Quantity { get; set; }

    /// <summary>Stock actual del producto en el origen. Sirve para armar el borrador.</summary>
    public decimal AvailableAtOrigin { get; set; }

    /// <summary>Lo que viajó, por lote. Vacío mientras es borrador.</summary>
    public List<StockTransferLotResponse> Lots { get; set; } = [];
}

public class StockTransferLotResponse
{
    public Guid Id { get; set; }
    public Guid DetailId { get; set; }
    public string LotCode { get; set; } = "";
    public DateTime? ExpiryDate { get; set; }
    public string SerialNumber { get; set; } = "";
    public decimal QuantitySent { get; set; }
    public decimal? QuantityReceived { get; set; }
}
