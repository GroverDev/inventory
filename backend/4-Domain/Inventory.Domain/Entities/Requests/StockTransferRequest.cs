using FluentValidation;

namespace Inventory.Domain;

/// <summary>
/// Borrador de traspaso. El origen es siempre la sucursal de la sesión: no viaja
/// en el pedido, así nadie puede armar un traspaso desde una sucursal ajena.
/// </summary>
public class StockTransferRequest
{
    public Guid DestBranchId { get; set; }
    public string Notes { get; set; } = "";
    public List<StockTransferLineRequest> Detail { get; set; } = [];
}

public class StockTransferLineRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class StockTransferRequestValidator : AbstractValidator<StockTransferRequest>
{
    public StockTransferRequestValidator()
    {
        RuleFor(t => t.DestBranchId)
            .NotEmpty().WithMessage("Elija la sucursal de destino.");
        RuleFor(t => t.Notes)
            .MaximumLength(500).WithMessage("Las notas no pueden superar {MaxLength} caracteres.");
        RuleFor(t => t.Detail)
            .NotEmpty().WithMessage("Agregue al menos un producto.")
            .Must(d => d.Select(l => l.ProductId).Distinct().Count() == d.Count)
            .WithMessage("Hay productos repetidos: sume las cantidades en una sola línea.");
        RuleForEach(t => t.Detail).ChildRules(l =>
        {
            l.RuleFor(x => x.ProductId).NotEmpty().WithMessage("Hay una línea sin producto.");
            l.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Las cantidades deben ser mayores a cero.");
        });
    }
}

/// <summary>
/// Lo recibido de cada lote enviado. Un lote que no figure se toma como recibido
/// completo, así que una lista vacía recibe todo.
/// </summary>
public class ReceiveStockTransferRequest
{
    public List<ReceivedLotRequest> Lots { get; set; } = [];
}

public class ReceivedLotRequest
{
    public Guid LotId { get; set; }
    public decimal QuantityReceived { get; set; }
}
