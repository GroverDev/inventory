using FluentValidation;

namespace Inventory.Domain;

/// <summary>
/// Dar de baja una existencia puntual (lote vencido, dañado, retirado). A
/// diferencia del ajuste genérico, apunta a un <see cref="StockItemId"/>
/// explícito: no tiene sentido "ajustar el producto en general" cuando lo que
/// se quiere sacar es un lote específico que ya venció.
/// </summary>
public class StockWriteOffRequest
{
    public string ProductId { get; set; } = "";
    public string StockItemId { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = "";
    public string Observation { get; set; } = "";
}

public class StockWriteOffRequestValidator : AbstractValidator<StockWriteOffRequest>
{
    public StockWriteOffRequestValidator()
    {
        RuleFor(r => r.ProductId)
            .NotEmpty().WithMessage("El producto es requerido.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("Id de producto inválido.");

        RuleFor(r => r.StockItemId)
            .NotEmpty().WithMessage("La existencia/lote es requerida.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("Id de existencia inválido.");

        RuleFor(r => r.Quantity)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");

        RuleFor(r => r.Reason)
            .NotEmpty().WithMessage("El motivo de la baja es requerido.");
    }
}
